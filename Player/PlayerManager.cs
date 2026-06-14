using UnityEngine;
using DG.Tweening;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;

namespace PP
{
public class PlayerManager : MonoBehaviour
{
    
    //[SerializeField, Tooltip("延迟多久开始行为，单位秒。")]
    //public float maxDelay = 0f;

    [Header("CorgiEngine References")]
    [SerializeField] public Character character;
    [SerializeField] public AIBrain aiBrain;
    [SerializeField] public CharacterHorizontalMovement horizontalMovement;
    [SerializeField] public CharacterJump characterJump;

    [Header("AI Control")]
    public float aiDuration = 1f;
    public bool StopInput;

    [Header("State Icons")]
    [SerializeField] private SpriteRenderer hesitateSprite;
    [SerializeField] private SpriteRenderer resistanceSprite;
    [SerializeField] private Vector3 hesitateTargetScale = Vector3.one;
    [SerializeField] private Vector3 resistanceTargetScale = Vector3.one;
    [SerializeField] private float stateTweenDuration = 0.25f;

    private float random;
    private Coroutine currentAICoroutine = null;
    private GameManager gameManager;
    private VolumeManager volumeManager;
    private PlayerAnimation_EXT animExt;

    private float baseWalkSpeed;
    private float baseJumpHeight;
    private bool isFirstResistance = true;
    private bool previousHesitateState = false;
    private bool previousResistanceState = false;

    void Awake()
    {
        // 通过 GameManager 注册自己，确保全局可访问
        GameManager.Instance.playerManager = this;
    }

    void Start()
    {
        gameManager = GameManager.Instance;
        volumeManager = gameManager?.volumeManager;
        if (volumeManager == null)
        {
            volumeManager = FindFirstObjectByType<VolumeManager>();
            if (volumeManager != null)
            {
                Debug.Log("[PlayerManager] Found VolumeManager by FindObjectOfType.");
            }
        }

        // 自动获取组件（如果未在 Inspector 中指定）
        if (character == null) character = GetComponent<Character>();
        if (aiBrain == null) aiBrain = GetComponent<AIBrain>();
        if (horizontalMovement == null) horizontalMovement = GetComponent<CharacterHorizontalMovement>();
        if (characterJump == null) characterJump = GetComponent<CharacterJump>();

        baseWalkSpeed = horizontalMovement != null ? horizontalMovement.WalkSpeed : 0f;
        baseJumpHeight = characterJump != null ? characterJump.JumpHeight : 0f;

        // 初始状态：AIBrain 禁用，由玩家控制
        if (aiBrain != null)
        {
            aiBrain.BrainActive = false;
            aiBrain.enabled = false;
        }

        // 修复：AIBrain.Start() 中的 ResetBrainOnStart 可能在 PlayerManager.Start()
        // 之前执行，导致 AIActionPPIdle.OnEnterState() 设置了 Hesitate=true。
        // 这里显式重置动画状态，确保启动时不会残留犹豫/反抗动画。
        animExt = GetComponent<PlayerAnimation_EXT>();
        if (animExt != null)
        {
            animExt.SetHesitate(false);
            animExt.SetResistance(false);
        }

        ResetStateSprites();
    }

    void Update()
    {
        // 攀爬动画由 CorgiEngine 的 Character 系统自动处理
        UpdateStateSprites();
    }

    
    public void refreshPlayer(float learningDegree)
    {
        if (horizontalMovement != null)
        {
            horizontalMovement.WalkSpeed = baseWalkSpeed * (1f + learningDegree);
            horizontalMovement.ResetHorizontalSpeed(); // 同步 MovementSpeed，使修改立即生效
        }

        if (characterJump != null)
        {
            characterJump.JumpHeight = baseJumpHeight * (1f + learningDegree);
        }

        //this.GetComponent<PlayerAnimation_EXT>().SetLevelUp(true);
    }

    /// <summary>
    /// 移动选择：direction=0 表示停止，直接执行不触发AI概率。
    /// direction!=0 时根据 ResistanceDegree 概率决定是否由AI接管。
    /// </summary>
    public void MoveSelect(int direction)
    {
        // direction == 0 表示松开按键停止移动，不触发AI概率判定
        if (direction == 0)
        {
            if (horizontalMovement != null)
                horizontalMovement.SetHorizontalMove(0f);
            return;
        }

        // 如果已经在AI控制中，忽略玩家输入
        if (StopInput) return;

        random = Random.Range(0f, 1f);
        if (random < gameManager.ResistanceDegree)
        {
            Debug.Log($"AI takes over movement. random={random} < ResistanceDegree={gameManager.ResistanceDegree}");
            StartAIControl(aiDuration);
        }
        else
        {
            if (horizontalMovement != null)
                horizontalMovement.SetHorizontalMove(direction);
        }
    }

    /// <summary>
    /// 跳跃选择：根据 ResistanceDegree 概率决定是否由AI接管。
    /// </summary>
    public void JumpSelect()
    {
        // 如果已经在AI控制中，忽略玩家输入
        if (StopInput) return;

        random = Random.Range(0f, 1f);
        if (random < gameManager.ResistanceDegree)
        {
            Debug.Log($"AI takes over jump. random={random} < ResistanceDegree={gameManager.ResistanceDegree}");
            StartAIControl(aiDuration);
        }
        else
        {
            if (characterJump != null)
                characterJump.JumpStart();
        }
    }

    /// <summary>
    /// 启动AI控制，确保不会重复启动多个协程。
    /// </summary>
    private void StartAIControl(float duration)
    {
        if(isFirstResistance)
        {
            isFirstResistance = false;
            gameManager.FirstResistance?.Invoke();
        }
        // 如果已有AI协程在运行，不重复启动
        if (currentAICoroutine != null)
        {
            StopCoroutine(currentAICoroutine);
        }
        currentAICoroutine = StartCoroutine(AIPlayer(duration));
    }

    private System.Collections.IEnumerator AIPlayer(float duration)
    {
        if (volumeManager != null)
        {
            volumeManager.TriggerGlitch(gameManager.ResistanceDegree);
        }
        else
        {
            Debug.LogWarning("[PlayerManager] VolumeManager is null, cannot trigger glitch.");
        }

        StopInput = true;

        // 停止玩家当前移动
        if (horizontalMovement != null)
            horizontalMovement.SetHorizontalMove(0f);

        // 启用 AIBrain
        if (aiBrain != null)
        {
            aiBrain.BrainActive = true;
            aiBrain.enabled = true;
            aiBrain.ResetBrain();
        }

        float endTime = Time.time + duration;
        while (Time.time < endTime || (animExt != null && (animExt.IsHesitating || animExt.IsResisting)))
        {
            yield return null;
        }

        // 禁用 AIBrain
        if (aiBrain != null)
        {
            aiBrain.BrainActive = false;
            aiBrain.enabled = false;
        }

        // 修复：AIBrain 被禁用后，AIActionPPIdle.OnExitState() 不会被调用，
        // 导致 Resistance/Hesitate 动画状态无法复位。这里显式重置所有动画状态。
        if (animExt != null)
        {
            animExt.SetHesitate(false);
            animExt.SetResistance(false);
        }

        // 反抗结束后立即停止 Glitch 效果，避免持续循环触发
        if (volumeManager != null)
        {
            volumeManager.ResetGlitch();
        }

        StopInput = false;
        currentAICoroutine = null;
    }

    private void UpdateStateSprites()
    {
        if (animExt == null)
            return;

        var currentHesitate = animExt.IsHesitating;
        var currentResistance = animExt.IsResisting;

        if (currentHesitate != previousHesitateState)
        {
            AnimateStateSprite(hesitateSprite, currentHesitate ? hesitateTargetScale : Vector3.zero, stateTweenDuration);
            previousHesitateState = currentHesitate;
        }

        if (currentResistance != previousResistanceState)
        {
            AnimateStateSprite(resistanceSprite, currentResistance ? resistanceTargetScale : Vector3.zero, stateTweenDuration);
            previousResistanceState = currentResistance;
        }
    }

    private void ResetStateSprites()
    {
        previousHesitateState = false;
        previousResistanceState = false;
        SetSpriteScaleImmediately(hesitateSprite, Vector3.zero);
        SetSpriteScaleImmediately(resistanceSprite, Vector3.zero);
    }

    private void SetSpriteScaleImmediately(SpriteRenderer sprite, Vector3 scale)
    {
        if (sprite == null)
            return;

        sprite.transform.localScale = scale;
    }

    private void AnimateStateSprite(SpriteRenderer sprite, Vector3 targetScale, float duration)
    {
        if (sprite == null)
            return;

        sprite.transform.DOKill();
        sprite.transform.DOScale(targetScale, duration).SetEase(Ease.OutBack);
    }

    public void Stop()
    {
        // CorgiEngine 的 Character 系统会自动处理状态切换
        // 这个方法保留供外部调用，但不再需要手动设置动画
    }
}
}
