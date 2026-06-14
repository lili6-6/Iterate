using UnityEngine;
using UnityEngine.Events;

namespace PP
{
public class Property_Checkpoint : Property_base
{
    [Header("Checkpoint Settings")]
    [SerializeField] private bool isActivated = false; // 是否已被激活

    [Header("Events")]
    [SerializeField] private UnityEvent OnCheckpointActivated; // 激活事件

    protected override void OnTriggerEnter2D(Collider2D others)
    {
        if (((1 << others.gameObject.layer) & targetLayer) != 0)
        {
            if (currentState == State.Idle && !isActivated)
            {
                // 激活存档点
                isActivated = true;
                ChangeState(State.Awake);
                AwakeEvent.Invoke();

                // 触发自定义激活事件
                OnCheckpointActivated.Invoke();

                // 进入交互状态
                //ChangeState(State.Inter);
                //InterEvent.Invoke();
            }
        }
    }
    protected override void OnTriggerExit2D(Collider2D others)
    {
        
    }

    /// <summary>
    /// 重置存档点状态
    /// </summary>
    public void ResetCheckpoint()
    {
        isActivated = false;
        ChangeState(State.Idle);
        IdleEvent.Invoke();
    }

    /// <summary>
    /// 获取存档点是否已激活
    /// </summary>
    public bool IsActivated()
    {
        return isActivated;
    }
}
}
