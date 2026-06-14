using UnityEngine;
using MoreMountains.Tools;
using MoreMountains.CorgiEngine;

namespace PP
{
    /// <summary>
    /// 平地随机方向走 Action。
    /// 进入状态时随机选择一个方向（左/右/停止），使用 Corgi 的 CharacterHorizontalMovement 移动。
    /// 可配置方向切换间隔和移动速度倍率。
    /// </summary>
    [AddComponentMenu("PP/AI/Actions/AI Action PP Random Direction")]
    public class AIActionPPRandomDirection : AIAction
    {
        [Header("Direction Options")]
        /// 是否允许向左移动
        [Tooltip("是否允许向左移动")]
        public bool CanGoLeft = true;
        /// 是否允许向右移动
        [Tooltip("是否允许向右移动")]
        public bool CanGoRight = true;
        /// 是否允许原地停止
        [Tooltip("是否允许原地停止")]
        public bool CanIdle = true;
        /// 方向切换的最小间隔（秒）
        [Tooltip("方向切换的最小间隔（秒）")]
        public float MinChangeInterval = 1f;
        /// 方向切换的最大间隔（秒）
        [Tooltip("方向切换的最大间隔（秒）")]
        public float MaxChangeInterval = 3f;

        protected CharacterHorizontalMovement _characterHorizontalMovement;
        protected float _nextChangeTime;
        protected int _currentDirection;

        /// <summary>
        /// 初始化时获取 CharacterHorizontalMovement 组件
        /// </summary>
        public override void Initialization()
        {
            if (!ShouldInitialize) return;
            base.Initialization();
            _characterHorizontalMovement = GetComponentInParent<Character>()?.FindAbility<CharacterHorizontalMovement>();
        }

        /// <summary>
        /// 进入状态时，立即随机一个方向并设定下次切换时间
        /// </summary>
        public override void OnEnterState()
        {
            base.OnEnterState();
            PickRandomDirection();
            ScheduleNextChange();
            Debug.Log($"RANDOM DIRECTION");
            //Debug.Log($"[AIActionPPRandomDirection] EnterState → 方向={DirectionToString(_currentDirection)}, 下次切换={_nextChangeTime:F2}s");
        }

        /// <summary>
        /// 每帧执行：到达切换时间则重新随机方向
        /// </summary>
        public override void PerformAction()
        {
            if (_characterHorizontalMovement == null) return;

            // 到达切换时间 → 重新随机方向
            if (Time.time >= _nextChangeTime)
            {
                int oldDir = _currentDirection;
                PickRandomDirection();
                ScheduleNextChange();
                if (oldDir != _currentDirection)
                {
                    //Debug.Log($"[AIActionPPRandomDirection] 方向切换 {DirectionToString(oldDir)} → {DirectionToString(_currentDirection)}, 下次切换={_nextChangeTime:F2}s");
                }
            }

            // 应用当前方向
            _characterHorizontalMovement.SetHorizontalMove(_currentDirection);
        }

        /// <summary>
        /// 退出状态时，停止移动
        /// </summary>
        public override void OnExitState()
        {
            base.OnExitState();
            if (_characterHorizontalMovement != null)
            {
                _characterHorizontalMovement.SetHorizontalMove(0f);
            }
            //Debug.Log($"[AIActionPPRandomDirection] ExitState");
            _currentDirection = 0;
        }

        /// <summary>
        /// 随机选择一个方向
        /// </summary>
        protected virtual void PickRandomDirection()
        {
            // 收集可用的方向选项
            int[] options;
            int canGoLeft = CanGoLeft ? 1 : 0;
            int canGoRight = CanGoRight ? 1 : 0;
            int canIdle = CanIdle ? 1 : 0;
            int totalOptions = canGoLeft + canGoRight + canIdle;

            if (totalOptions == 0)
            {
                _currentDirection = 0;
                return;
            }

            options = new int[totalOptions];
            int index = 0;
            if (CanGoLeft) options[index++] = -1;
            if (CanGoRight) options[index++] = 1;
            if (CanIdle) options[index++] = 0;

            _currentDirection = options[Random.Range(0, options.Length)];
        }

        /// <summary>
        /// 设定下次方向切换的时间
        /// </summary>
        protected virtual void ScheduleNextChange()
        {
            _nextChangeTime = Time.time + Random.Range(MinChangeInterval, MaxChangeInterval);
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
