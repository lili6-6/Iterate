using PP;
using UnityEngine;
using UnityEngine.Events;
namespace pp{



public class Character_AI : MonoBehaviour
{
    [Header("Player Detection")]
    public string playerTag = "Player";
    public float detectionRadius = 3f;
    public bool useTrigger = true;

    [Header("AI Components")]
    public AI_Animation aiAnimation;
    public DialogueUI dialogueUI;
    public AI_Communication aiCommunication;
    [SerializeField] private UnityEvent onPlayerApproach;

    // [Header("Aliyun Bailian API")]
    // [SerializeField] private string apiKey = "sk-xxx";
    // [SerializeField] private string appId = "your_app_id";

    private bool playerNearby;
    private bool isAwaitingResponse;
    private bool lockedPlayerInputByAI;
    private bool isReplyHoldActive;
    private string lastAIResponseText;
    private Coroutine replyHoldCoroutine;
    private Transform playerTransform;

    private void Start()
    {
        if (aiAnimation == null)
        {
            aiAnimation = GetComponent<AI_Animation>();
            aiAnimation.SetState(AI_Animation.AIState.Idle);
        }

        if (dialogueUI != null)
        {
            if (dialogueUI.hideOnStart)
            {
                dialogueUI.SetVisible(false);
            }
            dialogueUI.characterAI = this;
        }

        if (aiCommunication != null)
        {
            // 配置 AI_Communication �?API 信息
            // aiCommunication.apiKey = apiKey;
            // aiCommunication.appId = appId;
            aiCommunication.OnResponseReceived += HandleAIResponse;
        }
    }

    private void Update()
    {
        FindPlayerTransformIfNeeded();

        if (!useTrigger)
        {
            CheckDistanceApproach();
            return;
        }

        if (playerTransform == null)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        if (!playerNearby && distance <= detectionRadius)
        {
            OnPlayerApproach();
        }
        else if (playerNearby && distance > detectionRadius)
        {
            OnPlayerLeave();
        }
    }

    private void FindPlayerTransformIfNeeded()
    {
        if (playerTransform != null)
        {
            return;
        }

        GameObject player = GameManager.Instance.playerManager.gameObject;
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void CheckDistanceApproach()
    {
        if (playerTransform == null)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        if (distance <= detectionRadius)
        {
            if (!playerNearby)
            {
                OnPlayerApproach();
            }
        }
        else
        {
            if (playerNearby)
            {
                OnPlayerLeave();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!useTrigger || !other.CompareTag(playerTag))
        {
            return;
        }

        OnPlayerApproach();
        onPlayerApproach?.Invoke();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!useTrigger || !other.CompareTag(playerTag))
        {
            return;
        }

        OnPlayerLeave();
    }

    public void OnPlayerApproach()
    {
        playerNearby = true;
        aiAnimation?.SetState(AI_Animation.AIState.Approach);
        RefreshDialogueUI();
    }

    public void OnPlayerLeave()
    {
        playerNearby = false;
        aiAnimation?.SetState(AI_Animation.AIState.Idle);
        RefreshDialogueUI();
    }

    public void ProcessPlayerPrompt(string prompt)
    {
        if (aiCommunication == null)
        {
            dialogueUI?.SetOutputText("AI 通信组件未配置�?);
            return;
        }

        isAwaitingResponse = true;
        isReplyHoldActive = false;
        StopReplyHoldCoroutine();
        SetPlayerInputLocked(true);
        aiAnimation?.SetState(AI_Animation.AIState.Thinking);
        RefreshDialogueUI();
        aiCommunication.RequestResponse(prompt);
    }

    private void HandleAIResponse(string response)
    {
        isAwaitingResponse = false;
        isReplyHoldActive = true;
        lastAIResponseText = response;
        aiAnimation?.SetState(AI_Animation.AIState.Reply);
        RefreshDialogueUI();
        StopReplyHoldCoroutine();
        replyHoldCoroutine = StartCoroutine(ReplyHoldRoutine());
    }

    private void SetPlayerInputLocked(bool locked)
    {
        if (GameManager.Instance?.playerManager != null)
        {
            if (locked)
            {
                if (!GameManager.Instance.playerManager.StopInput)
                {
                    lockedPlayerInputByAI = true;
                    GameManager.Instance.playerManager.StopInput = true;
                }
            }
            else if (lockedPlayerInputByAI)
            {
                GameManager.Instance.playerManager.StopInput = false;
                lockedPlayerInputByAI = false;
            }
        }

        if (dialogueUI != null)
        {
            dialogueUI.SetInputLocked(locked);
        }
    }

    private void RefreshDialogueUI()
    {
        if (dialogueUI == null)
            return;

        if (isAwaitingResponse)
        {
            dialogueUI.SetVisible(true);
            dialogueUI.ShowReplyMode(dialogueUI.waitingText);
            SetPlayerInputLocked(true);
            return;
        }

        if (isReplyHoldActive)
        {
            dialogueUI.SetVisible(true);
            dialogueUI.ShowReplyMode(lastAIResponseText);
            SetPlayerInputLocked(true);
            return;
        }

        SetPlayerInputLocked(false);

        if (playerNearby)
        {
            dialogueUI.SetVisible(true);
            dialogueUI.ClearOutput();
            dialogueUI.ShowInputMode();
        }
        else
        {
            dialogueUI.SetVisible(false);
        }
    }

    private System.Collections.IEnumerator ReplyHoldRoutine()
    {
        yield return new WaitForSeconds(5f);

        isReplyHoldActive = false;
        replyHoldCoroutine = null;
        RefreshDialogueUI();
    }

    private void StopReplyHoldCoroutine()
    {
        if (replyHoldCoroutine != null)
        {
            StopCoroutine(replyHoldCoroutine);
            replyHoldCoroutine = null;
        }
    }

    private void OnDestroy()
    {
        StopReplyHoldCoroutine();
    }
}
}
