using UnityEngine;
using UnityEngine.UI;

public class HealthVisualizer : MonoBehaviour
{
    // ����HealthSystem���
    public HealthSystem healthSystem;
    // ��������ֵ��ʾ��Image���
    public Image healthImage;
    // ����ֵ��ʱ����ɫ
    public Color fullHealthColor = Color.green;
    // ����ֵ�е�ʱ����ɫ
    public Color midHealthColor = Color.yellow;
    // ����ֵ��ʱ����ɫ
    public Color lowHealthColor = Color.red;
    // ��˸�ٶ�
    public float flashSpeed = 2f;

    // ��Ҷ���ı�ǩ
    public string playerTag = "Player";

    private void Start()
    {
        // ������Ҷ���
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            healthSystem = player.GetComponent<HealthSystem>();
        }
        else
        {
            Debug.LogError("�Ҳ�����ǩΪ" + playerTag + "����Ҷ���");
        }
    }

    private void Update()
    {
        if (healthSystem == null)
        {
            // �ٴγ��Բ�����Ҷ���
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                healthSystem = player.GetComponent<HealthSystem>();
            }
            return;
        }

        // ���㵱ǰ����ֵ����
        float healthRatio = healthSystem.currentHealth / healthSystem.maxHealth;
        // ���������
        healthImage.fillAmount = healthRatio;

        // ��������ֵ����������ɫ
        if (healthRatio > 0.5f)
        {
            healthImage.color = Color.Lerp(midHealthColor, fullHealthColor, (healthRatio - 0.5f) * 2);
        }
        else if (healthRatio > 0.25f)
        {
            healthImage.color = Color.Lerp(lowHealthColor, midHealthColor, (healthRatio - 0.25f) * 4);
        }
        else
        {
            // ����ֵ����25%ʱ��˸
            float flash = Mathf.PingPong(Time.time * flashSpeed, 1);
            healthImage.color = Color.Lerp(lowHealthColor, Color.clear, flash);
        }
    }
}