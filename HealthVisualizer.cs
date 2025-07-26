using UnityEngine;
using UnityEngine.UI;

public class HealthVisualizer : MonoBehaviour
{
    // 引用HealthSystem组件
    public HealthSystem healthSystem;
    // 引用生命值显示的Image组件
    public Image healthImage;
    // 生命值满时的颜色
    public Color fullHealthColor = Color.green;
    // 生命值中等时的颜色
    public Color midHealthColor = Color.yellow;
    // 生命值低时的颜色
    public Color lowHealthColor = Color.red;
    // 闪烁速度
    public float flashSpeed = 2f;

    // 玩家对象的标签
    public string playerTag = "Player";

    private void Start()
    {
        // 查找玩家对象
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            healthSystem = player.GetComponent<HealthSystem>();
        }
        else
        {
            Debug.LogError("找不到标签为" + playerTag + "的玩家对象！");
        }
    }

    private void Update()
    {
        if (healthSystem == null)
        {
            // 再次尝试查找玩家对象
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                healthSystem = player.GetComponent<HealthSystem>();
            }
            return;
        }

        // 计算当前生命值比例
        float healthRatio = healthSystem.currentHealth / healthSystem.maxHealth;
        // 更新填充量
        healthImage.fillAmount = healthRatio;

        // 根据生命值比例设置颜色
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
            // 生命值低于25%时闪烁
            float flash = Mathf.PingPong(Time.time * flashSpeed, 1);
            healthImage.color = Color.Lerp(lowHealthColor, Color.clear, flash);
        }
    }
}