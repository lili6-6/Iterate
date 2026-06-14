using UnityEngine;
using System.Collections.Generic;

namespace PP
{
    /// <summary>
    /// 主菜单管理器，负责关卡选择、继续游戏等逻辑
    /// 数据通过 SceneManager 读写存档，本地 levelOrder 仅作为 UI 展示缓存
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        [Header("关卡配置")]
        [Tooltip("所有关卡数据列表（Inspector 中配置初始顺序），Start 时从 SceneManager 加载存档同步状态")]
        [SerializeField] private List<LevelSaveData> levelOrder = new List<LevelSaveData>();

        [Header("UI 引用")]
        [Tooltip("关卡选择容器，存放所有关卡按钮")]
        [SerializeField] private Transform levelCardContainer;

        // SceneManager 引用缓存
        private SceneManager _sceneManager;
        private SceneManager SceneManagerRef
        {
            get
            {
                if (_sceneManager == null)
                    _sceneManager = FindFirstObjectByType<SceneManager>();
                return _sceneManager;
            }
        }

        // ===== 初始化 =====

        private void Start()
        {
            // 从 SceneManager 加载存档，同步到 levelOrder
            LoadFromSceneManager();
            RefreshLevelUI();
        }

        /// <summary>
        /// 从 SceneManager 读取存档，更新 levelOrder 中每个关卡的解锁状态
        /// </summary>
        private void LoadFromSceneManager()
        {
            if (SceneManagerRef == null)
            {
                Debug.LogWarning("[MainMenuManager] SceneManager 未找到，使用 Inspector 默认配置");
                return;
            }

            // 让 SceneManager 从 PlayerPrefs 加载存档到内存
            SceneManagerRef.Load();

            // 获取存档中的关卡数据
            List<LevelSaveData> savedLevels = SceneManagerRef.GetAllLevelSaveData();
            if (savedLevels == null || savedLevels.Count == 0)
            {
                Debug.Log("[MainMenuManager] 无存档数据，使用 Inspector 默认配置");
                return;
            }

            // 按 levelOrder 的顺序，用存档数据更新 unlocked 状态
            for (int i = 0; i < levelOrder.Count; i++)
            {
                LevelSaveData saved = savedLevels.Find(l => l.levelName == levelOrder[i].levelName);
                if (saved != null)
                {
                    levelOrder[i].unlocked = saved.unlocked;
                }
            }

            Debug.Log($"[MainMenuManager] 从 SceneManager 同步存档完成，共 {levelOrder.Count} 个关卡");
        }

        // ===== UI 刷新 =====

        /// <summary>
        /// 根据 levelOrder 列表刷新所有关卡按钮的解锁/锁定状态
        /// </summary>
        public void RefreshLevelUI()
        {
            if (levelCardContainer == null) return;

            for (int i = 0; i < levelCardContainer.childCount && i < levelOrder.Count; i++)
            {
                GameObject card = levelCardContainer.GetChild(i).gameObject;
                bool unlocked = levelOrder[i].unlocked;
                LevelCard levelCard = card.GetComponent<LevelCard>();
                // 假设第一个子物体是锁定图标，解锁时隐藏
                if (card.transform.childCount > 0)
                {
                    levelCard.LockImage.gameObject.SetActive(!unlocked);
                }
                levelCard.LevelNameText.gameObject.SetActive(unlocked);
            }
        }

        // ===== 关卡解锁（委托给 SceneManager） =====

        /// <summary>
        /// 解锁指定关卡（委托给 SceneManager.UnlockLevel，后者会自动 Save）
        /// </summary>
        /// <param name="levelName">要解锁的关卡名</param>
        public void UnlockLevel(string levelName)
        {
            if (string.IsNullOrWhiteSpace(levelName)) return;

            // 委托 SceneManager 解锁并保存
            SceneManagerRef?.UnlockLevel(levelName);

            // 同步更新本地 levelOrder
            LevelSaveData data = levelOrder.Find(l => l.levelName == levelName);
            if (data != null)
            {
                data.unlocked = true;
            }
            else
            {
                levelOrder.Add(new LevelSaveData(levelName, true));
            }

            RefreshLevelUI();
        }

        // ===== 公开方法（供 UI Button 绑定） =====

        /// <summary>
        /// 选择关卡并跳转（调用 SceneManager.SwitchScene）
        /// </summary>
        public void SelectLevel(string levelName)
        {
            if (string.IsNullOrWhiteSpace(levelName))
            {
                Debug.LogWarning("[MainMenuManager] 关卡名为空");
                return;
            }

            if (!IsLevelUnlocked(levelName))
            {
                Debug.Log($"[MainMenuManager] 关卡 {levelName} 未解锁");
                return;
            }

            Debug.Log($"[MainMenuManager] 选择关卡: {levelName}");
            SceneManagerRef?.SwitchScene(levelName);
        }

        /// <summary>
        /// 继续游戏（跳转到最新解锁的关卡）
        /// </summary>
        public void ContinueGame()
        {
            string latestLevel = SceneManagerRef?.GetLatestUnlockedLevel();
            if (string.IsNullOrWhiteSpace(latestLevel))
            {
                if (levelOrder.Count > 0)
                {
                    Debug.Log("[MainMenuManager] 无存档，从第一关开始");
                    SceneManagerRef?.SwitchScene(levelOrder[0].levelName);
                }
                return;
            }

            Debug.Log($"[MainMenuManager] 继续游戏 -> {latestLevel}");
            SceneManagerRef?.SwitchScene(latestLevel);
        }

        /// <summary>
        /// 新游戏（重置 SceneManager 存档，从第一关开始）
        /// </summary>
        public void NewGame()
        {
            // 重置 SceneManager 存档
            SceneManagerRef?.ResetAllProgress();

            // 同步重置本地 levelOrder
            foreach (LevelSaveData data in levelOrder)
            {
                data.unlocked = false;
            }

            if (levelOrder.Count > 0)
            {
                Debug.Log($"[MainMenuManager] 新游戏 -> {levelOrder[0].levelName}");
                SceneManagerRef?.SwitchScene(levelOrder[0].levelName);
            }
        }

        // ===== 查询方法（基于 levelOrder 列表） =====

        /// <summary>
        /// 获取指定关卡的解锁状态
        /// </summary>
        public bool IsLevelUnlocked(string levelName)
        {
            if (string.IsNullOrWhiteSpace(levelName)) return false;
            LevelSaveData data = levelOrder.Find(l => l.levelName == levelName);
            return data != null && data.unlocked;
        }

        /// <summary>
        /// 获取最新解锁的关卡名
        /// </summary>
        public string GetLatestUnlockedLevel()
        {
            for (int i = levelOrder.Count - 1; i >= 0; i--)
            {
                if (levelOrder[i].unlocked)
                    return levelOrder[i].levelName;
            }
            return null;
        }

        /// <summary>
        /// 获取 levelOrder 列表（供外部读取）
        /// </summary>
        public List<LevelSaveData> GetLevelOrder()
        {
            return levelOrder;
        }
        public void QuitGame()
        {
            GlobalManager.Instance.QuitGame();
        }
    }
}
