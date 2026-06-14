using System.Collections.Generic;
using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;
using Tooltip = BehaviorDesigner.Runtime.Tasks.TooltipAttribute;
using DG.Tweening;
using System.Linq;
using UnityEngine.UI;

namespace PP
{
    [TaskCategory("PP/Image")]
    [TaskDescription("图像变换行为（Sprite / UI Image）")]
    public class BD_Action_Image : Action
    {
        public enum ACTION_TYPE
        {
            Null,
            Sprite_Color,
            Sprite_Sort_Offset,
            Sprite_Mask_Range_Offset,
            Sprite_Sort_Layer,
            Image_Color,
            Image_Fill
        }

        public ACTION_TYPE action;
        public Transform target;
        public bool includeChildren;
        public Color targetColor;
        public int targetInt;
        public float targetFloat;
        public string targetString;
        public TweenSetting tweenSetting = new TweenSetting();

        private List<SpriteRenderer> targetSprites = new List<SpriteRenderer>();
        private List<Image> targetImages = new List<Image>();

        public override void OnStart()
        {
            if (target == null)
            {
                Debug.LogError($"{FriendlyName} 未指定目标对象！");
                return;
            }

            switch (action)
            {
                case ACTION_TYPE.Sprite_Color:
                case ACTION_TYPE.Sprite_Sort_Offset:
                case ACTION_TYPE.Sprite_Sort_Layer:
                    CallSpriteAction();
                    break;
                case ACTION_TYPE.Sprite_Mask_Range_Offset:
                    CallSpriteMaskAction();
                    break;
                case ACTION_TYPE.Image_Color:
                case ACTION_TYPE.Image_Fill:
                    CallImageAction();
                    break;
            }
        }

        public override TaskStatus OnUpdate()
        {
            return TaskStatus.Success;
        }

        private void CallSpriteAction()
        {
            SpriteRenderer[] sprites = target.GetComponentsInChildren<SpriteRenderer>(true);
            if (sprites == null || sprites.Length == 0) return;

            targetSprites.Clear();
            if (includeChildren)
                targetSprites = sprites.ToList();
            else
                targetSprites.Add(sprites[0]);

            foreach (SpriteRenderer sr in targetSprites)
            {
                if (sr == null) continue;

                switch (action)
                {
                    case ACTION_TYPE.Sprite_Color:
                        sr.DOColor(targetColor, tweenSetting.Duration)
                          .SetDelay(tweenSetting.Delay)
                          .SetLoops(tweenSetting.LoopCycle, tweenSetting.LoopType)
                          .SetEase(tweenSetting.EaseType);
                        break;
                    case ACTION_TYPE.Sprite_Sort_Offset:
                        sr.sortingOrder += targetInt;
                        break;
                    case ACTION_TYPE.Sprite_Sort_Layer:
                        if (!string.IsNullOrEmpty(targetString))
                            sr.sortingLayerName = targetString;
                        break;
                }
            }
        }

        private void CallSpriteMaskAction()
        {
            SpriteMask mask = target.GetComponent<SpriteMask>();
            if (mask == null) return;

            switch (action)
            {
                case ACTION_TYPE.Sprite_Mask_Range_Offset:
                    if (mask.isCustomRangeActive)
                    {
                        mask.frontSortingOrder += targetInt;
                        mask.backSortingOrder += targetInt;
                    }
                    break;
            }
        }

        private void CallImageAction()
        {
            Image[] images = target.GetComponentsInChildren<Image>(true);
            if (images == null || images.Length == 0) return;

            targetImages.Clear();
            if (includeChildren)
                targetImages = images.ToList();
            else
                targetImages.Add(images[0]);

            foreach (Image image in targetImages)
            {
                if (image == null) continue;

                Tweener tweener = null;
                switch (action)
                {
                    case ACTION_TYPE.Image_Color:
                        tweener = image.DOColor(targetColor, tweenSetting.Duration);
                        break;
                    case ACTION_TYPE.Image_Fill:
                        tweener = image.DOFillAmount(targetFloat, tweenSetting.Duration);
                        break;
                }

                if (tweener != null)
                {
                    tweener.SetDelay(tweenSetting.Delay)
                           .SetLoops(tweenSetting.LoopCycle, tweenSetting.LoopType)
                           .SetEase(tweenSetting.EaseType);
                }
            }
        }
    }

    [System.Serializable]
    public class TweenSetting
    {
        public float Duration = 0.2f;
        public float Delay;
        public int LoopCycle;
        public LoopType LoopType;
        public Ease EaseType = Ease.OutQuad;
    }
}