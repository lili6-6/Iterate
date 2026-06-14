using UnityEngine;
using MoreMountains.Tools;
using MoreMountains.CorgiEngine;

namespace PP
{
    /// <summary>
    /// 前方障碍物检测 Decision。
    /// 使用 Corgi 的 CorgiController 检测角色当前面朝方向是否有障碍物（墙壁）。
    /// 可选择是否检测悬崖（前方地面缺失）。
    /// </summary>
    [AddComponentMenu("PP/AI/Decisions/AI Decision PP Obstacle Ahead")]
    public class AIDecisionPPObstacleAhead : AIDecision
    {
        [Header("Detection")]
        /// 是否检测墙壁碰撞
        [Tooltip("是否检测墙壁碰撞")]
        public bool DetectWalls = true;
        /// 是否检测悬崖（前方无地面）
        [Tooltip("是否检测悬崖（前方无地面）")]
        public bool DetectHoles = false;
        /// 悬崖检测射线偏移
        [Tooltip("悬崖检测射线偏移")]
        public Vector2 HoleDetectionOffset = new Vector2(0f, 0f);
        /// 悬崖检测射线的长度
        [Tooltip("悬崖检测射线的长度")]
        public float HoleDetectionRaycastLength = 1f;
        /// 悬崖检测的层级
        [Tooltip("悬崖检测的层级（默认使用 PlatformMask）")]
        public LayerMask HoleDetectionMask;

        protected CorgiController _controller;
        protected Character _character;
        protected Vector2 _raycastOrigin;
        protected RaycastHit2D _raycastHit2D;

        /// <summary>
        /// 初始化时获取 CorgiController 和 Character 组件
        /// </summary>
        public override void Initialization()
        {
            _controller = GetComponentInParent<CorgiController>();
            _character = GetComponentInParent<Character>();

            if (HoleDetectionMask.value == 0 && _controller != null)
            {
                HoleDetectionMask = _controller.PlatformMask;
            }
        }

        /// <summary>
        /// 每帧判断前方是否有障碍物
        /// </summary>
        public override bool Decide()
        {
            bool result = DetectObstacle();
            //Debug.Log($"[AIDecisionPPObstacleAhead] Decide → {(result ? "有障碍物" : "无障碍物")} (面朝{( _character != null && _character.IsFacingRight ? "右" : "左" )})");
            return result;
        }

        /// <summary>
        /// 检测前方是否有障碍物
        /// </summary>
        protected virtual bool DetectObstacle()
        {
            if (_controller == null || _character == null) return false;

            // 检测墙壁
            if (DetectWalls)
            {
                bool wallDetected = false;
                if (_character.IsFacingRight)
                {
                    wallDetected = _controller.State.IsCollidingRight;
                }
                else
                {
                    wallDetected = _controller.State.IsCollidingLeft;
                }

                if (wallDetected)
                {
                    Debug.Log($"[AIDecisionPPObstacleAhead] 检测到墙壁！面朝{( _character.IsFacingRight ? "右" : "左" )}");
                    return true;
                }
            }

            // 检测悬崖
            if (DetectHoles && _controller.State.IsGrounded)
            {
                bool holeDetected = DetectHoleAhead();
                if (holeDetected)
                {
                    Debug.Log($"[AIDecisionPPObstacleAhead] 检测到悬崖！面朝{( _character.IsFacingRight ? "右" : "左" )}");
                }
                return holeDetected;
            }

            return false;
        }

        /// <summary>
        /// 检测前方是否有悬崖（地面缺失）
        /// </summary>
        protected virtual bool DetectHoleAhead()
        {
            // 计算射线起点：角色面朝方向的边缘底部
            if (_character.IsFacingRight)
            {
                _raycastOrigin = transform.position 
                    + (_controller.Bounds.x / 2 + HoleDetectionOffset.x) * transform.right 
                    + HoleDetectionOffset.y * transform.up;
            }
            else
            {
                _raycastOrigin = transform.position 
                    - (_controller.Bounds.x / 2 + HoleDetectionOffset.x) * transform.right 
                    + HoleDetectionOffset.y * transform.up;
            }

            // 向下发射射线检测地面
            _raycastHit2D = MMDebug.RayCast(
                _raycastOrigin, 
                Vector2.down, 
                HoleDetectionRaycastLength, 
                HoleDetectionMask, 
                Color.red, 
                true
            );

            // 如果没有检测到地面 → 前方是悬崖
            return _raycastHit2D.collider == null;
        }
    }
}
