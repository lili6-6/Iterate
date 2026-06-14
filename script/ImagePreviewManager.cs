using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace PP
{
    /// <summary>
    /// 图片预览管理器：按顺序播放一组图片，带渐入渐出切换动画，
    /// 最后一张点击后触发 UnityEvent。
    /// </summary>
    public class ImagePreviewManager : MonoBehaviour
    {
        [Header("图片配置")]
        [Tooltip("预览图片列表，按顺序播放")]
        [SerializeField] private Sprite[] images;

        [Header("UI 引用")]
        [Tooltip("用于显示图片的 UI Image")]
        [SerializeField] private Image displayImage;

        [Tooltip("控制渐隐渐现的 CanvasGroup（如果没有会自动添加）")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("动画设置")]
        [Tooltip("淡入/淡出持续时间（秒）")]
        [SerializeField] private float fadeDuration = 0.5f;

        [Header("事件")]
        [Tooltip("所有图片播放完毕后触发的事件")]
        [SerializeField] private UnityEvent onAllImagesViewed;

        // 当前显示的图片索引
        private int _currentIndex = -1;
        // 是否正在播放过渡动画
        private bool _isTransitioning = false;
        // 是否已播放到最后一张并触发过事件
        private bool _hasTriggeredFinalEvent = false;

        private void Awake()
        {
            if (displayImage == null)
            {
                displayImage = GetComponent<Image>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        private void Start()
        {
            if (images == null || images.Length == 0)
            {
                Debug.LogWarning("[ImagePreviewManager] 图片列表为空，请配置图片。");
                return;
            }

            // 初始化显示第一张
            ShowImageImmediately(0);
        }

        private void Update()
        {
            // 支持鼠标点击/触摸点击切换下一张
            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                OnClickNext();
            }
        }

        /// <summary>
        /// 点击切换到下一张图片（可绑定到按钮或自动检测点击）
        /// </summary>
        public void OnClickNext()
        {
            if (_isTransitioning)
                return;

            if (images == null || images.Length == 0)
                return;

            // 已经是最后一张，触发事件
            if (_currentIndex >= images.Length - 1)
            {
                if (!_hasTriggeredFinalEvent)
                {
                    _hasTriggeredFinalEvent = true;
                    onAllImagesViewed?.Invoke();
                    Debug.Log("[ImagePreviewManager] 所有图片已播放完毕，触发 onAllImagesViewed 事件。");
                }
                return;
            }

            // 切换到下一张
            int nextIndex = _currentIndex + 1;
            StartCoroutine(FadeTransition(nextIndex));
        }

        /// <summary>
        /// 直接显示指定索引图片（无动画，用于初始化）
        /// </summary>
        private void ShowImageImmediately(int index)
        {
            if (index < 0 || index >= images.Length)
                return;

            _currentIndex = index;
            displayImage.sprite = images[index];
            canvasGroup.alpha = 1f;
        }

        /// <summary>
        /// 渐出 -> 换图 -> 渐入的协程动画
        /// </summary>
        private IEnumerator FadeTransition(int targetIndex)
        {
            _isTransitioning = true;

            // 1. 淡出
            yield return StartCoroutine(FadeAlpha(1f, 0f, fadeDuration));

            // 2. 切换图片
            _currentIndex = targetIndex;
            displayImage.sprite = images[targetIndex];

            // 3. 淡入
            yield return StartCoroutine(FadeAlpha(0f, 1f, fadeDuration));

            _isTransitioning = false;
        }

        /// <summary>
        /// Alpha 渐变协程
        /// </summary>
        private IEnumerator FadeAlpha(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }
            canvasGroup.alpha = to;
        }

        /// <summary>
        /// 重置到第一张，可重复播放
        /// </summary>
        public void ResetPreview()
        {
            StopAllCoroutines();
            _isTransitioning = false;
            _hasTriggeredFinalEvent = false;
            ShowImageImmediately(0);
        }

        /// <summary>
        /// 跳转到指定索引的图片（带动画）
        /// </summary>
        public void JumpToImage(int index)
        {
            if (_isTransitioning)
                return;

            if (index < 0 || index >= images.Length)
                return;

            if (index == _currentIndex)
                return;

            StartCoroutine(FadeTransition(index));
        }
    }
}
