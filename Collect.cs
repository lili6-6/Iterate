using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SocialPlatforms.Impl;


namespace PP
{
public class Collect : MonoBehaviour
{
    
    [SerializeField]public UnityEvent collectEvent;
    private float scoreValue = 1.0f;
    
    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float audioVolume = 1.0f;
    [SerializeField] private GameObject body;

    [Header("LevelUp Detection")]
    [SerializeField, Tooltip("玩家靠近到该距离内时播放 LevelUp 动画")]
    private float detectRange = 3.0f;
    
    private GameManager gameManager;
    private PlayerAnimation_EXT playerAnimExt;
    private bool wasPlayerInRange = false;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameManager=GameManager.Instance;
        // Setup audio source if assigned
        if (audioSource != null)
        {
            audioSource.volume = audioVolume;
            audioSource.playOnAwake = false;
        }

        // 缓存玩家身上的 PlayerAnimation_EXT 组件
        if (gameManager != null && gameManager.playerManager != null)
        {
            playerAnimExt = gameManager.playerManager.GetComponent<PlayerAnimation_EXT>();
        }
       scoreValue = GameManager.Instance.sliderController.CollectNum > 0 
    ? 1f / GameManager.Instance.sliderController.CollectNum 
    : 0f;
    }

    // Update is called once per frame
    void Update()
    {
        // 如果没有玩家引用，尝试获取
        if (playerAnimExt == null)
        {
            if (gameManager != null && gameManager.playerManager != null)
            {
                playerAnimExt = gameManager.playerManager.GetComponent<PlayerAnimation_EXT>();
            }
            if (playerAnimExt == null)
                return;
        }

        // 计算玩家与当前物体的距离
        float distance = Vector3.Distance(
            gameManager.playerManager.transform.position,
            transform.position
        );

        bool isInRange = distance <= detectRange;

        // 状态变化时才更新，避免每帧重复设置
        if (isInRange && !wasPlayerInRange)
        {
            // 玩家进入范围 → 开启 LevelUp 动画
            playerAnimExt.SetLevelUp(true);
            wasPlayerInRange = true;
            Debug.Log($"Player entered range ({detectRange} units). LevelUp ON.");
        }
        else if (!isInRange && wasPlayerInRange)
        {
            // 玩家离开范围 → 关闭 LevelUp 动画
            playerAnimExt.SetLevelUp(false);
            wasPlayerInRange = false;
            Debug.Log($"Player left range ({detectRange} units). LevelUp OFF.");
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Play collection sound
            if (audioSource != null )
            {
                GameManager.Instance.CollectAudio.Play();
                audioSource.Play();
            }
            
            
                Debug.Log("Collectible collected! Adding score.");
                gameManager=GameManager.Instance;
                gameManager.UpdateLearningDegree(scoreValue);

                // 若玩家仍在范围内，先关闭 LevelUp 动画，避免销毁后 Update 不再执行导致动画无法复位
                if (wasPlayerInRange && playerAnimExt != null)
                {
                    playerAnimExt.SetLevelUp(false);
                    wasPlayerInRange = false;
                    Debug.Log($"Collectible collected while player in range. LevelUp OFF.");
                }

                collectEvent.Invoke();
                body.SetActive(false); // Hide the collectible's body immediately
                this.GetComponent<Collider2D>().enabled = false; // Disable collider to prevent multiple triggers
                // Delay destruction to let audio finish playing
                if (audioSource != null && audioSource.clip != null)
                {
                    Destroy(gameObject, audioSource.clip.length-1f); // Destroy after audio finishes (subtracting a small buffer)
                }
                else
                {
                    Destroy(gameObject);
                }
            
            
        }
    }
}
}