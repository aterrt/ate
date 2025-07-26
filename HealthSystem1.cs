using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class HealthSystem : MonoBehaviour
{
    [Header("生命值设置")]
    [Tooltip("最大生命值")] public float maxHealth = 100f;
    [Tooltip("当前生命值")] public float currentHealth;
    [Tooltip("高处坠落伤害阈值(米)")] public float fallDamageThreshold = 5f;
    [Tooltip("每米坠落伤害")] public float damagePerMeter = 5f;
    [Tooltip("受伤冷却时间(秒)")] public float damageCooldown = 1f;

    [Header("布娃娃设置（新方案）")]
    [Tooltip("死亡时生成的布娃娃预制体")] public GameObject ragdollPrefab; // 新增：布娃娃预制体
    [Tooltip("布娃娃物理效果持续时间(秒)")] public float ragdollDuration = 1f; // 新增：1秒后静止
    [Tooltip("角色原模型的根节点（死亡后隐藏）")] public GameObject animatedModelRoot;

    [Header("关联组件")]
    [Tooltip("摔落伤害特效引用")] public FallDamageEffect fallDamageEffect;
    [Tooltip("角色动画控制器")] public Animator characterAnimator;
    [Tooltip("玩家移动组件")] public PlayerMovement playerMovement; // 新增：显式引用移动组件
    private CharacterController characterController;
    private CharacterStateMachine stateMachine; // 新增：状态机引用

    [Header("事件")]
    public UnityEvent onDeath;
    public UnityEvent<float> onDamageTaken; // 参数: 伤害值
    public UnityEvent<float> onHealed; // 参数: 治疗值

    // 生命值变化事件，供UI更新使用
    public delegate void HealthChanged(float current, float max);
    public event HealthChanged OnHealthChanged;

    private float lastDamageTime;
    private float fallStartY;
    private bool isFalling = false;
    private FOVShake fovShake; // 缓存FOV震动组件引用
    private static readonly int IsDamaged = Animator.StringToHash("IsDamaged"); // 动画参数哈希

    private void Awake()
    {
        currentHealth = maxHealth;
        FindFOVShakeComponent();

        // 自动获取必要组件
        characterController = GetComponent<CharacterController>();
        stateMachine = GetComponent<CharacterStateMachine>();

        if (characterAnimator == null)
            characterAnimator = GetComponent<Animator>();
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        // 验证布娃娃预制体
        if (ragdollPrefab == null)
            Debug.LogError("请在Inspector中赋值布娃娃预制体！");
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (fovShake == null)
            FindFOVShakeComponent();
    }

    // 查找FOVShake组件并缓存
    private void FindFOVShakeComponent()
    {
        GameObject mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        if (mainCamera != null)
        {
            fovShake = mainCamera.GetComponent<FOVShake>();
            if (fovShake == null)
            {
                Debug.LogWarning("主相机上未找到FOVShake组件");
                FOVShake[] allShakes = FindObjectsOfType<FOVShake>();
                if (allShakes.Length > 0)
                    fovShake = allShakes[0];
            }
        }
        else
        {
            Debug.LogWarning("未找到标签为MainCamera的相机");
        }
    }

    private void Update()
    {
        // 检测坠落（保持原有逻辑）
        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            if (!controller.isGrounded && !isFalling)
            {
                fallStartY = transform.position.y;
                isFalling = true;
            }
            else if (controller.isGrounded && isFalling)
            {
                CalculateFallDamage();
                isFalling = false;
            }
        }
        else
        {
            Debug.LogWarning("玩家对象上未找到CharacterController组件");
        }
    }

    // 计算坠落伤害（保持原有逻辑）
    private void CalculateFallDamage()
    {
        float fallDistance = fallStartY - transform.position.y;
        if (fallDistance > fallDamageThreshold)
        {
            float damage = (fallDistance - fallDamageThreshold) * damagePerMeter;
            TakeDamage(damage);

            if (fallDamageEffect != null)
                fallDamageEffect.PlayDamageEffect();
            else
                Debug.LogWarning("未设置FallDamageEffect组件");
        }
    }

    // 受到伤害（保持原有逻辑）
    public void TakeDamage(float damage)
    {
        if (Time.time - lastDamageTime < damageCooldown)
            return;

        lastDamageTime = Time.time;
        currentHealth = Mathf.Max(0, currentHealth - damage);

        onDamageTaken?.Invoke(damage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        TriggerFOVShake();
        TriggerDamageAnimation();

        if (currentHealth <= 0)
            Die(); // 生命值为0时触发死亡逻辑
    }

    // 触发视野震动（保持原有逻辑）
    private void TriggerFOVShake()
    {
        if (fovShake != null)
            fovShake.TriggerShake();
        else
        {
            Debug.LogError("FOVShake组件未找到！");
            FindFOVShakeComponent();
        }
    }

    // 触发受伤动画（保持原有逻辑）
    private void TriggerDamageAnimation()
    {
        if (characterAnimator == null) return;

        characterAnimator.SetBool(IsDamaged, true);
        Invoke(nameof(ResetDamageAnimation), 0.2f);
    }

    private void ResetDamageAnimation()
    {
        if (characterAnimator != null)
            characterAnimator.SetBool(IsDamaged, false);
    }

    // 治疗（保持原有逻辑）
    public void Heal(float amount)
    {
        if (currentHealth <= 0) return;

        float previousHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        float actualHeal = currentHealth - previousHealth;

        if (actualHeal > 0)
        {
            onHealed?.Invoke(actualHeal);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }

    // 死亡处理（核心修改：生成布娃娃预制体）
    private void Die()
    {
        onDeath?.Invoke();
        Debug.Log("玩家已死亡，生成独立布娃娃预制体");

        // 通知状态机进入死亡状态
        if (stateMachine != null)
            stateMachine.SetDeadState();

        SpawnRagdollPrefab(); // 生成布娃娃预制体
        DisablePlayerLogic(); // 禁用原玩家所有逻辑
        Invoke(nameof(ResetLevel), 5f); // 5秒后重置场景
    }

    // 新增：生成布娃娃预制体
    private void SpawnRagdollPrefab()
    {
        if (ragdollPrefab == null)
        {
            Debug.LogError("布娃娃预制体未赋值，无法生成！");
            return;
        }

        // 在玩家当前位置和旋转生成布娃娃
        GameObject ragdoll = Instantiate(
            ragdollPrefab,
            transform.position,
            transform.rotation
        );

        // 给布娃娃添加自然下落的冲击力
        Rigidbody[] ragdollRBs = ragdoll.GetComponentsInChildren<Rigidbody>();
        foreach (var rb in ragdollRBs)
        {
            rb.velocity = Vector3.zero; // 清除初始速度
            rb.AddForce(Vector3.down * 2f + transform.forward * -1f, ForceMode.Impulse);
        }

        // 1秒后冻结布娃娃
        StartCoroutine(FreezeRagdollAfterDelay(ragdoll, ragdollDuration));
    }

    // 新增：延迟冻结布娃娃
    private IEnumerator FreezeRagdollAfterDelay(GameObject ragdoll, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (ragdoll == null) yield break;

        // 冻结所有刚体
        Rigidbody[] ragdollRBs = ragdoll.GetComponentsInChildren<Rigidbody>();
        foreach (var rb in ragdollRBs)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true; // 完全固定，不再受物理影响
        }
    }

    // 新增：禁用原玩家所有逻辑组件
    private void DisablePlayerLogic()
    {
        // 隐藏原玩家模型
        if (animatedModelRoot != null)
            animatedModelRoot.SetActive(false);
        else
            gameObject.SetActive(false);

        // 禁用角色控制器
        if (characterController != null)
            characterController.enabled = false;

        // 禁用动画器
        if (characterAnimator != null)
            characterAnimator.enabled = false;

        // 禁用移动脚本
        if (playerMovement != null)
            playerMovement.enabled = false;

        // 解锁鼠标
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // 重置关卡（保持原有逻辑）
    private void ResetLevel()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    // 重置生命值（保持原有逻辑）
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // 供外部调用的UI更新方法（保持原有逻辑）
    public void UpdateHealthUI()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
