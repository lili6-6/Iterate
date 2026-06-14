using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;
using Tooltip = BehaviorDesigner.Runtime.Tasks.TooltipAttribute;


namespace PP
{
  [TaskCategory("Custom/Animation")]
[TaskDescription("执行动画参数控制：Bool/Trigger")]

public class AnimationAction : Action
{
    public enum AnimationActionType
    {
        None,
        SetBool,
        SetTrigger
    }

    [Tooltip("选择要执行的动画操作")]
    public AnimationActionType triggerAction;

    [Tooltip("目标动画控制器 Animator")]
    public Animator TargetAnimator;

    [Tooltip("动画参数名，多个参数用英文逗号分隔")]
    public string AnimatorParams;

    [Tooltip("仅对 SetBool 有效：true=设置为真，false=设置为假")]
    public bool BoolEnable;

    private string[] animatorParamsArray;

    public override void OnAwake()
    {
        // 基础参数校验
        if (triggerAction == AnimationActionType.None)
        {
            Debug.LogError($"{FriendlyName} 必须选择一个动画操作类型！");
        }
        
        if (TargetAnimator == null)
        {
            Debug.LogError($"{FriendlyName} 未指定目标 Animator！");
        }

        if (string.IsNullOrWhiteSpace(AnimatorParams))
        {
            Debug.LogError($"{FriendlyName} 未填写任何动画参数名！");
        }

        // 分割参数数组
        animatorParamsArray = AnimatorParams.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
    }

    public override void OnStart()
    {
        ExecuteAnimationAction();
    }

    private void ExecuteAnimationAction()
    {
        // 空引用安全校验
        if (TargetAnimator == null || animatorParamsArray == null || animatorParamsArray.Length == 0)
            return;

        switch (triggerAction)
        {
            case AnimationActionType.SetBool:
                foreach (var param in animatorParamsArray)
                {
                    if (!string.IsNullOrWhiteSpace(param))
                        TargetAnimator.SetBool(param.Trim(), BoolEnable);
                }
                break;

            case AnimationActionType.SetTrigger:
                foreach (var param in animatorParamsArray)
                {
                    if (!string.IsNullOrWhiteSpace(param))
                        TargetAnimator.SetTrigger(param.Trim());
                }
                break;
        }
    }
}}