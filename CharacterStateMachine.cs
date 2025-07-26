using UnityEngine;

public enum CharacterState
{
    Idle,           // ����
    Walk,           // ����
    Run,            // ����
    Crouch,         // ����
    CrouchWalk,     // ��������
    Jump,           // ��Ծ
    CrouchJump,     // ����
    Dead            // ����������״̬
}

[RequireComponent(typeof(Animator))]
public class CharacterStateMachine : MonoBehaviour
{
    [Header("״̬�жϲ���")]
    public float walkSpeedThreshold = 0.5f;
    public float runSpeedThreshold = 5f;
    public float jumpSpeedThreshold = 0.5f;
    public KeyCode runKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl; // ���¼�
    [Tooltip("ͷ������")] public Transform headCheckTransform;
    public float headCheckDistance = 0.5f;
    public LayerMask obstacleLayers;

    // ����������״̬���
    public bool IsDead { get; private set; } = false;
    private bool isMouseLocked = true; // �������״̬

    // ����Ƿ����ڶ�������
    private bool isCrouchWalking;
    // ״̬����ǰ״̬
    public CharacterState CurrentState { get; private set; }

    // �ٶ����������
    private float currentHorizontalSpeed;
    private float currentVerticalSpeed;
    private bool isRunningKeyPressed;
    private bool isCrouchKeyPressed; // ���¼��Ƿ���

    // �������
    private Animator animator;
    private PlayerMovement playerMovement;

    // ����������ϣ
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
        LockMouse(true); // ��ʼ�������
    }

    private void Update()
    {
        // ����״̬��ֹͣ�������봦��
        if (IsDead)
        {
            UpdateDeadState();
            return;
        }

        // ����״̬������
        isRunningKeyPressed = Input.GetKey(runKey);
        isCrouchKeyPressed = Input.GetKey(crouchKey);

        // ���¶������߱��
        isCrouchWalking = IsPlayerCrouching() && currentHorizontalSpeed > walkSpeedThreshold;

        // ����״̬���Ͷ���
        UpdateStateMachine();
        UpdateAnimator();
    }

    // ����״̬�����߼�
    private void UpdateDeadState()
    {
        // ���ö���ϵͳ
        if (animator.enabled)
            animator.enabled = false;

        // ��������ƶ�
        if (playerMovement != null && playerMovement.enabled)
            playerMovement.enabled = false;

        // �������
        if (isMouseLocked)
            LockMouse(false);
    }

    // �������/��������
    private void LockMouse(bool lockState)
    {
        isMouseLocked = lockState;
        Cursor.lockState = lockState ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockState;
    }

    // �����ж��߼�
    private bool IsPlayerCrouching()
    {
        return playerMovement != null
            ? playerMovement.IsCrouching()
            : isCrouchKeyPressed;
    }

    private void UpdateStateMachine()
    {
        // 1. ��Ծ״̬����
        if (currentVerticalSpeed > jumpSpeedThreshold)
        {
            ChangeState(IsPlayerCrouching() ? CharacterState.CrouchJump : CharacterState.Jump);
            return;
        }

        // 2. �������״̬
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

        // 3. վ��״̬
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
                Debug.Log("�����������״̬");
                break;
            case CharacterState.Crouch:
                Debug.Log("������¾�ֹ״̬");
                break;
            case CharacterState.Dead:
                Debug.Log("��������״̬");
                break;
                // ����״̬����...
        }
    }

    private void OnExitState(CharacterState state)
    {
        switch (state)
        {
            case CharacterState.CrouchWalk:
                Debug.Log("�˳���������״̬");
                break;
                // ����״̬����...
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

    // ��������������״̬�ķ�������HealthSystem���ã�
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
