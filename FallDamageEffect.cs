using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class FallDamageEffect : MonoBehaviour
{
    [Header("����֡����")]
    [Tooltip("��Ѫ��Ч������֡ͼƬ")] public Sprite[] damageSprites;
    [Tooltip("���������ٶȣ�ÿ�벥�ŵ�֡����")] public float frameRate = 15f;
    [Tooltip("��Ч��ʾʱ�����룩")] public float effectDuration = 0.5f;

    [Header("��ʾ����")]
    [Tooltip("��Ч����Ļ�е�λ�ã�0-1��Χ��(0.5,0.5)Ϊ���ģ�")] public Vector2 screenPosition = new Vector2(0.5f, 0.3f);
    [Tooltip("��Ч���ű���")] public Vector2 scale = new Vector2(1f, 1f);

    private Image effectImage;
    private RectTransform rectTransform;
    private float frameInterval; // ÿ֡���ʱ��
    private int currentFrame;
    private float timer;
    private bool isPlaying;

    private void Awake()
    {
        effectImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();

        // ��ʼ����Ч״̬
        effectImage.enabled = false;
        effectImage.sprite = null;

        // ����ÿ֡���ʱ��
        if (frameRate > 0)
            frameInterval = 1f / frameRate;
    }

    private void Start()
    {
        // ������Ч����Ļ�е�λ��
        UpdatePosition();
        // ��������
        rectTransform.localScale = scale;
    }

    private void Update()
    {
        if (isPlaying && damageSprites != null && damageSprites.Length > 0)
        {
            timer += Time.deltaTime;

            // ��������֡
            if (timer >= frameInterval)
            {
                currentFrame++;
                timer = 0;

                // ����Ƿ񲥷����
                if (currentFrame >= damageSprites.Length || timer >= effectDuration)
                {
                    StopEffect();
                    return;
                }

                // ���µ�ǰ֡ͼƬ
                effectImage.sprite = damageSprites[currentFrame];
            }
        }
    }

    // ���ſ�Ѫ��Ч
    public void PlayDamageEffect()
    {
        if (damageSprites == null || damageSprites.Length == 0)
        {
            Debug.LogWarning("δ���ÿ�Ѫ��Ч������֡ͼƬ��");
            return;
        }

        // ���ò���״̬
        currentFrame = 0;
        timer = 0;
        isPlaying = true;

        // ��ʾ�����õ�һ֡
        effectImage.enabled = true;
        effectImage.sprite = damageSprites[0];

        // ȷ��λ����ȷ��Ӧ����Ļ�ֱ��ʱ仯��
        UpdatePosition();
    }

    // ֹͣ��Ч
    private void StopEffect()
    {
        isPlaying = false;
        effectImage.enabled = false;
        effectImage.sprite = null;
    }

    // ������Ч����Ļ�е�λ��
    private void UpdatePosition()
    {
        if (rectTransform == null) return;

        // ������Ļ���꣨���䲻ͬ�ֱ��ʣ�
        float x = screenPosition.x * Screen.width;
        float y = screenPosition.y * Screen.height;

        // ����UIλ��
        rectTransform.position = new Vector3(x, y, 0);
    }

    // �༭��Ԥ����ť
    [ContextMenu("���Բ�����Ч")]
    public void TestPlayEffect()
    {
        PlayDamageEffect();
    }
}
