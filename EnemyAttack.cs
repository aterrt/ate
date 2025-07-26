using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Tooltip("��������")] public float attackRange = 2f;
    [Tooltip("�����˺�")] public float attackDamage = 10f;
    [Tooltip("�������(��)")] public float attackCooldown = 2f;
    [Tooltip("��ұ�ǩ")] public string playerTag = "Player";

    private Transform targetPlayer;
    private float lastAttackTime;

    private void Start()
    {
        // �������
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            targetPlayer = player.transform;
        }
        else
        {
            Debug.LogWarning("δ�ҵ�����Player��ǩ����Ҷ���", this);
        }
    }

    private void Update()
    {
        // ����ҵ�����Ҳ�����ȴ�У�����Ƿ���Թ���
        if (targetPlayer != null && Time.time - lastAttackTime >= attackCooldown)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);
            if (distanceToPlayer <= attackRange)
            {
                AttackPlayer();
            }
        }
    }

    // �������
    private void AttackPlayer()
    {
        lastAttackTime = Time.time;

        // ��ȡ��ҵ�����ֵϵͳ������˺�
        HealthSystem playerHealth = targetPlayer.GetComponent<HealthSystem>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
            Debug.Log($"���˶���������{attackDamage}���˺�");
            // ���������ӹ�����������Ч��
        }
        else
        {
            Debug.LogWarning("��Ҷ�����δ�ҵ�HealthSystem���", this);
        }
    }

    // ���ƹ�����ΧGizmo
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
