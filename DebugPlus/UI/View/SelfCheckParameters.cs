using System.Collections.Generic;
using DebugPlus.Operations;
using UnityEngine;

namespace DebugPlus.UI.View
{
    /// <summary>
    /// 自检/范例区用的三个参数（批 3-3）——**值只存在调用方给的读写通道里**，
    /// 也就是"面板自己的字段"，不读不写任何游戏对象、不碰存档、不碰时间。
    ///
    /// 它们有两种身份，都重要：
    /// ① **实机验证台**：面板底部那块「控件自检 · 用法范例」区靠它们把
    ///    滑条 / 数值框 / 勾选 / 下拉四种控件摆到台面上 —— 走的是**真的**
    ///    <c>ParameterRowFactory</c> 分派（不是手搭的假行），所以验的是上线代码本身；
    /// ② **最短范例**："自定义参数怎么写"看这三个类就够了：
    ///    继承对应基类 → 把 Label/范围/单位/选项报出来 → 读和写各自接到自己的数据上。
    ///
    /// 与真实参数（如 <c>GrowthParameter</c>）的唯一区别只是"数据在哪"：
    /// 真实参数读写游戏组件，这里读写面板字段。**读写接口完全一样**。
    ///
    /// Target 传的是面板自己的 GameObject：自检参数不属于任何游戏实体，
    /// 这里只为满足基类"参数与目标一一绑定"的约定（基类不会去解引用它）。
    /// </summary>
    public sealed class SelfCheckNumericParameter : NumericParameter
    {
        private readonly string label;
        private readonly System.Func<float> read;
        private readonly System.Action<float> write;
        private readonly float min;
        private readonly float max;
        private readonly bool wholeNumbers;
        private readonly NumericControl control;
        private readonly string unit;
        private readonly string numberFormat;

        public SelfCheckNumericParameter(GameObject target, string label,
            System.Func<float> read, System.Action<float> write,
            float min, float max, bool wholeNumbers,
            NumericControl control, string unit = "", string numberFormat = null)
            : base(target)
        {
            this.label = label;
            this.read = read;
            this.write = write;
            this.min = min;
            this.max = max;
            this.wholeNumbers = wholeNumbers;
            this.control = control;
            this.unit = unit ?? string.Empty;
            this.numberFormat = numberFormat;
        }

        public override string Label
        {
            get { return label; }
        }

        public override string Unit
        {
            get { return unit; }
        }

        public override float Min
        {
            get { return min; }
        }

        public override float Max
        {
            get { return max; }
        }

        public override bool WholeNumbers
        {
            get { return wholeNumbers; }
        }

        /// <summary>显示形态由构造时显式指定 —— 这正是"由参数自己声明"那条规矩的示范。</summary>
        public override NumericControl Control
        {
            get { return control; }
        }

        public override float GetValue()
        {
            return read != null ? read() : min;
        }

        public override void SetValue(float value)
        {
            if (write != null)
            {
                write(value);
            }
        }

        public override string Format(float value)
        {
            // 显式给了格式就用它（例：0–1 的滑条要两位小数），否则走基类的默认显示。
            // 注意这里覆写的是 **Format（不含单位）**：单位由基类的 Display = Format + Unit 补上，
            // 这样同一个参数无论显示成滑条还是数值框，单位都不会被拼两遍。
            return numberFormat != null ? value.ToString(numberFormat) : base.Format(value);
        }
    }

    /// <summary>自检/范例区的布尔参数（勾选方块）。读写都落调用方的通道。</summary>
    public sealed class SelfCheckToggleParameter : ToggleParameter
    {
        private readonly string label;
        private readonly System.Func<bool> read;
        private readonly System.Action<bool> write;

        public SelfCheckToggleParameter(GameObject target, string label,
            System.Func<bool> read, System.Action<bool> write)
            : base(target)
        {
            this.label = label;
            this.read = read;
            this.write = write;
        }

        public override string Label
        {
            get { return label; }
        }

        public override bool GetValue()
        {
            return read != null && read();
        }

        public override void SetValue(bool value)
        {
            if (write != null)
            {
                write(value);
            }
        }
    }

    /// <summary>自检/范例区的选项参数（下拉）。选项文本由调用方给定，读写的是**下标**。</summary>
    public sealed class SelfCheckChoiceParameter : ChoiceParameter
    {
        private readonly string label;
        private readonly IList<string> options;
        private readonly System.Func<int> read;
        private readonly System.Action<int> write;

        public SelfCheckChoiceParameter(GameObject target, string label,
            IList<string> options, System.Func<int> read, System.Action<int> write)
            : base(target)
        {
            this.label = label;
            this.options = options;
            this.read = read;
            this.write = write;
        }

        public override string Label
        {
            get { return label; }
        }

        public override IList<string> Options
        {
            get { return options; }
        }

        public override int GetIndex()
        {
            return read != null ? read() : 0;
        }

        public override void SetIndex(int index)
        {
            if (write != null)
            {
                write(index);
            }
        }
    }
}
