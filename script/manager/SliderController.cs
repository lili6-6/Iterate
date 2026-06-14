using UnityEngine;
using UnityEngine.UI;
using Michsky.MUIP;

namespace PP
{
    [RequireComponent(typeof(SliderManager))]
    public class SliderController : MonoBehaviour
    {
        private SliderManager sliderManager;
        public int CollectNum;

        void Awake()
        {
            sliderManager = GetComponent<SliderManager>();
        }

        void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterLearningSlider(sliderManager);
            }

                if (GameManager.Instance != null && sliderManager != null && sliderManager.mainSlider != null)
                {
                    sliderManager.mainSlider.value = GameManager.Instance.LearningDegree;
                    sliderManager.UpdateUI();
                    sliderManager.mainSlider.interactable = false; // 禁用用户交互，由 GameManager 控制
                }
        }

        void OnDestroy()
        {
                // 不再监听滑动事件，移除注册
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UnregisterLearningSlider(sliderManager);
            }
        }

        public void SyncFromGame()
        {
            if (GameManager.Instance != null && sliderManager != null && sliderManager.mainSlider != null)
            {
                sliderManager.mainSlider.value = GameManager.Instance.LearningDegree;
                sliderManager.UpdateUI();
            }
        }
    }
}
