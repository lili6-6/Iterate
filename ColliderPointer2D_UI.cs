using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;

public class ColliderPointer2D_UI : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("拖拽参数")]
    [Tooltip("鼠标按下后，需要x秒产生拖拽效果")]
    [SerializeField] private float draggingStartDamping = 0.5f;
    [Tooltip("鼠标松开后，需要x秒产生丢放效果")]
    [SerializeField] private float draggingStopDamping = 0.2f;

    [Header("调试输出")]
    [SerializeField] private bool enableDebugger = false;

    [Header("事件")]
    public UnityEvent OnClicked;
    public UnityEvent OnHover;
    public UnityEvent OnExit;
    public UnityEvent OnDragged;
    public UnityEvent OnDropped;

    private bool isDragging = false;
    private Coroutine draggingCoroutine;
    private Coroutine droppingCoroutine;

    // 指示是否悬停状态（用于防止重复触发）
    private bool isHovering = false;

    // 拖拽开始计时器
    private float dragTimer = 0f;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (enableDebugger) Debug.Log("OnPointerEnter");
        if (!isHovering)
        {
            isHovering = true;
            OnHover.Invoke();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (enableDebugger) Debug.Log("OnPointerExit");
        if (isHovering)
        {
            isHovering = false;
            OnExit.Invoke();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (enableDebugger) Debug.Log("OnPointerDown");
        // 点击事件延后执行，防止拖拽误触发
        // 直接触发点击事件，或你可以放在 OnPointerUp 里根据需要调整
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (enableDebugger) Debug.Log("OnPointerUp");

        if (!isDragging)
        {
            if (enableDebugger) Debug.Log("触发点击事件");
            OnClicked.Invoke();
        }
        else
        {
            // 拖拽结束，启动拖拽停止计时
            if (droppingCoroutine != null) StopCoroutine(droppingCoroutine);
            droppingCoroutine = StartCoroutine(StopDraggingSequence());
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (enableDebugger) Debug.Log("OnBeginDrag");

        // 开始拖拽计时，确认达到拖拽阈值才真正开始拖拽
        if (draggingCoroutine != null) StopCoroutine(draggingCoroutine);
        draggingCoroutine = StartCoroutine(StartDraggingSequence());
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            // 拖拽中，你可以做拖拽逻辑，例如移动物体
            // 这里触发拖拽事件
            OnDragged.Invoke();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (enableDebugger) Debug.Log("OnEndDrag");
        if (draggingCoroutine != null) StopCoroutine(draggingCoroutine);

        if (isDragging)
        {
            // 拖拽结束，启动拖拽停止计时
            if (droppingCoroutine != null) StopCoroutine(droppingCoroutine);
            droppingCoroutine = StartCoroutine(StopDraggingSequence());
        }
    }

    private IEnumerator StartDraggingSequence()
    {
        dragTimer = 0f;
        while (dragTimer < draggingStartDamping)
        {
            dragTimer += Time.deltaTime;
            yield return null;
        }

        isDragging = true;
        if (enableDebugger) Debug.Log("开始拖拽");
        OnDragged.Invoke();
    }

    private IEnumerator StopDraggingSequence()
    {
        float timer = 0f;
        while (timer < draggingStopDamping)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        isDragging = false;
        if (enableDebugger) Debug.Log("拖拽结束");
        OnDropped.Invoke();
    }
}
