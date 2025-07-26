using UnityEngine;

public enum CharacterState
{
    Idle,           // 空闲
    Walk,           // 行走
    Run,            // 奔跑
    Crouch,         // 蹲下
    CrouchWalk,     // 蹲下行走
    Jump,           // 跳跃
    CrouchJump,     // 蹲跳
    Dead            // 新增：死亡状态
}

[RequireComponent(typeof(Animator))]
public class CharacterStateMachine : MonoBehaviour
{
    [Header("状态判断参数")]
    public float walkSpeedThreshold = 0.5f;
    public float runSpeedThreshold = 5f;
    public float jumpSpeedThreshold = 0.5f;
    public KeyCode runKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl; // 蹲下键
    [Tooltip("头部检测点")] public Transform headCheckTransform;
    public float headCheckDistance = 0.5f;
    public LayerMask obstacleLayers;

    // 新增：死亡状态标记
    public bool IsDead { get; private set; } = false;
    private bool isMouseLocked = true; // 鼠标锁定状态

    // 标记是否正在蹲下行走
    private bool isCrouchWalking;
    // 状态机当前状态
    public CharacterState CurrentState { get; private set; }

    // 速度与输入变量
    private float currentHorizontalSpeed;
    private float currentVerticalSpeed;
    private bool isRunningKeyPressed;
    private bool isCrouchKeyPressed; // 蹲下键是否按下

    // 组件引用
    private Animator animator;
    private PlayerMovement playerMovement;

    // 动画参数哈希
    private static readonly int IsIdle = Animator.StringToHash("IsIdle");
    private static readonly int IsWalking = Animator.StringToHash("IsWalking");
    private static readonly int IsRunning = Animator.StringToHash("IsRunning");
    private static readonly int IsCrouching = Animator.StringToHash("IsCrouching");
    private static readonly int IsCrouchWalking = Animator.StringToHash("IsCrouchWalking");
    private static readonly int IsJumping = Animator.StringToHash("IsJumping");
    private static readonly int IsCrouchJumping = Animator.StringToHash("IsCrouchJumping");
    private static readonly int VerticalSpeed = Animator.StringToHash("VerticalSpeed");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>();

        if (headCheckTransform == null)
        {
            headCheckTransform = new GameObject("HeadCheck").transform;
            headCheckTransform.parent = transform;
            headCheckTransform.localPosition = new Vector3(0, 1.8f, 0);
        }
    }

    private void Start()
    {
        CurrentState = CharacterState.Idle;
        LockMouse(true); // 初始锁定鼠标
    }

    private void Update()
    {
        // 死亡状态：停止所有输入处理
        if (IsDead)
        {
            UpdateDeadState();
            return;
        }

        // 正常状态输入检测
        isRunningKeyPressed = Input.GetKey(runKey);
        isCrouchKeyPressed = Input.GetKey(crouchKey);

        // 更新蹲下行走标记
        isCrouchWalking = IsPlayerCrouching() && currentHorizontalSpeed > walkSpeedThreshold;

        // 更新状态机和动画
        UpdateStateMachine();
        UpdateAnimator();
    }

    // 死亡状态处理逻辑
    private void UpdateDeadState()
    {
        // 禁用动画系统
        if (animator.enabled)
            animator.enabled = false;

        // 禁用玩家移动
        if (playerMovement != null && playerMovement.enabled)
            playerMovement.enabled = false;

        // 解锁鼠标
        if (isMouseLocked)
            LockMouse(false);
    }

    // 鼠标锁定/解锁控制
    private void LockMouse(bool lockState)
    {
        isMouseLocked = lockState;
        Cursor.lockState = lockState ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockState;
    }

    // 蹲下判断逻辑
    private bool IsPlayerCrouching()
    {
        return playerMovement != null
            ? playerMovement.IsCrouching()
            : isCrouchKeyPressed;
    }

    private void UpdateStateMachine()
    {
        // 1. 跳跃状态优先
        if (currentVerticalSpeed > jumpSpeedThreshold)
        {
            ChangeState(IsPlayerCrouching() ? CharacterState.CrouchJump : CharacterState.Jump);
            return;
        }

        // 2. 蹲下相关状态
        if (IsPlayerCrouching())
        {
            if (isCrouchWalking)
            {
                ChangeState(CharacterState.CrouchWalk);
            }
            else
            {
                ChangeState(CharacterState.Crouch);
            }
            return;
        }

        // 3. 站立状态
        if (currentHorizontalSpeed > runSpeedThreshold && isRunningKeyPressed)
        {
            ChangeState(CharacterState.Run);
        }
        else if (currentHorizontalSpeed > walkSpeedThreshold)
        {
            ChangeState(CharacterState.Walk);
        }
        else
        {
            ChangeState(CharacterState.Idle);
        }
    }

    private void ChangeState(CharacterState newState)
    {
        if (CurrentState == newState) return;

        OnExitState(CurrentState);
        CurrentState = newState;
        OnEnterState(newState);
    }

    private void OnEnterState(CharacterState state)
    {
        switch (state)
        {
            case CharacterState.CrouchWalk:
                Debug.Log("进入蹲下行走状态");
                break;
            case CharacterState.Crouch:
                Debug.Log("进入蹲下静止状态");
                break;
            case CharacterState.Dead:
                Debug.Log("进入死亡状态");
                break;
                // 其他状态处理...
        }
    }

    private void OnExitState(CharacterState state)
    {
        switch (state)
        {
            case CharacterState.CrouchWalk:
                Debug.Log("退出蹲下行走状态");
                break;
                // 其他状态处理...
        }
    }

    private void UpdateAnimator()
    {
        animator.SetBool(IsIdle, CurrentState == CharacterState.Idle);
        animator.SetBool(IsWalking, CurrentState == CharacterState.Walk);
        animator.SetBool(IsRunning, CurrentState == CharacterState.Run);
        animator.SetBool(IsCrouching, CurrentState == CharacterState.Crouch);
        animator.SetBool(IsCrouchWalking, CurrentState == CharacterState.CrouchWalk);
        animator.SetBool(IsJumping, CurrentState == CharacterState.Jump);
        animator.SetBool(IsCrouchJumping, CurrentState == CharacterState.CrouchJump);
        animator.SetFloat(VerticalSpeed, currentVerticalSpeed);
    }

    // 新增：设置死亡状态的方法（供HealthSystem调用）
    public void SetDeadState()
    {
        IsDead = true;
        CurrentState = CharacterState.Dead;
        OnEnterState(CharacterState.Dead);
    }

    public void SetVelocity(float horizontalSpeed, float verticalSpeed)
    {
        currentHorizontalSpeed = Mathf.Abs(horizontalSpeed);
        currentVerticalSpeed = verticalSpeed;
    }

    public bool HasHeadObstacle()
    {
        if (headCheckTransform == null) return false;
        return Physics.Raycast(headCheckTransform.position, Vector3.up, headCheckDistance, obstacleLayers);
    }

    public void PerformJump(bool isCrouchJump = false)
    {
        if (playerMovement != null && !playerMovement.IsTransitioning())
        {
            playerMovement.Jump(isCrouchJump);
        }
    }
}
