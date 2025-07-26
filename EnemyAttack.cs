using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Tooltip("攻击距离")] public float attackRange = 2f;
    [Tooltip("攻击伤害")] public float attackDamage = 10f;
    [Tooltip("攻击间隔(秒)")] public float attackCooldown = 2f;
    [Tooltip("玩家标签")] public string playerTag = "Player";

    private Transform targetPlayer;
    private float lastAttackTime;

    private void Start()
    {
        // 查找玩家
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            targetPlayer = player.transform;
        }
        else
        {
            Debug.LogWarning("未找到带有Player标签的玩家对象", this);
        }
    }

    private void Update()
    {
        // 如果找到玩家且不在冷却中，检测是否可以攻击
        if (targetPlayer != null && Time.time - lastAttackTime >= attackCooldown)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);
            if (distanceToPlayer <= attackRange)
            {
                AttackPlayer();
            }
        }
    }

    // 攻击玩家
    private void AttackPlayer()
    {
        lastAttackTime = Time.time;

        // 获取玩家的生命值系统并造成伤害
        HealthSystem playerHealth = targetPlayer.GetComponent<HealthSystem>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
            Debug.Log($"敌人对玩家造成了{attackDamage}点伤害");
            // 这里可以添加攻击动画、音效等
        }
        else
        {
            Debug.LogWarning("玩家对象上未找到HealthSystem组件", this);
        }
    }

    // 绘制攻击范围Gizmo
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
