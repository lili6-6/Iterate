using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;
using Tooltip = BehaviorDesigner.Runtime.Tasks.TooltipAttribute;
using UnityEngine.SceneManagement;

namespace PP
{
    [TaskCategory("PP/Scene")]
    [TaskDescription("场景管理行为：切换、加载、卸载场景")]
    public class BD_Action_Scene : Action
    {
        public enum ACTION_NAME
        {
            NULL,
            SWITCH_SCENE_SINGLE,
            LOAD_SCENE_MINIGAME,
            UNLOAD_SCENE_MINIGAME,
        }

        public ACTION_NAME triggerAction;
        public string targetSwitchScene;
        public string targetAdditiveMiniGameScene;
        public GameObject progressControllerPrefab;

        public override void OnStart()
        {
            CallAction();
        }

        public override TaskStatus OnUpdate()
        {
            return TaskStatus.Success;
        }

        private void CallAction()
        {
            switch (triggerAction)
            {
                case ACTION_NAME.SWITCH_SCENE_SINGLE:
                    if (!string.IsNullOrWhiteSpace(targetSwitchScene))
                    {
                        UnityEngine.SceneManagement.SceneManager.LoadScene(targetSwitchScene, LoadSceneMode.Single);
                    }
                    break;

                case ACTION_NAME.LOAD_SCENE_MINIGAME:
                    if (!string.IsNullOrWhiteSpace(targetAdditiveMiniGameScene))
                    {
                        UnityEngine.SceneManagement.SceneManager.LoadScene(targetAdditiveMiniGameScene, LoadSceneMode.Additive);
                    }
                    break;

                case ACTION_NAME.UNLOAD_SCENE_MINIGAME:
                    if (!string.IsNullOrWhiteSpace(targetAdditiveMiniGameScene))
                    {
                        UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(targetAdditiveMiniGameScene);
                    }
                    break;
            }
        }
    }
}