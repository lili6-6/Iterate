using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

namespace PP
{
    public class PlotController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup backgroundCG;
        [SerializeField] private CanvasGroup CG;
        [SerializeField] private TextMeshProUGUI dialogText;

        [Header("Dialog Data")]
        [SerializeField] private List<string> dialogList = new List<string>();

        [Header("Animation Settings")]
        [SerializeField] private float fadeDuration = 0.3f;
        [SerializeField] private Ease fadeEase = Ease.OutQuad;

        [Header("Background Click")]
        [SerializeField] private bool backgroundBlocksUIClicks = true;

        [Header("Options")]
        public bool StartOnAwake = false;
        public bool AutoPlot = true;
        public bool PauseOnPlot = true;
        public float autoInterval = 2f;
        [Header("Events")]
        public UnityEvent OnPlotStarted;
        public UnityEvent OnPlotCompleted;

        private int currentDialogIndex = 0;
        private bool isPlaying = false;
        private bool isTransitioning = false;

        public bool IsPausedDuringPlot { get; private set; } = false;

        private bool autoPlaying = false;

        private Tweener currentTweener;
        private Coroutine autoCoroutine;

        public bool IsPlaying => isPlaying;

        void Start()
        {
            if (StartOnAwake)
            {
                if (AutoPlot)
                    StartAutoPlot();
                else
                    StartPlot();
                OnPlotStarted?.Invoke();
            }
        }

        #region ================= 背景 =================

        private void EnsureBackgroundRaycastTarget()
        {
            if (backgroundCG == null) return;

            Image img = backgroundCG.GetComponent<Image>();
            if (img == null)
            {
                img = backgroundCG.gameObject.AddComponent<Image>();
                img.color = new Color(0, 0, 0, 0);
            }

            img.raycastTarget = backgroundBlocksUIClicks;
        }

        private void ApplyBackgroundClickSettings()
        {
            if (backgroundCG == null) return;

            backgroundCG.blocksRaycasts = backgroundBlocksUIClicks;
            backgroundCG.interactable = backgroundBlocksUIClicks;
        }

        public void ShowBackground()
        {
            KillTweener();

            EnsureBackgroundRaycastTarget();

            backgroundCG.gameObject.SetActive(true);
            backgroundCG.alpha = 0;
            ApplyBackgroundClickSettings();

            currentTweener = backgroundCG.DOFade(1, fadeDuration)
                .SetEase(fadeEase)
                .OnComplete(() =>
                {
                    // ❗只有手动模式且开启暂停才暂停游戏
                    if (!autoPlaying && PauseOnPlot)
                        PauseGame();
                });
        }

        public void HideBackground()
        {
            KillTweener();

            currentTweener = backgroundCG.DOFade(0, fadeDuration)
                .SetEase(fadeEase)
                .OnComplete(() =>
                {
                    backgroundCG.gameObject.SetActive(false);
                    backgroundCG.blocksRaycasts = false;
                    backgroundCG.interactable = false;

                    // ❗只有手动模式且开启暂停才恢复
                    if (!autoPlaying && PauseOnPlot)
                        ResumeGame();
                });
        }

        #endregion

        #region ================= 对话UI =================

        public void ShowPrefab()
        {
            KillTweener();

            CG.gameObject.SetActive(true);
            CG.alpha = 0;
            CG.blocksRaycasts = true;
            CG.interactable = true;

            currentTweener = CG.DOFade(1, fadeDuration)
                .SetEase(fadeEase)
                .OnComplete(() =>
                {
                    isTransitioning = false;
                });
        }

        public void HidePrefab()
        {
            KillTweener();

            currentTweener = CG.DOFade(0, fadeDuration)
                .SetEase(fadeEase)
                .OnComplete(() =>
                {
                    CG.gameObject.SetActive(false);
                    CG.blocksRaycasts = false;
                    CG.interactable = false;
                });
        }

        #endregion

        #region ================= 主流程 =================

        public void StartPlot()
        {
            if (dialogList == null || dialogList.Count == 0) return;
            if (isPlaying) return;

            StopAutoPlot();

            autoPlaying = false;
            isPlaying = true;
            isTransitioning = true;

            currentDialogIndex = 0;

            // 立即把首句文本设置好，避免显示已有的旧文本
            UpdateDialogText(currentDialogIndex);
            if (CG != null)
            {
                CG.gameObject.SetActive(false);
                CG.alpha = 0;
                CG.blocksRaycasts = false;
                CG.interactable = false;
            }

            ShowBackground();

            StartCoroutine(FirstDialogDelay());
        }

        public void StartAutoPlot()
        {
            if (dialogList == null || dialogList.Count == 0) return;
            if (isPlaying) return;

            StopAutoPlot();

            autoPlaying = true;
            isPlaying = true;
            isTransitioning = true;

            currentDialogIndex = 0;

            // 立即把首句文本设置好，避免显示已有的旧文本
            UpdateDialogText(currentDialogIndex);
            if (CG != null)
            {
                CG.gameObject.SetActive(false);
                CG.alpha = 0;
                CG.blocksRaycasts = false;
                CG.interactable = false;
            }

            ShowBackground();

            StartCoroutine(AutoStart());
        }

        private IEnumerator FirstDialogDelay()
        {
            yield return new WaitForSeconds(fadeDuration);

            UpdateDialogText(currentDialogIndex);
            ShowPrefab();
        }

        private IEnumerator AutoStart()
        {
            yield return new WaitForSeconds(fadeDuration);

            UpdateDialogText(currentDialogIndex);
            ShowPrefab();

            autoCoroutine = StartCoroutine(AutoLoop());
        }

        private IEnumerator AutoLoop()
        {
            while (isPlaying && autoPlaying)
            {
                yield return new WaitForSeconds(autoInterval);
                AdvanceDialog();
            }
        }

        private void AdvanceDialog()
        {
            if (!isPlaying || isTransitioning) return;

            isTransitioning = true;

            HidePrefab();

            int next = currentDialogIndex + 1;

            if (next >= dialogList.Count)
            {
                isPlaying = false;
                autoPlaying = false;
                StartCoroutine(EndDelay());
            }
            else
            {
                currentDialogIndex = next;
                StartCoroutine(NextDialogDelay());
            }
        }

        private IEnumerator NextDialogDelay()
        {
            yield return new WaitForSeconds(fadeDuration);

            UpdateDialogText(currentDialogIndex);
            ShowPrefab();
        }

        private IEnumerator EndDelay()
        {
            yield return new WaitForSeconds(fadeDuration);

            HideBackground();
            OnPlotCompleted?.Invoke();
        }

        public void OnClickNext()
        {
            // ❗Auto模式完全禁止点击
            if (autoPlaying) return;

            AdvanceDialog();
        }

        #endregion

        #region ================= 文本 =================

        private void UpdateDialogText(int index)
        {
            if (dialogText == null && CG != null)
                dialogText = CG.GetComponentInChildren<TextMeshProUGUI>();

            if (index < 0 || index >= dialogList.Count) return;

            dialogText.text = dialogList[index];
        }

        #endregion

        #region ================= 输入控制 =================

        private void PauseGame()
        {
            if (!PauseOnPlot || GameManager.Instance?.playerManager == null) return;

            GameManager.Instance.playerManager.StopInput = true;
            IsPausedDuringPlot = true;
        }

        private void ResumeGame()
        {
            if (!PauseOnPlot || GameManager.Instance?.playerManager == null) return;

            GameManager.Instance.playerManager.StopInput = false;
            IsPausedDuringPlot = false;
        }

        #endregion

        #region ================= 工具 =================

        private void KillTweener()
        {
            if (currentTweener != null && currentTweener.IsActive())
            {
                currentTweener.Kill();
                currentTweener = null;
            }
        }

        public void ForceStopPlot()
        {
            isPlaying = false;
            autoPlaying = false;
            isTransitioning = false;

            StopAutoPlot();
            StopAllCoroutines();
            KillTweener();

            if (CG != null)
            {
                CG.gameObject.SetActive(false);
                CG.alpha = 0;
            }

            if (backgroundCG != null)
            {
                backgroundCG.gameObject.SetActive(false);
                backgroundCG.alpha = 0;
                backgroundCG.blocksRaycasts = false;
                backgroundCG.interactable = false;
            }

            ResumeGame();
        }

        private void StopAutoPlot()
        {
            autoPlaying = false;

            if (autoCoroutine != null)
            {
                StopCoroutine(autoCoroutine);
                autoCoroutine = null;
            }
        }

        private void OnDestroy()
        {
            StopAutoPlot();
            KillTweener();
        }

        #endregion
    }
}
