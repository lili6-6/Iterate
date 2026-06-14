using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using Tooltip = BehaviorDesigner.Runtime.Tasks.TooltipAttribute;
using UnityEngine.Video;

namespace PP
{
    [TaskCategory("PP/Video")]
    [TaskDescription("视频播放控制：播放、暂停、停止")]
    public class BD_Action_Video : Action
    {
        public enum ACTION_NAME
        {
            NULL,
            PLAY_VIDEO,
            STOP_VIDEO,
            PAUSE_VIDEO
        }

        public ACTION_NAME triggerAction;

        [Tooltip("视频播放器")]
        public VideoPlayer Player;

        [Tooltip("要播放的视频片段")]
        public VideoClip Clip;

        [Tooltip("是否循环播放")]
        public bool Loop;

        public override void OnStart()
        {
            if (Player == null)
            {
                Debug.LogError($"{FriendlyName} 未指定 VideoPlayer！");
                return;
            }

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
                case ACTION_NAME.PLAY_VIDEO:
                    PlayLocalVideo();
                    break;

                case ACTION_NAME.STOP_VIDEO:
                    StopLocalVideo();
                    break;

                case ACTION_NAME.PAUSE_VIDEO:
                    PauseLocalVideo();
                    break;
            }
        }

        private void PlayLocalVideo()
        {
            if (Clip != null)
                Player.clip = Clip;

            Player.isLooping = Loop;
            Player.Play();
        }

        private void StopLocalVideo()
        {
            Player.Stop();
        }

        private void PauseLocalVideo()
        {
            Player.Pause();
        }
    }
}