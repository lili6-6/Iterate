using UnityEngine;
using MoreMountains.Tools;
using MoreMountains.CorgiEngine;

namespace PP
{
    /// <summary>
    /// 空闲随机判定 Decision。
    /// 检测 AIActionPPIdle 的犹豫阶段是否结束，然后进行随机判定：
    ///
    /// 工作流程：
    /// 1. 犹豫未结束 → 返回 false（保持 Idle 状态）
    /// 2. 犹豫结束 → 掷骰子
    ///    - 随机命中（dice ≤ Odds）
    ///      → 调用 AIActionPPIdle.PlayResistance() 播放反抗动画
    ///      → 等待反抗动画播完（IsResistanceCompleted = true）
    ///      → 返回 true → AIBrain 切换到 TrueState
    ///    - 随机未命中（dice > Odds）
    ///      → 返回 false → AIBrain 切换到 FalseState（建议填 "Idle" 重新循环）
    ///
    /// 例如：TotalChance=10, Odds=7 → 70% 概率播反抗后切换，30% 概率重新 Idle
    /// </summary>
    [AddComponentMenu("PP/AI/Decisions/AI Decision PP Idle Random")]
    public class AIDecisionPPIdleRandom : AIDecision
    {
        [Header("Random")]
        /// 随机判定的分母（如 '10 分之 7' 中的 10）
        [Tooltip("随机判定的分母（如 '10 分之 7' 中的 10）")]
        public int TotalChance = 10;
        /// 随机判定的分子，掷骰子结果 <= Odds 时播放反抗动画并切换状态
        [Tooltip("随机判定的分子，掷骰子结果 <= Odds 时播放反抗动画并切换状态")]
        public int Odds = 7;

        protected AIActionPPIdle _idleAction;
        protected bool _hasRolled = false;
        protected bool _rolledResult = false;

        /// <summary>
        /// 初始化时获取 AIActionPPIdle 组件
        /// </summary>
        public override void Initialization()
        {
            base.Initialization();
            _idleAction = GetComponentInParent<AIActionPPIdle>();
            if (_idleAction == null)
            {
                Debug.LogWarning("[AIDecisionPPIdleRandom] 未找到 AIActionPPIdle 组件，请确保该 Decision 挂载在 Idle Action 所在的 GameObject 上。");
            }
        }

        /// <summary>
        /// 进入状态时重置随机标记
        /// </summary>
        public override void OnEnterState()
        {
            base.OnEnterState();
            _hasRolled = false;
        }

        /// <summary>
        /// 每帧判定：
        /// - 犹豫未结束 → 返回 false
        /// - 犹豫结束 → 掷骰子
        ///   - 命中 → 播反抗 → 等反抗结束 → 返回 true
        ///   - 未命中 → 返回 false
        /// </summary>
        public override bool Decide()
        {
            if (_idleAction == null) return false;

            // 1. 犹豫还没结束 → 不切换
            if (!_idleAction.IsHesitateCompleted) return false;

            // 2. 犹豫已结束，但还没掷骰子 → 掷骰子
            if (!_hasRolled)
            {
                int dice = MMMaths.RollADice(TotalChance);
                _rolledResult = (dice <= Odds);
                _hasRolled = true;

                //Debug.Log($"[AIDecisionPPIdleRandom] 掷骰子: {dice}/{TotalChance} → {(_rolledResult ? "命中，播放反抗" : "未命中")}");

                if (_rolledResult)
                {
                    // 命中 → 触发反抗动画
                    _idleAction.PlayResistance();
                    // 此时返回 false，等待反抗播完
                    return false;
                }
                else
                {
                    // 未命中 → 直接返回 false，走 FalseState
                    return false;
                }
            }

            // 3. 已掷骰子
            if (_rolledResult)
            {
                // 命中的情况：等待反抗动画播完
                if (_idleAction.IsResistanceCompleted)
                {
                    //Debug.Log("[AIDecisionPPIdleRandom] 反抗结束 → 切换状态");
                    return true;
                }
                return false;
            }
            else
            {
                // 未命中的情况：持续返回 false
                return false;
            }
        }
    }
}
