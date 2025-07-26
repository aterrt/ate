using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class FallDamageEffect : MonoBehaviour
{
    [Header("序列帧设置")]
    [Tooltip("扣血特效的序列帧图片")] public Sprite[] damageSprites;
    [Tooltip("动画播放速度（每秒播放的帧数）")] public float frameRate = 15f;
    [Tooltip("特效显示时长（秒）")] public float effectDuration = 0.5f;

    [Header("显示设置")]
    [Tooltip("特效在屏幕中的位置（0-1范围，(0.5,0.5)为中心）")] public Vector2 screenPosition = new Vector2(0.5f, 0.3f);
    [Tooltip("特效缩放比例")] public Vector2 scale = new Vector2(1f, 1f);

    private Image effectImage;
    private RectTransform rectTransform;
    private float frameInterval; // 每帧间隔时间
    private int currentFrame;
    private float timer;
    private bool isPlaying;

    private void Awake()
    {
        effectImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();

        // 初始化特效状态
        effectImage.enabled = false;
        effectImage.sprite = null;

        // 计算每帧间隔时间
        if (frameRate > 0)
            frameInterval = 1f / frameRate;
    }

    private void Start()
    {
        // 设置特效在屏幕中的位置
        UpdatePosition();
        // 设置缩放
        rectTransform.localScale = scale;
    }

    private void Update()
    {
        if (isPlaying && damageSprites != null && damageSprites.Length > 0)
        {
            timer += Time.deltaTime;

            // 更新序列帧
            if (timer >= frameInterval)
            {
                currentFrame++;
                timer = 0;

                // 检查是否播放完毕
                if (currentFrame >= damageSprites.Length || timer >= effectDuration)
                {
                    StopEffect();
                    return;
                }

                // 更新当前帧图片
                effectImage.sprite = damageSprites[currentFrame];
            }
        }
    }

    // 播放扣血特效
    public void PlayDamageEffect()
    {
        if (damageSprites == null || damageSprites.Length == 0)
        {
            Debug.LogWarning("未设置扣血特效的序列帧图片！");
            return;
        }

        // 重置播放状态
        currentFrame = 0;
        timer = 0;
        isPlaying = true;

        // 显示并设置第一帧
        effectImage.enabled = true;
        effectImage.sprite = damageSprites[0];

        // 确保位置正确（应对屏幕分辨率变化）
        UpdatePosition();
    }

    // 停止特效
    private void StopEffect()
    {
        isPlaying = false;
        effectImage.enabled = false;
        effectImage.sprite = null;
    }

    // 更新特效在屏幕中的位置
    private void UpdatePosition()
    {
        if (rectTransform == null) return;

        // 计算屏幕坐标（适配不同分辨率）
        float x = screenPosition.x * Screen.width;
        float y = screenPosition.y * Screen.height;

        // 设置UI位置
        rectTransform.position = new Vector3(x, y, 0);
    }

    // 编辑器预览按钮
    [ContextMenu("测试播放特效")]
    public void TestPlayEffect()
    {
        PlayDamageEffect();
    }
}
