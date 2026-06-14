using UnityEngine;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;

namespace PP
{
    /// <summary>
    /// PlayerAnimation_EXT 是 Corgi Engine 的 CharacterAbility 扩展，
    /// 为角色添加自定义动画参数：
    /// - LevelUp（升级动画，Bool）
    /// - Hesitate（犹豫状态，Bool）
    /// - Resistance（反抗状态，Bool）
    ///
    /// 用法：将此组件添加到角色 GameObject 上，Corgi Engine 的 Character 系统
    /// 会自动在每帧调用 InitializeAnimatorParameters() 和 UpdateAnimator()。
    /// 外部通过 SetLevelUp()、SetHesitate()、SetResistance() 控制状态。
    /// </summary>
    [AddComponentMenu("Corgi Engine/Character/Abilities/Player Animation EXT")]
    public class PlayerAnimation_EXT : CharacterAbility
    {
        public override string HelpBoxText()
        {
            return "为角色添加升级、犹豫、反抗等自定义动画状态。" +
                   "Animator 参数：LevelUp (Bool), Hesitate (Bool), Resistance (Bool)";
        }

        [Header("Animation State")]
        /// 是否正在播放升级动画
        [Tooltip("是否正在播放升级动画")]
        [SerializeField]
        private bool _isLevelUp = false;
        
        /// 是否处于犹豫状态
        [Tooltip("是否处于犹豫状态")]
        [SerializeField]
        private bool _isHesitate = false;

        /// 是否处于反抗状态
        [Tooltip("是否处于反抗状态")]
        [SerializeField]
        private bool _isResistance = false;

        // Animator 参数名称常量
        protected const string _levelUpAnimationParameterName = "LevelUp";
        protected const string _hesitateAnimationParameterName = "Hesitate";
        protected const string _resistanceAnimationParameterName = "Resistance";

        // Animator 参数哈希
        protected int _levelUpAnimationParameter;
        protected int _hesitateAnimationParameter;
        protected int _resistanceAnimationParameter;

        /// <summary>
        /// 设置升级动画状态（Bool，持续保持）
        /// </summary>
        /// <param name="state">true=播放升级动画，false=停止升级动画</param>
        public virtual void SetLevelUp(bool state)
        {
            if (!AbilityAuthorized)
                return;

            _isLevelUp = state;
            
            if (state)
            {
                AbilityStartFeedbacks?.PlayFeedbacks();
            }
        }

        /// <summary>
        /// 获取当前是否处于升级动画状态
        /// </summary>
        public virtual bool IsLevelingUp => _isLevelUp;

        /// <summary>
        /// 设置犹豫状态
        /// </summary>
        /// <param name="state">是否犹豫</param>
        public virtual void SetHesitate(bool state)
        {
            if (!AbilityAuthorized)
                return;

            _isHesitate = state;
        }

        /// <summary>
        /// 切换犹豫状态
        /// </summary>
        public virtual void ToggleHesitate()
        {
            _isHesitate = !_isHesitate;
        }

        /// <summary>
        /// 获取当前是否处于犹豫状态
        /// </summary>
        public virtual bool IsHesitating => _isHesitate;

        /// <summary>
        /// 设置反抗状态
        /// </summary>
        /// <param name="state">是否反抗</param>
        public virtual void SetResistance(bool state)
        {
            if (!AbilityAuthorized)
                return;

            _isResistance = state;
        }

        /// <summary>
        /// 切换反抗状态
        /// </summary>
        public virtual void ToggleResistance()
        {
            _isResistance = !_isResistance;
        }

        /// <summary>
        /// 获取当前是否处于反抗状态
        /// </summary>
        public virtual bool IsResisting => _isResistance;

        #region Animator

        /// <summary>
        /// 注册自定义动画参数到 Character 的 animator 参数列表
        /// </summary>
        protected override void InitializeAnimatorParameters()
        {
            RegisterAnimatorParameter(_levelUpAnimationParameterName, AnimatorControllerParameterType.Bool, out _levelUpAnimationParameter);
            RegisterAnimatorParameter(_hesitateAnimationParameterName, AnimatorControllerParameterType.Bool, out _hesitateAnimationParameter);
            RegisterAnimatorParameter(_resistanceAnimationParameterName, AnimatorControllerParameterType.Bool, out _resistanceAnimationParameter);
        }

        /// <summary>
        /// 每帧更新 animator 参数值
        /// 由 Character 类在每帧的 UpdateAnimators() 中自动调用
        /// </summary>
        public override void UpdateAnimator()
        {
            // 更新 LevelUp Bool（持续保持状态）
            MMAnimatorExtensions.UpdateAnimatorBool(_animator, _levelUpAnimationParameter, _isLevelUp, _character._animatorParameters, _character.PerformAnimatorSanityChecks);

            // 更新 Hesitate Bool
            MMAnimatorExtensions.UpdateAnimatorBool(_animator, _hesitateAnimationParameter, _isHesitate, _character._animatorParameters, _character.PerformAnimatorSanityChecks);

            // 更新 Resistance Bool
            MMAnimatorExtensions.UpdateAnimatorBool(_animator, _resistanceAnimationParameter, _isResistance, _character._animatorParameters, _character.PerformAnimatorSanityChecks);
        }

        #endregion

        #region Reset

        /// <summary>
        /// 角色复活时重置状态
        /// </summary>
        public override void ResetAbility()
        {
            base.ResetAbility();
            _isLevelUp = false;
            _isHesitate = false;
            _isResistance = false;
        }

        #endregion
    }
}
