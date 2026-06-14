using UnityEngine;
using MoreMountains.Tools;
using MoreMountains.CorgiEngine;

namespace PP
{
    /// <summary>
    /// 边跳边移动 Action。
    /// 在移动的同时持续跳跃，使用 Corgi 的 CharacterHorizontalMovement 和 CharacterJump。
    /// 可配置移动方向和跳跃间隔。
    ///
    /// 注意：由于 CharacterJump 的 JumpIsProportionalToThePressTime 可能为 true，
    /// AI 需要模拟"按住跳跃键"的效果，在跳跃触发后持续调用 JumpStart() 一小段时间，
    /// 否则跳跃高度会非常低（几乎看不出来）。
    /// </summary>
    [AddComponentMenu("PP/AI/Actions/AI Action PP Jump And Move")]
    public class AIActionPPJumpAndMove : AIAction
    {
        [Header("Movement")]
        /// 移动方向：-1=左，0=随机，1=右
        [Tooltip("移动方向：-1=左，0=随机，1=右")]
        public int MoveDirection = 0;
        /// 是否允许向左移动（随机方向时有效）
        [Tooltip("是否允许向左移动（随机方向时有效）")]
        public bool CanGoLeft = true;
        /// 是否允许向右移动（随机方向时有效）
        [Tooltip("是否允许向右移动（随机方向时有效）")]
        public bool CanGoRight = true;

        [Header("Jump")]
        /// 跳跃的最小间隔（秒）
        [Tooltip("跳跃的最小间隔（秒）")]
        public float MinJumpInterval = 0.5f;
        /// 跳跃的最大间隔（秒）
        [Tooltip("跳跃的最大间隔（秒）")]
        public float MaxJumpInterval = 2f;
        /// 是否只在着地后才允许再次跳跃
        [Tooltip("是否只在着地后才允许再次跳跃")]
        public bool JumpOnlyWhenGrounded = true;

        [Header("AI Jump Hold")]
        /// AI模拟按住跳跃键的持续时间（秒）。
        /// 当 CharacterJump.JumpIsProportionalToThePressTime = true 时，
        /// 跳跃高度取决于按键时间，AI需要持续调用 JumpStart() 才能跳得够高。
        [Tooltip("AI模拟按住跳跃键的持续时间（秒）")]
        public float JumpHoldDuration = 0.2f;

        protected CharacterHorizontalMovement _characterHorizontalMovement;
        protected CharacterJump _characterJump;
        protected CorgiController _controller;
        protected int _currentDirection;
        protected float _nextJumpTime;
        protected float _jumpHoldEndTime;
        protected bool _isHoldingJump;

        /// <summary>
        /// 初始化时获取所需组件
        /// </summary>
        public override void Initialization()
        {
            if (!ShouldInitialize) return;
            base.Initialization();

            Character character = GetComponentInParent<Character>();
            if (character != null)
            {
                _characterHorizontalMovement = character.FindAbility<CharacterHorizontalMovement>();
                _characterJump = character.FindAbility<CharacterJump>();
            }
            _controller = GetComponentInParent<CorgiController>();
        }

        /// <summary>
        /// 进入状态时初始化方向和跳跃计时
        /// </summary>
        public override void OnEnterState()
        {
            base.OnEnterState();
            DetermineDirection();
            ScheduleNextJump();
            _isHoldingJump = false;
            Debug.Log($"JUMP AND MOVE");
        }

        /// <summary>
        /// 每帧执行：持续移动 + 按间隔跳跃（模拟按住效果）
        /// </summary>
        public override void PerformAction()
        {
            if (_characterHorizontalMovement == null) return;

            // 持续移动
            _characterHorizontalMovement.SetHorizontalMove(_currentDirection);

            if (_characterJump == null) return;

            // ---- 跳跃按住阶段：持续调用 JumpStart() 模拟按住跳跃键 ----
            if (_isHoldingJump)
            {
                if (Time.time < _jumpHoldEndTime)
                {
                    _characterJump.JumpStart();
                }
                else
                {
                    // 按住时间结束
                    _isHoldingJump = false;
                }
                return; // 按住期间不检查新的跳跃
            }

            // ---- 检查是否可以触发新跳跃 ----
            if (Time.time >= _nextJumpTime)
            {
                bool canJump = true;

                if (JumpOnlyWhenGrounded)
                {
                    canJump = (_controller != null && _controller.State.IsGrounded);
                }

                if (canJump)
                {
                    // 触发跳跃，并进入按住阶段
                    _characterJump.JumpStart();
                    _isHoldingJump = true;
                    _jumpHoldEndTime = Time.time + JumpHoldDuration;
                    ScheduleNextJump();
                }
            }
        }

        /// <summary>
        /// 退出状态时停止移动，重置按住状态
        /// </summary>
        public override void OnExitState()
        {
            base.OnExitState();
            if (_characterHorizontalMovement != null)
            {
                _characterHorizontalMovement.SetHorizontalMove(0f);
            }
            _isHoldingJump = false;
        }

        /// <summary>
        /// 确定移动方向
        /// </summary>
        protected virtual void DetermineDirection()
        {
            if (MoveDirection != 0)
            {
                _currentDirection = MoveDirection;
            }
            else
            {
                int canGoLeft = CanGoLeft ? 1 : 0;
                int canGoRight = CanGoRight ? 1 : 0;
                int total = canGoLeft + canGoRight;

                if (total == 0)
                {
                    _currentDirection = 0;
                    return;
                }

                int[] options = new int[total];
                int index = 0;
                if (CanGoLeft) options[index++] = -1;
                if (CanGoRight) options[index++] = 1;

                _currentDirection = options[Random.Range(0, options.Length)];
            }
        }

        /// <summary>
        /// 安排下次跳跃时间
        /// </summary>
        protected virtual void ScheduleNextJump()
        {
            _nextJumpTime = Time.time + Random.Range(MinJumpInterval, MaxJumpInterval);
        }

        /// <summary>
        /// 将方向值转为可读字符串
        /// </summary>
        protected virtual string DirectionToString(int dir)
        {
            if (dir == -1) return "← 左";
            if (dir == 1) return "→ 右";
            return "· 停止";
        }
    }
}
