using UnityEngine;
using MoreMountains.CorgiEngine;

namespace PP
{

[RequireComponent(typeof(Character))]
public class PlayerInput : MonoBehaviour
{
    private Character character;
    private CharacterHorizontalMovement horizontalMovement;
    private CharacterJump characterJump;
    private CharacterLadder characterLadder;
    private PlayerManager playerManager;

    // 记录当前按下的方向键，用于松开时停止
    private int currentPressedDirection = 0;

    private void Awake()
    {
        character = GetComponent<Character>();
        horizontalMovement = GetComponent<CharacterHorizontalMovement>();
        characterJump = GetComponent<CharacterJump>();
        characterLadder = GetComponent<CharacterLadder>();
    }

    void Start()
    {
        playerManager = GameManager.Instance.playerManager;

        // 关闭 CorgiEngine 的自动读输入，由我们手动控制
        if (horizontalMovement != null)
            horizontalMovement.ReadInput = false;
    }

    private void Update()
    {
        // AI控制期间，玩家输入全部无效
        if (playerManager.StopInput) return;

        ReadInput();
    }

    public void ReadInput()
    {
        // --- 方向键：按下时移动，松开时停止 ---
        bool pressA = Input.GetKey(KeyCode.A);
        bool pressD = Input.GetKey(KeyCode.D);

        // 检测方向键"按下"的瞬间（从松开变为按住）
        bool pressADown = Input.GetKeyDown(KeyCode.A);
        bool pressDDown = Input.GetKeyDown(KeyCode.D);

        if (pressA && !pressD)
        {
            if (pressADown || currentPressedDirection != -1)
            {
                // 按下A的瞬间 → 触发概率判定（可能被AI夺舍）
                // 或者方向从D切换到A → 重新触发判定
                currentPressedDirection = -1;
                playerManager.MoveSelect(-1);
            }
            else if (!playerManager.StopInput)
            {
                // 持续按住A且不在AI控制中 → 直接移动，不触发AI概率判定
                if (horizontalMovement != null)
                    horizontalMovement.SetHorizontalMove(-1);
            }
        }
        else if (pressD && !pressA)
        {
            if (pressDDown || currentPressedDirection != 1)
            {
                // 按下D的瞬间 → 触发概率判定
                // 或者方向从A切换到D → 重新触发判定
                currentPressedDirection = 1;
                playerManager.MoveSelect(1);
            }
            else if (!playerManager.StopInput)
            {
                // 持续按住D且不在AI控制中 → 直接移动，不触发AI概率判定
                if (horizontalMovement != null)
                    horizontalMovement.SetHorizontalMove(1);
            }
        }
        else if (!pressA && !pressD && currentPressedDirection != 0)
        {
            // 所有方向键都松开 → 停止移动
            currentPressedDirection = 0;
            playerManager.MoveSelect(0);
        }

        // --- 跳跃键：按下时触发 ---
        if (Input.GetKeyDown(KeyCode.Space))
        {
            playerManager.JumpSelect();
        }

        // --- 梯子攀爬输入：W/S 或 Up/Down ---
        if (characterLadder != null)
        {
            float vertical = 0f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                vertical = 1f;
            else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                vertical = -1f;

            float horizontal = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                horizontal = -1f;
            else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                horizontal = 1f;

            characterLadder.SetInput(new Vector2(horizontal, vertical), 0.5f);
        }
    }
}
}
