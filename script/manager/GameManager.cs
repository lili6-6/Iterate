using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.CorgiEngine;
using Michsky.MUIP;
using UnityEngine.Events;

namespace PP
{
public class GameManager : MonoBehaviour
{
    [Range(0,1)]
    [SerializeField] public float LearningDegree = 0;
    [Range(0,1)]
    [SerializeField] public float ResistanceDegree = 0;
    [SerializeField]private LevelDate levelDate;
    [SerializeField]public PlayerManager playerManager;
    [SerializeField]public VolumeManager volumeManager;
    [SerializeField]public SliderController sliderController;
    [SerializeField]public AudioSource CollectAudio;
    private SliderManager learningSlider;
    public static GameManager Instance { get; private set; }
    [Header("Events")]
    public UnityEvent FirstResistance;

    /// <summary>
    /// 当前是否处于暂停状态。
    /// </summary>
    public bool IsPaused { get; private set; } = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        GlobalManager.Instance.registManager(this);
        updateLevel();
    }

    // Update is called once per frame
    void Update()
    {
         
    }
    private void updateLevel()
    {
        
            if(levelDate == null)
            {
                Debug.Log("[GameManager] LevelDate 未设置！");
                return;
            }
            LevelDate currentLevel = levelDate;
            ResistanceDegree = currentLevel.ResistanceDegree;
            playerManager.aiDuration = currentLevel.aiDuration;

            // 通过 CorgiEngine 的 CharacterHorizontalMovement 设置移动速度
            if (playerManager.horizontalMovement != null)
            {
                playerManager.horizontalMovement.WalkSpeed = Mathf.Max(0f, currentLevel.moveSpeed);
            }

            // 通过 CorgiEngine 的 CharacterJump 设置跳跃高度
            if (playerManager.characterJump != null)
            {
                playerManager.characterJump.JumpHeight = Mathf.Max(0f, currentLevel.jumpForce);
            }
            
            playerManager.GetComponent<AIActionPPIdle>().MinHesitateDuration = currentLevel.minaiHesitation;
            playerManager.GetComponent<AIActionPPIdle>().MaxHesitateDuration = currentLevel.maxaiHesitation;
        
             
    }
    public void UpdateLearningDegree(float amount)
    {
        SetLearningDegree(LearningDegree + amount);
    }

    public void SetLearningDegree(float value)
    {
        LearningDegree = Mathf.Clamp01(value);
        playerManager.refreshPlayer(LearningDegree);

        if (learningSlider != null && learningSlider.mainSlider != null)
        {
            Debug.Log($"[GameManager] Updating Learning Slider: {LearningDegree}");
            learningSlider.mainSlider.value = LearningDegree;
            learningSlider.UpdateUI();
        }
    }

    public void RegisterLearningSlider(SliderManager slider)
    {
        learningSlider = slider;
        if (learningSlider != null && learningSlider.mainSlider != null)
        {
            learningSlider.mainSlider.value = LearningDegree;
            learningSlider.UpdateUI();
        }
    }

    public void UnregisterLearningSlider(SliderManager slider)
    {
        if (learningSlider == slider) learningSlider = null;
    }

    /// <summary>
    /// 暂停游戏。将 Time.timeScale 设为 0，停止所有时间相关逻辑。
    /// </summary>
    private void PauseGame()
    {
        if (IsPaused) return;
        IsPaused = true;
        Time.timeScale = 0f;
        volumeManager.FadeInPauseVolume(); // 渐入暂停后音效
        Debug.Log("[GameManager] 游戏已暂停");
    }

    /// <summary>
    /// 继续游戏。将 Time.timeScale 恢复为 1。
    /// </summary>
    private void ResumeGame()
    {
        if (!IsPaused) return;
        IsPaused = false;
        Time.timeScale = 1f;
        volumeManager.FadeOutPauseVolume(); // 渐出暂停后音效
        Debug.Log("[GameManager] 游戏已继续");
    }

    /// <summary>
    /// 切换暂停/继续状态。
    /// </summary>
    public void TogglePause()
    {
        if (IsPaused)
            ResumeGame();
        else
            PauseGame();
    }
    public void UpdateLevelLock(string targetLevelName)
    {
        GlobalManager.Instance.sceneManager.UnlockLevel(targetLevelName);
            
        
    }
}
}
