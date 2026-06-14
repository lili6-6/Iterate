using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

namespace PP
{
    /// <summary>
    /// 平台脚步声脚本
    /// 功能：
    /// 1. 支持不同类型的平台（通过碰撞检测）
    /// 2. 检测玩家是否接触平台
    /// 3. 检测玩家是否在移动
    /// 4. 定义AudioSource列表
    /// 5. 接触平台且移动时随机播放音效
    /// 6. 停止移动时立即停止音效
    /// </summary>
    public class PlatformFootstep : MonoBehaviour
    {
        [Header("音效列表")]
        [Tooltip("脚步声音效列表")]
        [SerializeField] private List<AudioSource> footstepSounds = new List<AudioSource>();

        [Header("移动检测")]
        [Tooltip("玩家移动速度阈值，低于此值认为没有移动")]
        [SerializeField] private float moveThreshold = 0.1f;

        [Tooltip("脚步播放间隔")]
        [SerializeField] private float footstepInterval = 0.3f;

        [SerializeField] private UnityEvent onPlayerOnPlatform; // 玩家所在的层
        [Header("调试")]
        [SerializeField] private bool debugMode = true;

        private bool isPlayerOnPlatform = false;
        private bool isPlayerMoving = false;
        private float lastFootstepTime = 0f;
        private AudioSource currentPlayingSound = null;
        private Transform playerTransform;
        private Rigidbody2D playerRigidbody;
        private int updateCallCount = 0;
        private Vector3 lastPlayerPosition;
        private bool hasRecordedLastPosition = false;

        private void Start()
        {
            // 初始化时确保所有音效不播放
            foreach (var sound in footstepSounds)
            {
                if (sound != null)
                {
                    sound.Stop();
                    sound.playOnAwake = false;
                }
            }
        }

        private void Update()
        {
            updateCallCount++;
            
            // 每次 Update 都输出状态（仅在 debugMode 时）
            // if (debugMode && updateCallCount % 60 == 0) // 每秒约1次（60帧）
            // {
            //     Debug.Log($"[PlatformFootstep] Update #{updateCallCount} | isPlayerOnPlatform={isPlayerOnPlatform} | playerTransform={(playerTransform != null ? "有" : "无")} | isPlayerMoving={isPlayerMoving}");
            // }

            if (!isPlayerOnPlatform || playerTransform == null)
                return;

            // 检测玩家是否在移动
            CheckPlayerMovement();

            // 如果玩家在移动且接触平台，播放脚步声
            if (isPlayerMoving && isPlayerOnPlatform)
            {
                TryPlayFootstep();
            }
            else if (!isPlayerMoving && currentPlayingSound != null)
            {
                // 停止移动时立即停止音效
                StopFootstep();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            //Debug.Log($"[PlatformFootstep] OnTriggerEnter2D 触发! 接触对象: {other.gameObject.name}, Tag: {other.gameObject.tag}");
            
            if (other.gameObject.CompareTag("Player"))
            {
                isPlayerOnPlatform = true;
                playerTransform = other.gameObject.transform;
                playerRigidbody = other.gameObject.GetComponent<Rigidbody2D>();
                onPlayerOnPlatform.Invoke();
                //Debug.Log($"[PlatformFootstep] 玩家进入平台(Trigger): {gameObject.name} | Rigidbody2D: {(playerRigidbody != null ? "有" : "无")}");
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                isPlayerOnPlatform = true;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                isPlayerOnPlatform = false;
                isPlayerMoving = false;
                StopFootstep();
                playerTransform = null;
                playerRigidbody = null;

                //Debug.Log($"[PlatformFootstep] 玩家离开平台(Trigger): {gameObject.name}");
            }
        }

        /// <summary>
        /// 检测玩家是否在移动
        /// </summary>
        private void CheckPlayerMovement()
        {
            if (playerTransform != null)
            {
                // 第一次记录位置
                if (!hasRecordedLastPosition)
                {
                    lastPlayerPosition = playerTransform.position;
                    hasRecordedLastPosition = true;
                    isPlayerMoving = false;
                    return;
                }

                // 通过位置变化检测移动
                float positionDelta = (playerTransform.position - lastPlayerPosition).magnitude;
                lastPlayerPosition = playerTransform.position;
                
                isPlayerMoving = positionDelta > moveThreshold * Time.deltaTime * 60f;

                //Debug.Log($"[PlatformFootstep] 位置变化: {positionDelta:F4} (阈值: {moveThreshold * Time.deltaTime * 60f:F4}) | isPlayerMoving: {isPlayerMoving}");
            }
            else
            {
                //Debug.LogWarning("[PlatformFootstep] playerTransform 为空!");
                isPlayerMoving = false;
                hasRecordedLastPosition = false;
            }
        }

        /// <summary>
        /// 尝试播放脚步声
        /// </summary>
        private void TryPlayFootstep()
        {
            // 检查播放间隔
            if (Time.time - lastFootstepTime < footstepInterval)
                return;

            // 过滤出可用的音效源
            List<AudioSource> availableSounds = footstepSounds.FindAll(s => s != null && s.clip != null);

            if (availableSounds.Count == 0)
            {
                if (debugMode)
                    Debug.LogWarning("[PlatformFootstep] 没有可用的脚步声音效!");
                return;
            }

            // 如果只有一个音效源，让它连续播放（不频繁 Stop/Play）
            if (availableSounds.Count == 1)
            {
                AudioSource singleSound = availableSounds[0];
                // 如果当前没有在播放，才开始播放
                if (currentPlayingSound == null || !currentPlayingSound.isPlaying)
                {
                    if (currentPlayingSound != null)
                        currentPlayingSound.Stop();
                    currentPlayingSound = singleSound;
                    currentPlayingSound.Play();
                    lastFootstepTime = Time.time;
                    if (debugMode)
                        Debug.Log($"[PlatformFootstep] 播放脚步声: {currentPlayingSound.clip.name}");
                }
                return;
            }

            // 多个音效时随机选择
            // 停止当前播放的音效
            if (currentPlayingSound != null)
            {
                currentPlayingSound.Stop();
            }

            // 随机选择一个音效并播放
            int randomIndex = Random.Range(0, availableSounds.Count);
            currentPlayingSound = availableSounds[randomIndex];
            currentPlayingSound.Play();

            lastFootstepTime = Time.time;

            if (debugMode)
                Debug.Log($"[PlatformFootstep] 播放脚步声: {currentPlayingSound.clip.name}");
        }

        /// <summary>
        /// 停止脚步声
        /// </summary>
        private void StopFootstep()
        {
            if (currentPlayingSound != null)
            {
                currentPlayingSound.Stop();
                currentPlayingSound = null;

                //if (debugMode)
                    //Debug.Log("[PlatformFootstep] 停止脚步声");
            }
        }

        /// <summary>
        /// 添加脚步声音效
        /// </summary>
        public void AddFootstepSound(AudioSource sound)
        {
            if (sound != null && !footstepSounds.Contains(sound))
            {
                footstepSounds.Add(sound);
            }
        }

        /// <summary>
        /// 移除脚步声音效
        /// </summary>
        public void RemoveFootstepSound(AudioSource sound)
        {
            if (sound != null)
            {
                footstepSounds.Remove(sound);
            }
        }
    }
}
