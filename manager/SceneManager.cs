using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace PP
{
    /// <summary>
    /// 单个关卡的存档数据
    /// </summary>
    [System.Serializable]
    public class LevelSaveData
    {
        public string levelName;  // 关卡场景名
        public bool unlocked;     // 是否已解锁

        public LevelSaveData(string levelName, bool unlocked)
        {
            this.levelName = levelName;
            this.unlocked = unlocked;
        }
    }

    /// <summary>
    /// 整体存档数据
    /// </summary>
    [System.Serializable]
    public class SaveData
    {
        public List<LevelSaveData> levels = new List<LevelSaveData>();
        public string latestUnlockedLevel;  // 最新解锁的关卡名
    }

    public class SceneManager : MonoBehaviour
    {
        private const string SAVE_KEY = "PP_SaveData";

        // 运行时内存中的存档数据
        private SaveData _saveData;

        // ===== 存档管理 =====

        /// <summary>
        /// 保存当前存档到 PlayerPrefs
        /// </summary>
        public void Save()
        {
            if (_saveData == null)
            {
                Debug.LogWarning("[SceneManager] 没有数据可保存");
                return;
            }

            string json = JsonUtility.ToJson(_saveData);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
            Debug.Log($"[SceneManager] 存档已保存，共 {_saveData.levels.Count} 个关卡记录");
        }

        /// <summary>
        /// 从 PlayerPrefs 读取存档，加载到内存
        /// </summary>
        public void Load()
        {
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                string json = PlayerPrefs.GetString(SAVE_KEY);
                _saveData = JsonUtility.FromJson<SaveData>(json);
                Debug.Log($"[SceneManager] 存档已加载，共 {_saveData.levels.Count} 个关卡记录");
            }
            else
            {
                _saveData = new SaveData();
                Debug.Log("[SceneManager] 无存档，创建新存档数据");
            }
        }

        // ===== 关卡解锁管理 =====

        /// <summary>
        /// 关卡通关时调用，解锁指定关卡并更新最新解锁关卡
        /// </summary>
        /// <param name="levelName">关卡场景名称</param>
        public void UnlockLevel(string levelName)
        {
            if (string.IsNullOrWhiteSpace(levelName))
                return;

            // 确保存档已加载
            if (_saveData == null) Load();

            // 查找是否已有该关卡记录
            LevelSaveData existing = _saveData.levels.Find(l => l.levelName == levelName);
            if (existing != null)
            {
                if (!existing.unlocked)
                {
                    existing.unlocked = true;
                    Debug.Log($"[SceneManager] 关卡解锁: {levelName}");
                }
            }
            else
            {
                _saveData.levels.Add(new LevelSaveData(levelName, true));
                Debug.Log($"[SceneManager] 关卡解锁: {levelName}");
            }

            // 更新最新解锁关卡
            _saveData.latestUnlockedLevel = levelName;

            // 自动保存
            Save();
        }

        /// <summary>
        /// 检查指定关卡是否已解锁
        /// </summary>
        /// <param name="levelName">关卡场景名称</param>
        /// <returns>是否已解锁</returns>
        public bool IsLevelUnlocked(string levelName)
        {
            if (string.IsNullOrWhiteSpace(levelName))
                return false;

            if (_saveData == null) Load();

            LevelSaveData existing = _saveData.levels.Find(l => l.levelName == levelName);
            return existing != null && existing.unlocked;
        }

        /// <summary>
        /// 获取最新解锁的关卡名称
        /// </summary>
        /// <returns>最新解锁的关卡名，无存档时返回 null</returns>
        public string GetLatestUnlockedLevel()
        {
            if (_saveData == null) Load();
            return _saveData.latestUnlockedLevel;
        }

        /// <summary>
        /// 获取所有关卡存档数据（供主菜单读取每个关卡的解锁状态）
        /// </summary>
        /// <returns>关卡存档数据列表</returns>
        public List<LevelSaveData> GetAllLevelSaveData()
        {
            if (_saveData == null) Load();
            return _saveData.levels;
        }

        /// <summary>
        /// 重置所有存档
        /// </summary>
        public void ResetAllProgress()
        {
            _saveData = new SaveData();
            Save();
            Debug.Log("[SceneManager] 所有关卡进度已重置");
        }

        // ===== 场景跳转（与 BD_Action_Scene 行为一致） =====

        /// <summary>
        /// 切换场景（单场景模式），与 BD_Action_Scene.SWITCH_SCENE_SINGLE 一致
        /// </summary>
        /// <param name="sceneName">目标场景名称</param>
        public void SwitchScene(string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            }
        }

       
    }
}