using UnityEngine;
using UnityEngine.Events;

namespace PP
{
    

public class Property_base : MonoBehaviour
{
    public State _currentState => currentState;
    public enum State
    {
        Idle,
        Awake,
        Inter,
        Disable       
    }
    [SerializeField]protected LayerMask targetLayer;
    protected State currentState = State.Idle;
    [SerializeField]private Property_Animator animator;
    [SerializeField]public UnityEvent AwakeEvent;
    [SerializeField]public UnityEvent InterEvent;
    [SerializeField]public UnityEvent IdleEvent;
    protected virtual void Start()
    {
      ChangeState(State.Idle);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public virtual void ChangeState(State state)
    {
        currentState = state;
        if (animator != null)
        {
            animator.ChangeAnimation(state);
        }
    }
    protected virtual void OnTriggerEnter2D(Collider2D others)
    {
        if (((1 << others.gameObject.layer) & targetLayer) != 0)
        {
            if (currentState == State.Idle)
            {
                ChangeState(State.Awake);
                AwakeEvent.Invoke();
            }
        }
    }
    protected virtual void OnTriggerExit2D(Collider2D others)
    {
        if (((1 << others.gameObject.layer) & targetLayer) != 0)
        {
            if (currentState == State.Awake||currentState == State.Inter)
            {
                ChangeState(State.Idle);
                IdleEvent.Invoke();
            }
        }
    }
    }
}