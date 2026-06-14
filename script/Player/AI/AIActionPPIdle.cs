using UnityEngine;
using MoreMountains.Tools;
using MoreMountains.CorgiEngine;

namespace PP
{
    /// <summary>
    /// 空闲等待 Action。
    /// 让角色在原地停止移动，播放犹豫动画。
    /// 犹豫结束后等待 AIDecisionPPIdleCompleted 的判定：
    /// - 随机命中 → Decision 调用 PlayResistance() 播放反抗动画，反抗结束后返回 true
    /// - 随机未命中 → Decision 直接返回 false，AIBrain 重新进入 Idle 循环
    /// </summary>
    [AddComponentMenu("PP/AI/Actions/AI Action PP Idle")]
    public class AIActionPPIdle : AIAction
    {
        [Header("Hesitate Duration")]
        /// 最小犹豫时间（秒）
        [Tooltip("最小犹豫时间（秒）")]
        public float MinHesitateDuration = 0.5f;
        /// 最大犹豫时间（秒）
        [Tooltip("最大犹豫时间（秒）")]
        public float MaxHesitateDuration = 2f;

        [Header("Resistance")]
        /// 反抗动画持续时间（秒）
        [Tooltip("反抗动画持续时间（秒）")]
        public float ResistanceDuration = 1f;

        protected CharacterHorizontalMovement _characterHorizontalMovement;
        protected Character _character;
        protected PlayerAnimation_EXT _playerAnimExt;

        // === 对外暴露的状态（供 Decision 读取） ===

        /// <summary>
        /// 犹豫阶段是否已结束
        /// </summary>
        public bool IsHesitateCompleted { get; private set; }

        /// <summary>
        /// 反抗动画是否已播完
        /// </summary>
        public bool IsResistanceCompleted { get; private set; }

        // === 内部状态 ===

        protected enum IdlePhase
        {
            Hesitate,   // 犹豫阶段
            Resistance, // 反抗阶段（由 Decision 触发）
            Done        // 结束
        }
        protected IdlePhase _currentPhase;
        protected float _hesitateEndTime;
        protected float _resistanceEndTime;

        /// <summary>
        /// 初始化时获取所需组件
        /// </summary>
        public override void Initialization()
        {
            if (!ShouldInitialize) return;
            base.Initialization();
            _character = GetComponentInParent<Character>();
            _characterHorizontalMovement = _character?.FindAbility<CharacterHorizontalMovement>();
            _playerAnimExt = _character?.FindAbility<PlayerAnimation_EXT>();
        }

        /// <summary>
        /// 进入状态时停止移动，开始犹豫
        /// </summary>
        public override void OnEnterState()
        {
            base.OnEnterState();

            // 停止移动
            if (_characterHorizontalMovement != null)
            {
                _characterHorizontalMovement.SetHorizontalMove(0f);
            }

            // 强制将 MovementState 切回 Idle，确保动画正确过渡到空闲状态
            if (_character != null)
            {
                _character.MovementState?.ChangeState(CharacterStates.MovementStates.Idle);
            }

            // 重置所有状态
            _currentPhase = IdlePhase.Hesitate;
            IsHesitateCompleted = false;
            IsResistanceCompleted = false;

            // 随机犹豫时长
            float hesitateDuration = Random.Range(MinHesitateDuration, MaxHesitateDuration);
            _hesitateEndTime = Time.time + hesitateDuration;

            // 播放犹豫动画
            if (_playerAnimExt != null)
            {
                _playerAnimExt.SetHesitate(true);
                _playerAnimExt.SetResistance(false);
            }

            //Debug.Log($"[AIActionPPIdle] 开始犹豫 {hesitateDuration:F2}s");
        }

        /// <summary>
        /// 每帧执行：保持停止状态，推进犹豫阶段
        /// </summary>
        public override void PerformAction()
        {
            // 确保角色保持停止
            if (_characterHorizontalMovement != null)
            {
                _characterHorizontalMovement.SetHorizontalMove(0f);
            }

            switch (_currentPhase)
            {
                case IdlePhase.Hesitate:
                    // 等待犹豫时间到
                    if (!IsHesitateCompleted && Time.time >= _hesitateEndTime)
                    {
                        IsHesitateCompleted = true;
                        // 停止犹豫动画
                        if (_playerAnimExt != null)
                        {
                            _playerAnimExt.SetHesitate(false);
                        }
                        //Debug.Log("[AIActionPPIdle] 犹豫结束，等待 Decision 判定");
                    }
                    break;

                case IdlePhase.Resistance:
                    // 等待反抗动画播完
                    if (Time.time >= _resistanceEndTime)
                    {
                        _currentPhase = IdlePhase.Done;
                        IsResistanceCompleted = true;
                        if (_playerAnimExt != null)
                        {
                            _playerAnimExt.SetResistance(false);
                        }
                        //Debug.Log("[AIActionPPIdle] 反抗结束");
                    }
                    break;

                case IdlePhase.Done:
                    // 什么都不做，等待 AIBrain 切换状态
                    break;
            }
        }

        /// <summary>
        /// 由 AIDecisionPPIdleCompleted 调用：播放反抗动画
        /// </summary>
        public void PlayResistance()
        {
            if (_currentPhase != IdlePhase.Hesitate) return;
            if (!IsHesitateCompleted) return;

            _currentPhase = IdlePhase.Resistance;
            _resistanceEndTime = Time.time + ResistanceDuration;
            IsResistanceCompleted = false;

            if (_playerAnimExt != null)
            {
                _playerAnimExt.SetResistance(true);
            }
            //Debug.Log("[AIActionPPIdle] Decision 触发 → 播放反抗动画");
        }

        /// <summary>
        /// 退出状态时复位所有动画状态
        /// </summary>
        public override void OnExitState()
        {
            base.OnExitState();

            // 复位动画状态
            if (_playerAnimExt != null)
            {
                _playerAnimExt.SetHesitate(false);
                _playerAnimExt.SetResistance(false);
            }

            _currentPhase = IdlePhase.Done;
            IsHesitateCompleted = false;
            IsResistanceCompleted = false;
        }
    }
}
