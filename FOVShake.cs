using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FOVShake : MonoBehaviour
{
    [Header("震动参数")]
    [Tooltip("震动强度（视野变化量）")]
    public float shakeIntensity = 30f; // 大幅提高默认值确保可见

    [Tooltip("震动持续时间（秒）")]
    public float shakeDuration = 0.7f;

    [Tooltip("震动频率（数值越高震动越频繁）")]
    public float shakeFrequency = 20f;

    private Camera playerCamera;
    private float originalFOV;
    private float shakeTimer;
    private bool isShaking;

    private void Awake()
    {
        playerCamera = GetComponent<Camera>();
        if (playerCamera != null)
        {
            originalFOV = playerCamera.fieldOfView;
            Debug.Log($"FOVShake初始化 - 相机: {playerCamera.name}, 初始FOV: {originalFOV}");
        }
        else
        {
            Debug.LogError("FOVShake必须挂载在有Camera组件的对象上！");
        }
    }

    private void Update()
    {
        if (isShaking && playerCamera != null)
        {
            shakeTimer -= Time.deltaTime;

            // 生成波动的视野值，确保肉眼可见
            float shake = Mathf.Sin(Time.time * shakeFrequency) * shakeIntensity * (shakeTimer / shakeDuration);
            playerCamera.fieldOfView = originalFOV + shake;

            // 震动结束后恢复
            if (shakeTimer <= 0)
            {
                isShaking = false;
                playerCamera.fieldOfView = originalFOV;
                Debug.Log("震动结束，恢复初始FOV");
            }
        }
    }

    public void TriggerShake()
    {
        if (playerCamera == null)
        {
            Debug.LogError("没有相机组件，无法震动！");
            return;
        }

        isShaking = true;
        shakeTimer = shakeDuration;
        Debug.Log($"触发震动 - 强度: {shakeIntensity}, 持续时间: {shakeDuration}");
    }

    // 强制测试按钮
    [ContextMenu("立即测试震动效果")]
    public void ForceTestShake()
    {
        if (playerCamera != null)
        {
            // 强制设置一个明显的视野变化
            playerCamera.fieldOfView = originalFOV + 40f;
            Debug.Log($"强制测试 - 临时FOV: {playerCamera.fieldOfView}");

            // 2秒后自动恢复
            Invoke(nameof(ResetFOV), 2f);
        }
    }

    private void ResetFOV()
    {
        if (playerCamera != null)
        {
            playerCamera.fieldOfView = originalFOV;
            Debug.Log($"测试结束 - 恢复FOV: {originalFOV}");
        }
    }
}
