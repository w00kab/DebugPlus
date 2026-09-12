using System.Collections.Generic;
using UnityEngine;

namespace DebugPlus.Operations
{
    /// <summary>
    /// 「实体特征 → 参数行」注册表（批 2b · A5）。
    ///
    /// 存在的意义：**判定（按钮要不要出现）与能力（面板里有几行）同源**。
    /// A1 的命中判定改为查这里，于是不可能出现「按钮在但面板空白」或
    /// 「能力在但没入口」两种不一致状态；批 2a 里那句临时判定
    /// （`go.GetComponent&lt;Growing&gt;() != null`）随之作废。
    ///
    /// 登记项全部是纯特征判定 + 构造参数对象，不碰界面、不碰时间。
    /// </summary>
    public static class OperationRegistry
    {
        /// <summary>一条能力：负责判定适用性并给出该实体身上的参数列表。</summary>
        public interface IOperation
        {
            /// <summary>该实体是否适用本能力（只做特征判定，不建界面）。</summary>
            bool Matches(GameObject go);

            /// <summary>把该实体的参数追加进 into（可追加 0 个或多个）。</summary>
            void BuildParameters(GameObject go, List<Parameter> into);
        }

        private static readonly List<IOperation> operations = new List<IOperation>();

        static OperationRegistry()
        {
            Register(new GrowthOperation()); // 初版只登记「植物生长进度」
        }

        public static void Register(IOperation operation)
        {
            if (operation != null)
            {
                operations.Add(operation);
            }
        }

        /// <summary>A1 的命中判定：该实体身上是否有任何已登记的可调参数。</summary>
        public static bool IsConfigurable(GameObject go)
        {
            if (go == null)
            {
                return false;
            }
            for (int i = 0; i < operations.Count; i++)
            {
                if (operations[i].Matches(go))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>构造该实体全部参数行（每行一个 Parameter）。</summary>
        public static List<Parameter> BuildParameters(GameObject go)
        {
            var result = new List<Parameter>();
            if (go == null)
            {
                return result;
            }
            for (int i = 0; i < operations.Count; i++)
            {
                if (operations[i].Matches(go))
                {
                    operations[i].BuildParameters(go, result);
                }
            }
            return result;
        }
    }
}
