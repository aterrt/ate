using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FOVShake : MonoBehaviour
{
    [Header("�𶯲���")]
    [Tooltip("��ǿ�ȣ���Ұ�仯����")]
    public float shakeIntensity = 30f; // ������Ĭ��ֵȷ���ɼ�

    [Tooltip("�𶯳���ʱ�䣨�룩")]
    public float shakeDuration = 0.7f;

    [Tooltip("��Ƶ�ʣ���ֵԽ����ԽƵ����")]
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
            Debug.Log($"FOVShake��ʼ�� - ���: {playerCamera.name}, ��ʼFOV: {originalFOV}");
        }
        else
        {
            Debug.LogError("FOVShake�����������Camera����Ķ����ϣ�");
        }
    }

    private void Update()
    {
        if (isShaking && playerCamera != null)
        {
            shakeTimer -= Time.deltaTime;

            // ���ɲ�������Ұֵ��ȷ�����ۿɼ�
            float shake = Mathf.Sin(Time.time * shakeFrequency) * shakeIntensity * (shakeTimer / shakeDuration);
            playerCamera.fieldOfView = originalFOV + shake;

            // �𶯽�����ָ�
            if (shakeTimer <= 0)
            {
                isShaking = false;
                playerCamera.fieldOfView = originalFOV;
                Debug.Log("�𶯽������ָ���ʼFOV");
            }
        }
    }

    public void TriggerShake()
    {
        if (playerCamera == null)
        {
            Debug.LogError("û�����������޷��𶯣�");
            return;
        }

        isShaking = true;
        shakeTimer = shakeDuration;
        Debug.Log($"������ - ǿ��: {shakeIntensity}, ����ʱ��: {shakeDuration}");
    }

    // ǿ�Ʋ��԰�ť
    [ContextMenu("����������Ч��")]
    public void ForceTestShake()
    {
        if (playerCamera != null)
        {
            // ǿ������һ�����Ե���Ұ�仯
            playerCamera.fieldOfView = originalFOV + 40f;
            Debug.Log($"ǿ�Ʋ��� - ��ʱFOV: {playerCamera.fieldOfView}");

            // 2����Զ��ָ�
            Invoke(nameof(ResetFOV), 2f);
        }
    }

    private void ResetFOV()
    {
        if (playerCamera != null)
        {
            playerCamera.fieldOfView = originalFOV;
            Debug.Log($"���Խ��� - �ָ�FOV: {originalFOV}");
        }
    }
}
