using UnityEngine;

namespace PP
{
public class Property_Animator : MonoBehaviour
{
    [SerializeField ]private Animator animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void ChangeAnimation(Property_base.State state)
    {
        animator.SetBool(Property_base.State.Idle.ToString(),false);
        animator.SetBool(Property_base.State.Awake.ToString(),false);
        animator.SetBool(Property_base.State.Disable.ToString(),false);

        if (state == Property_base.State.Inter)
        {
            animator.SetTrigger(Property_base.State.Inter.ToString());
        }
        else
        animator.SetBool(state.ToString(),true);
    }
}
}