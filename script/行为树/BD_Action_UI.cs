using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;
using Tooltip = BehaviorDesigner.Runtime.Tasks.TooltipAttribute;
using DG.Tweening;
using UnityEngine.UI;

namespace PP
{
    [TaskCategory("PP/UI")]
    [TaskDescription("UI 基础控制：CanvasGroup、按钮、射线、渐变动画")]
    public class BD_Action_UI : Action
    {
        public enum ACTION_NAME
        {
            NULL,
            CANVAS_GROUP_TRANSISTION,
            TOGGLE_CANVAS_RAYCASTER,
            SHOW_CANVAS_GROUP,
            HIDE_CANVAS_GROUP,
            TOGGLE_CANVAS_GROUP,
            INTERACTABLE_CANVAS_GROUP,
            RAYCAST_CANVAS_GROUP,
            ENABLE_BUTTON,
            DISABLE_BUTTON,
            TOGGLE_BUTTON
        }

        public ACTION_NAME triggerAction;
        public RectTransform currentUI;
        public RectTransform targetUI;
        public bool enable;
        public TweenSetting tweenSetting = new TweenSetting();

        private CanvasGroup currentCG;
        private CanvasGroup targetCG;
        private GraphicRaycaster raycaster;
        private Button targetButton;

        private Tweener targetTweener;

        public override void OnStart()
        {
            if (targetUI == null)
            {
                Debug.LogError($"{FriendlyName} 未指定目标 UI 对象！");
                return;
            }

            // 自动获取组件
            targetCG = targetUI.GetComponent<CanvasGroup>();
            raycaster = targetUI.GetComponent<GraphicRaycaster>();
            targetButton = targetUI.GetComponent<Button>();

            if (triggerAction == ACTION_NAME.CANVAS_GROUP_TRANSISTION && currentUI != null)
            {
                currentCG = currentUI.GetComponent<CanvasGroup>();
            }

            CallAction();
        }

        public override TaskStatus OnUpdate()
        {
            return TaskStatus.Success;
        }

        private void CallAction()
        {
            switch (triggerAction)
            {
                case ACTION_NAME.CANVAS_GROUP_TRANSISTION:
                    if (targetCG == null) break;
                    targetCG.blocksRaycasts = true;
                    targetCG.interactable = true;
                    targetTweener = DOTween.To(() => targetCG.alpha, val => targetCG.alpha = val, 1f, tweenSetting.Duration)
                        .SetLink(Owner.gameObject)
                        .SetDelay(tweenSetting.Delay)
                        .SetEase(tweenSetting.EaseType);
                    break;

                case ACTION_NAME.TOGGLE_CANVAS_RAYCASTER:
                    if (raycaster != null) raycaster.enabled = !raycaster.enabled;
                    break;

                case ACTION_NAME.SHOW_CANVAS_GROUP:
                    if (targetCG == null) break;
                    if (enable)
                    {
                        targetCG.interactable = true;
                        targetCG.blocksRaycasts = true;
                    }
                    targetTweener = DOTween.To(() => targetCG.alpha, val => targetCG.alpha = val, 1f, tweenSetting.Duration)
                        .SetLink(Owner.gameObject)
                        .SetDelay(tweenSetting.Delay)
                        .SetLoops(tweenSetting.LoopCycle, tweenSetting.LoopType)
                        .SetEase(tweenSetting.EaseType);
                    break;

                case ACTION_NAME.HIDE_CANVAS_GROUP:
                    if (targetCG == null) break;
                    targetCG.interactable = false;
                    targetCG.blocksRaycasts = false;
                    targetTweener = DOTween.To(() => targetCG.alpha, val => targetCG.alpha = val, 0f, tweenSetting.Duration)
                        .SetLink(Owner.gameObject)
                        .SetDelay(tweenSetting.Delay)
                        .SetLoops(tweenSetting.LoopCycle, tweenSetting.LoopType)
                        .SetEase(tweenSetting.EaseType);
                    break;

                case ACTION_NAME.TOGGLE_CANVAS_GROUP:
                    if (targetCG == null) break;
                    Tweener toggleTweener;
                    if (targetCG.alpha == 0)
                    {
                        if (enable)
                        {
                            targetCG.interactable = true;
                            targetCG.blocksRaycasts = true;
                        }
                        toggleTweener = targetCG.DOFade(1f, tweenSetting.Duration);
                    }
                    else
                    {
                        if (enable)
                        {
                            targetCG.interactable = false;
                            targetCG.blocksRaycasts = false;
                        }
                        toggleTweener = targetCG.DOFade(0f, tweenSetting.Duration);
                    }
                    toggleTweener
                        .SetLink(Owner.gameObject)
                        .SetDelay(tweenSetting.Delay)
                        .SetLoops(tweenSetting.LoopCycle, tweenSetting.LoopType)
                        .SetEase(tweenSetting.EaseType);
                    break;

                case ACTION_NAME.INTERACTABLE_CANVAS_GROUP:
                    if (targetCG != null) targetCG.interactable = enable;
                    break;

                case ACTION_NAME.RAYCAST_CANVAS_GROUP:
                    if (targetCG != null) targetCG.blocksRaycasts = enable;
                    break;

                case ACTION_NAME.ENABLE_BUTTON:
                    if (targetButton != null) targetButton.interactable = true;
                    break;

                case ACTION_NAME.DISABLE_BUTTON:
                    if (targetButton != null) targetButton.interactable = false;
                    break;

                case ACTION_NAME.TOGGLE_BUTTON:
                    if (targetButton != null) targetButton.interactable = !targetButton.interactable;
                    break;
            }
        }
    }

    
}