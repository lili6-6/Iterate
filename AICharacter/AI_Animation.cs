using UnityEngine;

public class AI_Animation : MonoBehaviour
{
    public enum AIState
    {
        Idle,
        Approach,
        Thinking,
        Reply
    }

    [Tooltip("Animator used to drive NPC animation state.")]
    public Animator animator;

    public AIState CurrentState { get; private set; } = AIState.Idle;

    public void SetState(AIState newState)
    {
        if (CurrentState == newState)
        {
            return;
        }

        CurrentState = newState;
        ApplyAnimation(newState);
    }

    private void ApplyAnimation(AIState state)
    {
        if (animator == null)
        {
            Debug.Log("AI_Animation: no Animator assigned, state=" + state);
            return;
        }

        animator.SetBool("Idle", state == AIState.Idle);
        animator.SetBool("Approach", state == AIState.Approach);
        animator.SetBool("Thinking", state == AIState.Thinking);
        animator.SetBool("Reply", state == AIState.Reply);
    }

    public void PlayIdle() => SetState(AIState.Idle);
    public void PlayApproach() => SetState(AIState.Approach);
    public void PlayThinking() => SetState(AIState.Thinking);
    public void PlayReply() => SetState(AIState.Reply);
}
