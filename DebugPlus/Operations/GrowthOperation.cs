using System.Collections.Generic;
using UnityEngine;

namespace DebugPlus.Operations
{
    /// <summary>
    /// 「植物生长进度」能力（批 2b · A6）—— 本 Mod 第一条真正的**操作**。
    ///
    /// 取组件的写法直接照原版 PlantBranchGrower.cs:402-403：
    /// 先 `GetComponent&lt;IManageGrowingStates&gt;()`，取不到再退回 `GetSMI&lt;IManageGrowingStates&gt;()`
    /// （树枝那类是 SMI 实现，普通植物是 Growing 组件实现 —— Growing.cs:9 声明了该接口）。
    /// 走接口而不是硬编码 Growing，是为了"凡是有生长状态的实体都可调"，且不重复原版已有的分派逻辑。
    ///
    /// 时间中性：本能力只调原版的写值入口（SetValue 一类），**不触碰任何时间/调度器 API**，
    /// 不推进生长、不等时间流逝（plan.md §二.2）。
    /// </summary>
    public sealed class GrowthOperation : OperationRegistry.IOperation
    {
        /// <summary>取实体的生长状态实现（原版取法：组件优先，SMI 兜底）。</summary>
        public static IManageGrowingStates GetGrowingStates(GameObject go)
        {
            if (go == null)
            {
                return null;
            }
            IManageGrowingStates states = go.GetComponent<IManageGrowingStates>();
            return states != null ? states : go.GetSMI<IManageGrowingStates>();
        }

        public bool Matches(GameObject go)
        {
            return GetGrowingStates(go) != null;
        }

        public void BuildParameters(GameObject go, List<Parameter> into)
        {
            IManageGrowingStates states = GetGrowingStates(go);
            if (states != null)
            {
                into.Add(new GrowthParameter(go, states));
            }
        }
    }

    /// <summary>
    /// 生长进度参数。滑条单位是**百分比 0–100**（整数刻度），写回时换算成原版要的 0–1 比例。
    /// </summary>
    public sealed class GrowthParameter : Parameter
    {
        private readonly IManageGrowingStates states;

        public GrowthParameter(GameObject target, IManageGrowingStates states)
            : base(target)
        {
            this.states = states;
        }

        public override string Label
        {
            get { return STRINGS.UI.DEBUGPLUS.PARAM_GROWTH; }
        }

        public override string Unit
        {
            get { return STRINGS.UI.DEBUGPLUS.UNIT_PERCENT; }
        }

        public override float Min
        {
            get { return 0f; }
        }

        public override float Max
        {
            get { return 100f; }
        }

        public override bool WholeNumbers
        {
            get { return true; }
        }

        public override float GetValue()
        {
            // Growing.PercentGrown()（Growing.cs:121-124）= maturity.value / maturity.GetMax()，即 0–1 比例。
            // 原版自己也是这么乘 100 显示的（CreatureStatusItems.cs:240/253）。
            return states.PercentGrown() * 100f;
        }

        public override void SetValue(float value)
        {
            // ⚠️ OverrideMaturityLevel 的参数是 **0–1 的比例**，不是 0–100
            // （Growing.cs:54-58：maturity.SetValue(maturity.GetMax() * percent)）⇒ 这里必须除以 100。
            states.OverrideMaturityLevel(Mathf.Clamp01(value / 100f));
        }

        public override string Display(float value)
        {
            return Mathf.RoundToInt(value).ToString() + Unit;
        }
    }
}
