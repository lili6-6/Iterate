using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;
using Unity.Cinemachine;

namespace PP {
  [TaskCategory("Custom/Camera")]
  [TaskDescription("仅修改 Cinemachine 相机 Priority 的行为")]
  public class BD_Action_Camera : Action {
    public enum ACTION_NAME {
      NULL,
      SET_TARGET_PRIORITY,
      SET_SEQUENCER_PRIORITY,
      RESET_ALL_PRIORITY
    }

    [Header("Action")]
    public ACTION_NAME triggerAction;

    [Header("Targets")]
    public CinemachineCamera targetCinemachine;
    public CinemachineSequencerCamera targetSequencer;

    [Header("Priority")]
    public int targetPriority = 100;
    public int resetPriority = 0;

    public override void OnStart() {
      if (triggerAction == ACTION_NAME.NULL) {
        Debug.LogError(FriendlyName + " 未选择任何相机行为！");
        return;
      }
      CallAction();
    }

    public override TaskStatus OnUpdate() {
      return TaskStatus.Success;
    }

    private void CallAction() {
      switch (triggerAction) {
        case ACTION_NAME.SET_TARGET_PRIORITY:
          if (targetCinemachine != null) targetCinemachine.Priority = targetPriority;
          break;
        case ACTION_NAME.SET_SEQUENCER_PRIORITY:
          if (targetSequencer != null) targetSequencer.Priority = targetPriority;
          break;
        case ACTION_NAME.RESET_ALL_PRIORITY:
          //ResetAllCinemachinePriority();
          break;
      }
    }

    // private void ResetAllCinemachinePriority() {
    //   var cameras = gameObject.FindObjectsOfType<CinemachineCamera>(FindObjectsSortMode.None);
    //   foreach (var cam in cameras) {
    //     cam.Priority = resetPriority;
    //   }
    //   var sequencers = gameObject.FindObjectsOfType<CinemachineSequencerCamera>(FindObjectsSortMode.None);
    //   foreach (var seq in sequencers) {
    //     seq.Priority = resetPriority;
    //   }
    // }
  }
}
