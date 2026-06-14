using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;
using Tooltip = BehaviorDesigner.Runtime.Tasks.TooltipAttribute;
using DG.Tweening;

namespace PP
{
    [TaskCategory("PP/Transform")]
    [TaskDescription("Transform 变换行为：位置、旋转、缩放（支持DOTween渐变）")]
    public class BD_Action_Transform : BehaviorDesigner.Runtime.Tasks.Action
    {
        [Flags]
        public enum ACTION_TYPE
        {
            Null = 1,
            Scale = 2,
            Position = 4,
            Rotation = 8
        }

        public ACTION_TYPE action;
        public Transform target;
        public bool includeChildren;
        public bool notIncludeParent;
        public Vector2 randomizeChildrenDelay;
        public bool isWorldPosition;
        public bool usingOffsetInstead;
        public Vector3 targetPosition;
        public bool isWorldRotation;
        public Vector3 targetRotation;
        public Vector3 targetScale;
        public TweenSetting tweenSetting = new TweenSetting();

        private List<Transform> targetTransforms = new List<Transform>();
        private float delay;
        private Tweener positionTweener, rotationTweener;

        public override void OnStart()
        {
            if (target == null)
            {
                Debug.LogError($"{FriendlyName} 未指定目标 Transform！");
                return;
            }

            delay = tweenSetting.Delay;
            if (randomizeChildrenDelay.y > randomizeChildrenDelay.x)
            {
                delay = UnityEngine.Random.Range(randomizeChildrenDelay.x, randomizeChildrenDelay.y);
            }

            CallAction();
        }

        public override TaskStatus OnUpdate()
        {
            return TaskStatus.Success;
        }

        private void CallAction()
        {
            targetTransforms.Clear();
            Transform[] transforms = target.GetComponentsInChildren<Transform>(true);

            if (transforms != null && transforms.Length > 0)
            {
                if (includeChildren)
                {
                    targetTransforms = transforms.ToList();
                    if (notIncludeParent)
                    {
                        targetTransforms.Remove(target);
                    }
                }
                else
                {
                    targetTransforms.Add(transforms[0]);
                }
            }

            foreach (Transform t in targetTransforms)
            {
                if (t == null) continue;

                // 位置
                if (action.HasFlag(ACTION_TYPE.Position))
                {
                    Vector3 positionTo = usingOffsetInstead ? t.position + targetPosition : targetPosition;
                    if (isWorldPosition)
                    {
                        positionTweener = t.DOMove(positionTo, tweenSetting.Duration);
                    }
                    else
                    {
                        positionTweener = t.DOLocalMove(positionTo, tweenSetting.Duration);
                    }

                    positionTweener
                        .SetLink(Owner.gameObject)
                        .SetDelay(delay)
                        .SetLoops(tweenSetting.LoopCycle, tweenSetting.LoopType)
                        .SetEase(tweenSetting.EaseType);
                }

                // 旋转
                if (action.HasFlag(ACTION_TYPE.Rotation))
                {
                    if (isWorldRotation)
                    {
                        rotationTweener = t.DORotate(targetRotation, tweenSetting.Duration);
                    }
                    else
                    {
                        rotationTweener = t.DOLocalRotate(targetRotation, tweenSetting.Duration);
                    }

                    rotationTweener
                        .SetLink(Owner.gameObject)
                        .SetDelay(delay)
                        .SetLoops(tweenSetting.LoopCycle, tweenSetting.LoopType)
                        .SetEase(tweenSetting.EaseType);
                }

                // 缩放
                if (action.HasFlag(ACTION_TYPE.Scale))
                {
                    t.DOScale(targetScale, tweenSetting.Duration)
                        .SetLink(Owner.gameObject)
                        .SetDelay(delay)
                        .SetLoops(tweenSetting.LoopCycle, tweenSetting.LoopType)
                        .SetEase(tweenSetting.EaseType);
                }
            }
        }
    }

   
}