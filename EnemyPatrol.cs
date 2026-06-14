using UnityEngine;
using System.Collections.Generic;
using MoreMountains.CorgiEngine;

namespace PP
{
    /// <summary>
    /// EnemyPatrol - 敌人巡逻脚本
    /// 基于坐标点（Waypoints）进行巡逻移动，类似 MMPathMovement 的 Loop/BackAndForth 模式。
    /// 
    /// 功能：
    /// - 通过 Inspector 设置巡逻路径点（世界坐标或相对偏移）
    /// - 支持 Loop（循环）、BackAndForth（往返）两种巡逻模式
    /// - 自动处理转身（Flip），支持 SpriteRenderer.flipX 和 Transform.localScale 两种方式
    /// - 提供动画参数：Speed (float), Walking (bool), FacingRight (bool)
    /// - 支持任意方向移动（水平、垂直、斜向）
    /// 
    /// 用法：
    /// 1. 将此脚本挂载到敌人 GameObject 上
    /// 2. 在 Inspector 中设置 Waypoints 列表（路径点）
    /// 3. 设置 PatrolSpeed、PatrolMode 等参数
    /// 4. 如果有 Animator，会自动更新动画参数
    /// </summary>
    [AddComponentMenu("PP/Enemy/Enemy Patrol")]
    public class EnemyPatrol : MonoBehaviour
    {
        /// <summary>
        /// 巡逻模式
        /// </summary>
        public enum PatrolMode
        {
            /// 循环：到达最后一个点后回到第一个点，无限循环
            Loop,
            /// 往返：到达最后一个点后反向走回第一个点，来回移动
            BackAndForth
        }

        /// <summary>
        /// 转身方式
        /// </summary>
        public enum FlipMode
        {
            /// 使用 SpriteRenderer.flipX
            SpriteFlipX,
            /// 使用 Transform.localScale 的 x 轴取反
            ScaleX,
            /// 不翻转
            None
        }

        [Header("巡逻路径")]
        /// 巡逻路径点列表（世界坐标）
        [Tooltip("巡逻路径点列表（世界坐标）")]
        public List<Vector3> Waypoints = new List<Vector3>();

        /// 是否使用相对偏移（相对于初始位置）
        [Tooltip("是否使用相对偏移（相对于初始位置）")]
        public bool UseRelativeOffset = false;

        [Header("巡逻设置")]
        /// 移动速度
        [Tooltip("移动速度")]
        public float PatrolSpeed = 2f;

        /// 巡逻模式
        [Tooltip("巡逻模式：Loop=循环，BackAndForth=往返")]
        public PatrolMode CurrentPatrolMode = PatrolMode.Loop;

        /// 到达路径点后的停留时间（秒）
        [Tooltip("到达路径点后的停留时间（秒）")]
        public float WaitTimeAtWaypoint = 0f;

        /// 到达路径点的判定距离
        [Tooltip("到达路径点的判定距离")]
        public float MinDistanceToWaypoint = 0.1f;

        /// 是否在 Start 时自动开始巡逻
        [Tooltip("是否在 Start 时自动开始巡逻")]
        public bool AutoStart = true;

        /// 是否与 CorgiController 兼容（启用时会临时禁用 CorgiController 的重力和位置控制）
        [Tooltip("是否与 CorgiController 兼容（启用时会临时禁用 CorgiController 的重力和位置控制）")]
        public bool DisableCorgiControllerOnPatrol = true;

        [Header("转身设置")]
        /// 转身方式
        [Tooltip("转身方式：SpriteFlipX=翻转Sprite，ScaleX=缩放X轴，None=不翻转")]
        public FlipMode CurrentFlipMode = FlipMode.SpriteFlipX;

        /// 是否根据移动方向自动转身
        [Tooltip("是否根据移动方向自动转身")]
        public bool AutoFlip = true;

        [Header("动画参数")]
        /// 是否更新 Animator 参数
        [Tooltip("是否更新 Animator 参数")]
        public bool UpdateAnimator = true;

        /// 移动时的动画参数名（float）
        [Tooltip("移动时的动画参数名（float）")]
        public string SpeedParameterName = "Speed";

        /// 是否在移动中的动画参数名（bool）
        [Tooltip("是否在移动中的动画参数名（bool）")]
        public string WalkingParameterName = "Walking";

        /// 面朝方向的动画参数名（bool）
        [Tooltip("面朝方向的动画参数名（bool）")]
        public string FacingRightParameterName = "FacingRight";

        // 运行时状态
        /// 当前是否正在巡逻
        public bool IsPatrolling { get; protected set; }

        /// 当前目标路径点索引
        public int CurrentWaypointIndex { get; protected set; }

        /// 当前移动方向（1=正向，-1=反向）
        public int CurrentDirection { get; protected set; } = 1;

        /// 当前是否面朝右
        public bool IsFacingRight { get; protected set; } = true;

        // 组件缓存
        protected SpriteRenderer _spriteRenderer;
        protected Animator _animator;
        protected Transform _transform;
        protected Rigidbody2D _rigidbody2D;
        protected CorgiController _corgiController;

        // 内部状态
        protected Vector3 _initialPosition;
        protected List<Vector3> _worldWaypoints = new List<Vector3>();
        protected int _currentIndex = 0;
        protected float _waitTimer = 0f;
        protected bool _isWaiting = false;
        protected float _currentSpeed;
        protected bool _hasCorgiController;
        protected bool _corgiControllerWasEnabled;

        // Animator 参数哈希
        protected int _speedParameterHash;
        protected int _walkingParameterHash;
        protected int _facingRightParameterHash;

        protected virtual void Awake()
        {
            _transform = transform;
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _corgiController = GetComponent<CorgiController>();
            _hasCorgiController = _corgiController != null;

            // 缓存 Animator 参数哈希
            if (_animator != null)
            {
                _speedParameterHash = Animator.StringToHash(SpeedParameterName);
                _walkingParameterHash = Animator.StringToHash(WalkingParameterName);
                _facingRightParameterHash = Animator.StringToHash(FacingRightParameterName);
            }
        }

        protected virtual void Start()
        {
            _initialPosition = _transform.position;

            // 初始化路径点
            InitializeWaypoints();

            // 自动开始巡逻
            if (AutoStart && _worldWaypoints.Count > 0)
            {
                StartPatrol();
            }
        }

        protected virtual void LateUpdate()
        {
            if (!IsPatrolling || _worldWaypoints.Count == 0)
                return;

            // 处理等待
            if (_isWaiting)
            {
                _waitTimer -= Time.deltaTime;
                if (_waitTimer <= 0f)
                {
                    _isWaiting = false;
                    MoveToNextWaypoint();
                }
                else
                {
                    _currentSpeed = 0f;
                    UpdateAnimatorParameters();
                }
                return;
            }

            // 向当前目标点移动
            MoveTowardsCurrentWaypoint();
        }

        #region 公共方法

        /// <summary>
        /// 开始巡逻
        /// </summary>
        public virtual void StartPatrol()
        {
            if (_worldWaypoints.Count == 0)
            {
                Debug.LogWarning($"[EnemyPatrol] {name} 没有设置路径点，无法开始巡逻");
                return;
            }

            IsPatrolling = true;
            _currentIndex = 0;
            CurrentDirection = 1;
            CurrentWaypointIndex = 0;
            _isWaiting = false;

            // 禁用 CorgiController 避免冲突
            if (_hasCorgiController && DisableCorgiControllerOnPatrol)
            {
                _corgiControllerWasEnabled = _corgiController.enabled;
                _corgiController.enabled = false;
            }

            // 直接设置目标为第一个路径点
            SetTargetWaypoint(0);
        }

        /// <summary>
        /// 停止巡逻
        /// </summary>
        public virtual void StopPatrol()
        {
            IsPatrolling = false;
            _currentSpeed = 0f;

            // 恢复 CorgiController
            if (_hasCorgiController && DisableCorgiControllerOnPatrol)
            {
                _corgiController.enabled = _corgiControllerWasEnabled;
            }

            UpdateAnimatorParameters();
        }

        /// <summary>
        /// 暂停巡逻（保持当前位置）
        /// </summary>
        public virtual void PausePatrol()
        {
            IsPatrolling = false;
        }

        /// <summary>
        /// 恢复巡逻
        /// </summary>
        public virtual void ResumePatrol()
        {
            if (_worldWaypoints.Count > 0)
            {
                IsPatrolling = true;
            }
        }

        /// <summary>
        /// 设置目标路径点索引
        /// </summary>
        public virtual void SetTargetWaypoint(int index)
        {
            if (index < 0 || index >= _worldWaypoints.Count)
                return;

            _currentIndex = index;
            CurrentWaypointIndex = index;
        }

        /// <summary>
        /// 强制翻转朝向
        /// </summary>
        public virtual void Flip()
        {
            IsFacingRight = !IsFacingRight;
            ApplyFlip();
        }

        /// <summary>
        /// 设置面朝方向
        /// </summary>
        public virtual void SetFacingRight(bool facingRight)
        {
            if (IsFacingRight != facingRight)
            {
                IsFacingRight = facingRight;
                ApplyFlip();
            }
        }

        /// <summary>
        /// 重新加载路径点（运行时修改 Waypoints 后调用）
        /// </summary>
        public virtual void ReloadWaypoints()
        {
            InitializeWaypoints();
        }

        #endregion

        #region 内部方法

        /// <summary>
        /// 初始化路径点：将相对偏移转换为世界坐标
        /// BackAndForth 模式下如果只有一个路径点，自动在初始位置和该点之间往返
        /// </summary>
        protected virtual void InitializeWaypoints()
        {
            _worldWaypoints.Clear();

            if (Waypoints.Count == 0)
                return;

            // BackAndForth 模式且只有一个路径点时，自动包含初始位置作为起点
            if (CurrentPatrolMode == PatrolMode.BackAndForth && Waypoints.Count == 1)
            {
                Vector3 targetPos = UseRelativeOffset ? _initialPosition + Waypoints[0] : Waypoints[0];
                // 确保目标点与初始位置不同
                if (Vector3.Distance(_initialPosition, targetPos) > MinDistanceToWaypoint)
                {
                    _worldWaypoints.Add(_initialPosition);
                    _worldWaypoints.Add(targetPos);
                    return;
                }
            }

            for (int i = 0; i < Waypoints.Count; i++)
            {
                if (UseRelativeOffset)
                {
                    _worldWaypoints.Add(_initialPosition + Waypoints[i]);
                }
                else
                {
                    _worldWaypoints.Add(Waypoints[i]);
                }
            }
        }

        /// <summary>
        /// 向当前目标路径点移动（支持任意方向）
        /// </summary>
        protected virtual void MoveTowardsCurrentWaypoint()
        {
            if (_currentIndex >= _worldWaypoints.Count)
                return;

            Vector3 targetPosition = _worldWaypoints[_currentIndex];
            Vector3 currentPosition = _transform.position;

            // 计算到目标点的距离
            float distance = Vector3.Distance(currentPosition, targetPosition);

            if (distance <= MinDistanceToWaypoint)
            {
                // 到达路径点
                _transform.position = targetPosition;
                _currentSpeed = 0f;

                // 处理等待
                if (WaitTimeAtWaypoint > 0f)
                {
                    _isWaiting = true;
                    _waitTimer = WaitTimeAtWaypoint;
                }
                else
                {
                    MoveToNextWaypoint();
                }
            }
            else
            {
                // 向目标点移动（支持任意方向：水平、垂直、斜向）
                Vector3 newPosition = Vector3.MoveTowards(
                    currentPosition,
                    targetPosition,
                    PatrolSpeed * Time.deltaTime
                );

                // 应用移动
                if (_rigidbody2D != null && !_rigidbody2D.isKinematic)
                {
                    _rigidbody2D.MovePosition(newPosition);
                }
                else
                {
                    _transform.position = newPosition;
                }

                // 计算移动方向用于转身
                Vector3 moveDirection = (targetPosition - currentPosition).normalized;
                float moveX = moveDirection.x;

                // 自动转身（根据水平移动方向）
                if (AutoFlip && CurrentFlipMode != FlipMode.None)
                {
                    HandleFacingDirection(moveX);
                }

                // 计算当前速度（用于动画）
                _currentSpeed = PatrolSpeed;

                UpdateAnimatorParameters();
            }
        }

        /// <summary>
        /// 移动到下一个路径点
        /// </summary>
        protected virtual void MoveToNextWaypoint()
        {
            switch (CurrentPatrolMode)
            {
                case PatrolMode.Loop:
                    // 循环模式：到达最后一个点后回到第一个
                    _currentIndex++;
                    if (_currentIndex >= _worldWaypoints.Count)
                    {
                        _currentIndex = 0;
                    }
                    break;

                case PatrolMode.BackAndForth:
                    // 往返模式：到达边界后反向
                    _currentIndex += CurrentDirection;
                    if (_currentIndex <= 0)
                    {
                        _currentIndex = 0;
                        CurrentDirection = 1;
                    }
                    else if (_currentIndex >= _worldWaypoints.Count - 1)
                    {
                        _currentIndex = _worldWaypoints.Count - 1;
                        CurrentDirection = -1;
                    }
                    break;
            }

            CurrentWaypointIndex = _currentIndex;
        }

        /// <summary>
        /// 处理面朝方向
        /// </summary>
        protected virtual void HandleFacingDirection(float moveX)
        {
            if (Mathf.Abs(moveX) < 0.01f)
                return;

            bool shouldFaceRight = moveX > 0f;
            if (IsFacingRight != shouldFaceRight)
            {
                IsFacingRight = shouldFaceRight;
                ApplyFlip();
            }
        }

        /// <summary>
        /// 应用翻转
        /// </summary>
        protected virtual void ApplyFlip()
        {
            switch (CurrentFlipMode)
            {
                case FlipMode.SpriteFlipX:
                    if (_spriteRenderer != null)
                    {
                        _spriteRenderer.flipX = !IsFacingRight;
                    }
                    break;

                case FlipMode.ScaleX:
                    Vector3 scale = _transform.localScale;
                    scale.x = Mathf.Abs(scale.x) * (IsFacingRight ? 1f : -1f);
                    _transform.localScale = scale;
                    break;

                case FlipMode.None:
                    break;
            }
        }

        /// <summary>
        /// 更新 Animator 参数
        /// </summary>
        protected virtual void UpdateAnimatorParameters()
        {
            if (!UpdateAnimator || _animator == null)
                return;

            bool isWalking = _currentSpeed > 0.01f;

            _animator.SetFloat(_speedParameterHash, _currentSpeed);
            _animator.SetBool(_walkingParameterHash, isWalking);
            _animator.SetBool(_facingRightParameterHash, IsFacingRight);
        }

        #endregion

        #region Gizmos

        protected virtual void OnDrawGizmosSelected()
        {
            if (Waypoints == null || Waypoints.Count == 0)
                return;

            Vector3 origin = Application.isPlaying ? _initialPosition : transform.position;

            // BackAndForth 单点模式下绘制自动生成的起点
            if (CurrentPatrolMode == PatrolMode.BackAndForth && Waypoints.Count == 1)
            {
                Vector3 targetPos = UseRelativeOffset ? origin + Waypoints[0] : Waypoints[0];
                if (Vector3.Distance(origin, targetPos) > 0.01f)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(origin, 0.2f);
                    Gizmos.color = Color.blue;
                    Gizmos.DrawSphere(targetPos, 0.2f);
                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(origin, targetPos);
                    return;
                }
            }

            // 绘制路径点
            for (int i = 0; i < Waypoints.Count; i++)
            {
                Vector3 pointPos = UseRelativeOffset ? origin + Waypoints[i] : Waypoints[i];

                Gizmos.color = (i == CurrentWaypointIndex) ? Color.blue : Color.green;
                Gizmos.DrawSphere(pointPos, 0.2f);

                // 绘制连线
                if (i + 1 < Waypoints.Count)
                {
                    Vector3 nextPos = UseRelativeOffset ? origin + Waypoints[i + 1] : Waypoints[i + 1];
                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(pointPos, nextPos);
                }
                else if (CurrentPatrolMode == PatrolMode.Loop)
                {
                    Vector3 firstPos = UseRelativeOffset ? origin + Waypoints[0] : Waypoints[0];
                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(pointPos, firstPos);
                }
            }
        }

        #endregion
    }
}
