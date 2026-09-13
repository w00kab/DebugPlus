using System.Collections.Generic;
using UnityEngine;

namespace DebugPlus.UI.Component
{
    /// <summary>
    /// 程序化贴图工厂：按需生成**圆角矩形 / 圆形 / 白方块**三类 sprite，并**按参数组合缓存**。
    ///
    /// 为什么要自己画：ONI 的原版圆角素材都在预制体/图集里（mod 拿不到引用），
    /// 而纯代码控件全是硬边方块 —— 观感差一档。这里用「白色遮罩 + 9-slice」的通用做法解决：
    /// 纹理只画 alpha（形状），颜色交给 `Image.color` 上色，于是**一张 sprite 可复用为任意颜色**，
    /// 配合 `Image.type = Sliced` 可拉伸成任意尺寸而圆角不变形。
    ///
    /// 三条实现要点：
    /// ① 圆角用**符号距离场 + smoothstep**算 alpha（不是简单"圆内圆外"二值），软边过渡消除锯齿，
    ///    同时圆角半径不会随缩放走样；
    /// ② sprite 的 9-slice border 把**模糊过渡带一起圈进去**（`radius + ceil(blur)`），
    ///    否则 Sliced 拉伸时软边会被裁掉、圆角变方；
    /// ③ 全部**按需生成 + 永久缓存**（键 = 尺寸/半径/模糊半径），UI 反复重建零成本；
    ///    生成失败回退白方块且**不缓存**，下次仍会重试（不把一次失败钉死）。
    ///
    /// 线程口径：全部在主线程按需创建，无静态初始化（不使用时一张纹理都不会生成）。
    /// </summary>
    public static class UISpriteFactory
    {
        /// <summary>缓存键：尺寸 / 圆角半径 / 边缘模糊半径（用结构体而非元组，避免额外类型依赖）。</summary>
        private struct SpriteKey : System.IEquatable<SpriteKey>
        {
            public readonly int Size;
            public readonly int Radius;

            public SpriteKey(int size, int radius, float blur)
            {
                Size = size;
                Radius = radius;
                Blur = Mathf.RoundToInt(blur * 10f); // 模糊半径折算成整数，避开浮点相等比较
            }

            public readonly int Blur;

            public bool Equals(SpriteKey other)
            {
                return Size == other.Size && Radius == other.Radius && Blur == other.Blur;
            }

            public override bool Equals(object obj)
            {
                return obj is SpriteKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = Size * 397;
                    hash = (hash ^ Radius) * 397;
                    return hash ^ Blur;
                }
            }
        }

        private static readonly Dictionary<SpriteKey, Sprite> RoundedCache =
            new Dictionary<SpriteKey, Sprite>();

        private static readonly Dictionary<int, Sprite> CircleCache = new Dictionary<int, Sprite>();

        private static Sprite whiteSquare;

        // ══════════════════ 圆角矩形 ══════════════════

        /// <summary>
        /// 取一个可 9-slice 的**白色圆角矩形** sprite：`Image.color` 上色、`Image.type = Sliced` 拉伸。
        /// </summary>
        /// <param name="size">纹理边长（默认 64；越大圆角越精细）</param>
        /// <param name="radius">圆角半径（默认 8；必须 &lt; size/2，超出自动收敛）</param>
        /// <param name="blur">边缘模糊半径（默认 1.5：消锯齿同时保持清晰；0 = 硬边）</param>
        public static Sprite GetRoundedRect(int size = 64, int radius = 8, float blur = 1.5f)
        {
            Normalize(ref size, ref radius, ref blur);
            var key = new SpriteKey(size, radius, blur);
            if (RoundedCache.TryGetValue(key, out Sprite cached))
            {
                return cached;
            }

            Sprite sprite = Build(size, radius, blur);

            if (sprite == null)
            {
                return GetWhiteSquare();
            }
            RoundedCache[key] = sprite;
            return sprite;
        }

        // ══════════════════ 圆形 ══════════════════

        /// <summary>取一个白色圆形 sprite（滑块/状态点/开关圆钮用；9-slice border = size/2，任意尺寸仍是圆）。</summary>
        public static Sprite GetCircle(int size = 32)
        {
            if (size < 8)
            {
                size = 8;
            }
            if (CircleCache.TryGetValue(size, out Sprite cached))
            {
                return cached;
            }

            Sprite sprite = BuildCircle(size);
            if (sprite == null)
            {
                // 回退：用"半径拉满"的圆角矩形近似圆
                return GetRoundedRect(size, size / 2 - 1, 1.5f);
            }
            CircleCache[size] = sprite;
            return sprite;
        }

        // ══════════════════ 白方块 ══════════════════

        /// <summary>1×1 白色方块 sprite（纯色块/占位；配合 `Image.color` 上色）。</summary>
        public static Sprite GetWhiteSquare()
        {
            if (whiteSquare != null)
            {
                return whiteSquare;
            }
            var texture = NewTexture(4);
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            whiteSquare = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
            whiteSquare.hideFlags = HideFlags.HideAndDontSave;
            return whiteSquare;
        }

        // ══════════════════ 内部实现 ══════════════════

        /// <summary>参数规范化：(size, radius, blur) 的不同写法命中同一张纹理，避免等价纹理重复生成。</summary>
        private static void Normalize(ref int size, ref int radius, ref float blur)
        {
            if (size < 4)
            {
                size = 4;
            }
            if (radius >= size / 2)
            {
                radius = size / 2 - 1;
            }
            if (radius < 1)
            {
                radius = 1;
            }
            if (blur < 0f)
            {
                blur = 0f;
            }
        }

        private static Texture2D NewTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave; // 别被存进场景
            return texture;
        }

        /// <summary>把「符号距离」映射为 alpha（过渡带 smoothstep），0.5 落在边界上。</summary>
        private static float DistanceToAlpha(float signedDistance, float blur)
        {
            if (blur <= 0.001f)
            {
                return signedDistance <= 0f ? 1f : 0f;
            }
            float t = signedDistance / blur;
            float alpha = Mathf.Clamp01(0.5f - t * 0.5f);
            return alpha * alpha * (3f - 2f * alpha); // smoothstep：过渡带中部更陡、两端更缓
        }

        /// <summary>生成圆角矩形（符号距离：正 = 形状外）。</summary>
        private static Sprite Build(int size, int radius, float blur)
        {
            Sprite sprite = null;
            Texture2D texture = null;
            try
            {
                texture = NewTexture(size);
                var pixels = new Color[size * size];
                float half = size * 0.5f;
                float radiusF = radius;
                // 内矩形半宽：去掉圆角后的直边区域（轴距超过 innerHalf 即进入圆角区）
                float innerHalf = half - radiusF;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float px = x + 0.5f; // 像素中心（连续空间）
                        float py = y + 0.5f;
                        // 到内矩形边的距离（正 = 在直边区之外）
                        float dx = Mathf.Abs(px - half) - innerHalf;
                        float dy = Mathf.Abs(py - half) - innerHalf;

                        float signed;
                        if (dx > 0f && dy > 0f)
                        {
                            signed = Mathf.Sqrt(dx * dx + dy * dy) - radiusF; // 角部四分之一圆
                        }
                        else if (dx > 0f)
                        {
                            signed = dx - radiusF; // 竖向直边
                        }
                        else if (dy > 0f)
                        {
                            signed = dy - radiusF; // 横向直边
                        }
                        else
                        {
                            signed = Mathf.Max(dx, dy) - radiusF; // 内部（恒负）
                        }

                        float alpha = DistanceToAlpha(signed, blur);
                        pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                }

                texture.SetPixels(pixels);
                texture.Apply();

                int border = radius + Mathf.CeilToInt(blur); // 9-slice 把软边圈进去
                sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f),
                    100f, 0u, SpriteMeshType.FullRect,
                    new Vector4(border, border, border, border));
                sprite.hideFlags = HideFlags.HideAndDontSave;
            }
            catch (System.Exception exception)
            {
                UnityEngine.Debug.LogWarning("[DebugPlus] 生成圆角 sprite 失败（" + size + "）：" +
                    exception.Message);
                if (texture != null && sprite == null)
                {
                    UnityEngine.Object.Destroy(texture); // 生成失败别漏纹理
                }
                return null;
            }
            return sprite;
        }

        private static Sprite BuildCircle(int size)
        {
            Sprite sprite = null;
            Texture2D texture = null;
            try
            {
                texture = NewTexture(size);
                var pixels = new Color[size * size];
                float half = size * 0.5f;
                float radius = half - 0.5f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x + 0.5f - half;
                        float dy = y + 0.5f - half;
                        float distance = Mathf.Sqrt(dx * dx + dy * dy);
                        float alpha = Mathf.Clamp01(radius - distance + 0.5f); // 1px 软边
                        pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                }

                texture.SetPixels(pixels);
                texture.Apply();

                int border = size / 2;
                sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f),
                    100f, 0u, SpriteMeshType.FullRect,
                    new Vector4(border, border, border, border));
                sprite.hideFlags = HideFlags.HideAndDontSave;
            }
            catch (System.Exception exception)
            {
                UnityEngine.Debug.LogWarning("[DebugPlus] 生成圆形 sprite 失败（" + size + "）：" +
                    exception.Message);
                if (texture != null && sprite == null)
                {
                    UnityEngine.Object.Destroy(texture);
                }
                return null;
            }
            return sprite;
        }
    }
}
