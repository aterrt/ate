using UnityEngine;
using System.Collections;
using UnityEngine.Assertions;
using InventorySystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    #region ��������
    [Tooltip("������Ч")] public AudioClip ��Ծ��Ч;
    [Tooltip("�����Ч")] public AudioClip �����Ч;
    [Tooltip("�����ٶ�")] public float �����ٶ� = 5f;
    [Tooltip("�ܲ��ٶ�")] public float �ܲ��ٶ� = 8f;
    [Tooltip("��Ծ����")] public float ��Ծ���� = 7f;
    [Tooltip("������������")] public float ������������ = 2f; // ��������������߶�
    [Tooltip("�������ٶ�")] public float �������ٶ� = -19.62f;
    #endregion

    #region ��ײ�������
    [Tooltip("վ����ײ��߶�")] public float վ���߶� = 2.3f;
    [Tooltip("������ײ��߶�")] public float ���¸߶� = 0.9f;
    [Tooltip("�����ƶ��ٶ�")] public float �����ٶ� = 3f;
    [Tooltip("���°���")] public KeyCode ���°��� = KeyCode.LeftControl;
    [Tooltip("��վ����ʱ��")] public float ����ʱ�� = 0.2f;
    #endregion

    #region ����Ч������
    [Header("���°���Ч��")]
    [Tooltip("�Ƿ����ð���Ч��")] public bool ���ð��� = true;
    [Tooltip("���ǲ�����")]
    public Material ���ǲ���;
    [Tooltip("����ʱ����ǿ��")] public float ���°���ǿ�� = 0.8f;
    [Tooltip("վ��ʱ����ǿ��")] public float վ������ǿ�� = 0f;
    [Tooltip("���ǹ����ٶ�")] public float ���ǹ����ٶ� = 5f;
    #endregion

    #region ����ϵͳ����
    [Header("����ϵͳ����")]
    [Tooltip("�������ֵ")] public float ������� = 100f;
    [Tooltip("�ܲ���������")] public float �ܲ��������� = 15f;
    [Tooltip("��Ծ����ֵ")] public float ��Ծ����ֵ = 20f;
    [Tooltip("������������")] public float ������������ = 10f; // ������������������
    [Tooltip("�����ָ�����")] public float �ָ����� = 8f;
    [Tooltip("�ָ��ӳ�ʱ��(��)")] public float �ָ��ӳ� = 1f;
    [Tooltip("���������")] public float ��������� = 200f;
    [Tooltip("�������߶�")] public float �������߶� = 8f;
    [Tooltip("��������ɫ")] public Color ��������ɫ = Color.white;
    [Tooltip("�̶�����")] public int �̶����� = 5;
    [Tooltip("�̶ȿ��")] public float �̶ȿ�� = 1f;
    [Tooltip("�̶ȸ߶ȱ���")] public float �̶ȸ߶ȱ��� = 0.7f;
    [Tooltip("����I���")] public float ������� = 6f;
    [Tooltip("����I�߶ȱ���")] public float �����߶ȱ��� = 1.5f;
    [Tooltip("����������ƫ��Yֵ")] public float ����ƫ��Y = -150f;
    #endregion

    #region ͷ�����
    [Tooltip("���뾶")] public float ���뾶 = 0.3f;
    [Tooltip("���ƫ����")] public float ���ƫ�� = 0.2f;
    [Tooltip("���Բ�")] public LayerMask ���Բ�;
    #endregion

    #region �������
    [Tooltip("������")] public Transform ������;
    [Tooltip("���������")] public float ��������� = 2f;
    [Tooltip("վ�����Y��λ��")] public float վ�����Y = 1.6f;
    [Tooltip("�������Y��λ��")] public float �������Y = 0.8f;
    #endregion

    #region ������
    [Tooltip("���������")] public float ��������� = 0.1f;
    [Tooltip("�����")] public LayerMask �����;
    #endregion

    #region ˽�б���
    private CharacterController ��ɫ������;
    private Animator ����������;
    private AudioSource ��ƵԴ;
    private Vector3 �ٶ�;
    private float ������;
    private bool �ѽӵ�;
    private bool ֮ǰ�ӵ�;
    private bool �����;
    private Vector3 Ŀ���ٶ�;
    private bool ������;
    private Vector3 ����ǰ��;

    // ����״̬
    private bool ���ڶ���;
    private bool ���ڹ���;
    private float ���ɽ���;
    private Vector3 ��ʼ����;

    // ����Ч��
    private float ��ǰ����ǿ��;
    private bool �����ѳ�ʼ�� = false;
    private bool ����ϵͳ���� = false;

    // ����ϵͳ����
    private float ��ǰ����;
    private float �ָ���ʱ��;
    private bool �����ܲ�;
    private Texture2D ��������;
    private bool ���������ѳ�ʼ�� = false;

    // ������״̬������
    private CharacterStateMachine ״̬��;

    // ����ϵͳ
    private Inventory ����ϵͳ;
    #endregion

    #region ������ϣ
    private static readonly int ����_���ڲ��� = Animator.StringToHash("IsWalking");
    private static readonly int ����_���ڶ��� = Animator.StringToHash("IsCrouching");
    private static readonly int ����_������Ծ = Animator.StringToHash("IsJumping");
    private static readonly int ����_���ڶ���׼�� = Animator.StringToHash("IsCrouchJumpPrepare"); // ����
    #endregion

    #region ��ʼ��
    private void Awake()
    {
        ��ɫ������ = GetComponent<CharacterController>();
        ���������� = GetComponent<Animator>();
        ��ƵԴ = gameObject.AddComponent<AudioSource>();
        ��ʼ���� = new Vector3(��ɫ������.center.x, 0, ��ɫ������.center.z);

        // ��ʼ������
        ��ǰ���� = �������;
        �ָ���ʱ�� = 0;

        // ��֤��Ҫ���
        Assert.IsNotNull(��ɫ������, "ȱ��CharacterController�����");
        Assert.IsNotNull(������, "��ָ����������");

        // ��ʼ������Ч��
        ��ʼ������();

        // ��ȡ״̬�����
        ״̬�� = GetComponent<CharacterStateMachine>();
        if (״̬�� == null)
        {
            ״̬�� = FindObjectOfType<CharacterStateMachine>();
            if (״̬�� == null)
            {
                Debug.LogWarning("δ�ҵ�CharacterStateMachine�����״̬��������쳣");
            }
        }

        // ��ȡInventory���
        ����ϵͳ = GetComponent<Inventory>();
        if (����ϵͳ == null)
        {
            ����ϵͳ = FindObjectOfType<Inventory>();
        }
    }

    private void Start()
    {
        ����վ��״̬();
        �������();

        // ��ʼ��������
        if (�����.value == 0) ����� = LayerMask.GetMask("Terrain");
        if (���Բ�.value == 0) ���Բ� = 1 << gameObject.layer;
    }

    private void ��ʼ������()
    {
        // ������ð���Ч����ֱ�ӷ���
        if (!���ð���)
        {
            ����ϵͳ���� = false;
            return;
        }

        // ���Ի�ȡ�򴴽����ǲ���
        if (���ǲ��� == null)
        {
            // ���ȳ��Ի�ȡUIר��͸����ɫ��
            Shader ͸����ɫ�� = Shader.Find("UI/Unlit/Transparent");
            // ����Ҳ���UI��ɫ�����ٳ�����ͨUnlit͸����ɫ��
            if (͸����ɫ�� == null)
            {
                ͸����ɫ�� = Shader.Find("Unlit/Transparent");
            }

            // �����Ȼ�Ҳ�����ɫ�������ð���Ч��
            if (͸����ɫ�� == null)
            {
                Debug.LogWarning("�Ҳ������ʵ�͸����ɫ��������Ч�������á���ȷ����Ŀ�д���Unlit/Transparent��UI/Unlit/Transparent��ɫ����");
                ����ϵͳ���� = false;
                return;
            }

            // �������ʲ����û�������
            ���ǲ��� = new Material(͸����ɫ��);
            ���ǲ���.name = "Auto-Created Vignette Material";

            // ������������
            Texture2D �������� = new Texture2D(256, 256);
            for (int y = 0; y < 256; y++)
            {
                for (int x = 0; x < 256; x++)
                {
                    // ����������ĵľ��루0-1��Χ��
                    float dx = (x / 255f) - 0.5f;
                    float dy = (y / 255f) - 0.5f;
                    float ���� = Mathf.Sqrt(dx * dx + dy * dy) * 2f;

                    // ��Ե����������͸���ľ��򽥱�
                    float alpha = Mathf.Lerp(0, 1, ����);
                    ��������.SetPixel(x, y, new Color(0, 0, 0, alpha));
                }
            }
            ��������.Apply();
            ���ǲ���.mainTexture = ��������;
        }

        // ��֤�����Ƿ����
        if (���ǲ��� == null || ���ǲ���.shader == null)
        {
            Debug.LogWarning("���ǲ�����Ч������Ч��������");
            ����ϵͳ���� = false;
            return;
        }

        // ��ʼ������ǿ��Ϊվ��״̬
        ��ǰ����ǿ�� = վ������ǿ��;
        ���°��ǲ���();

        �����ѳ�ʼ�� = true;
        ����ϵͳ���� = true;
    }

    private void ��ʼ����������()
    {
        �������� = new Texture2D(1, 1);
        ��������.SetPixel(0, 0, ��������ɫ);
        ��������.Apply();
        ���������ѳ�ʼ�� = true;
    }

    private void �������()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    #endregion

    #region ���ĸ���
    private void Update()
    {
        // ��������򿪣�ֻ���±�Ҫ״̬������������
        if (����ϵͳ != null && ����ϵͳ.isInventoryOpen)
        {
            ���½ӵ�״̬();
            ���¶���();
            return;
        }

        �����ӽ�();
        ���½ӵ�״̬();

        // �ƶ��߼�
        ���¶���״̬();
        �����ƶ�();
        ������Ծ();

        // ����ϵͳ����
        ��������();

        ��ɫ������.Move(�ٶ� * Time.deltaTime);
        ���¶���();

        // ֻ��ϵͳ����ʱ���°���Ч��
        if (����ϵͳ����)
        {
            ���°���Ч��();
        }
    }
    #endregion

    #region ����ϵͳ�߼�
    private void ��������()
    {
        // �ܲ�ʱ��������
        if (�����ܲ� && ��ǰ���� > 0)
        {
            ��ǰ���� -= �ܲ��������� * Time.deltaTime;
            �ָ���ʱ�� = �ָ��ӳ�;
        }
        // �ָ�����
        else if (��ǰ���� < ������� && !�����ܲ�)
        {
            �ָ���ʱ�� -= Time.deltaTime;
            if (�ָ���ʱ�� <= 0)
            {
                ��ǰ���� += �ָ����� * Time.deltaTime;
                if (��ǰ���� > �������)
                    ��ǰ���� = �������;
            }
        }
    }

    private bool ���㹻����(bool �Ƕ��� = false)
    {
        if (�Ƕ���)
        {
            return ��ǰ���� > ��Ծ����ֵ + ������������;
        }
        return ��ǰ���� > 0;
    }

    private void ������Ծ����(bool �Ƕ��� = false)
    {
        if (�Ƕ���)
        {
            ��ǰ���� -= ��Ծ����ֵ + ������������;
        }
        else
        {
            ��ǰ���� -= ��Ծ����ֵ;
        }

        if (��ǰ���� < 0) ��ǰ���� = 0;
        �ָ���ʱ�� = �ָ��ӳ�;
    }
    #endregion

    #region �����ת
    private void �����ӽ�()
    {
        float ���X = Input.GetAxis("Mouse X") * ���������;
        float ���Y = Input.GetAxis("Mouse Y") * ���������;

        // ��ɫˮƽ��ת
        transform.Rotate(Vector3.up * ���X);

        // �����ֱ��ת
        ������ = Mathf.Clamp(������ - ���Y, -90f, 90f);
        ������.localRotation = Quaternion.Euler(������, 0, 0);

        // ����ˮƽ����
        ����ǰ�� = transform.forward;
        ����ǰ��.y = 0;
        ����ǰ��.Normalize();
    }
    #endregion

    #region ������
    private void ���½ӵ�״̬()
    {
        ֮ǰ�ӵ� = �ѽӵ�;
        �ѽӵ� = ��ɫ������.isGrounded;

        // ���μ��
        if (!�ѽӵ�)
        {
            Vector3 ԭ�� = transform.position + ��ɫ������.center -
                           Vector3.up * (��ɫ������.height / 2 - ��ɫ������.radius);
            �ѽӵ� = Physics.SphereCast(ԭ��, ��ɫ������.radius,
                                          Vector3.down, out _, ���������, �����);
        }

        // ��ؼ��
        ����� = !֮ǰ�ӵ� && �ѽӵ� && �ٶ�.y < -1f;
        if (����� && �����Ч != null)
            ��ƵԴ.PlayOneShot(�����Ч);
    }
    #endregion

    #region �����ƶ�
    private void �����ƶ�()
    {
        if (���ڹ���) return;

        // ������
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        ������ = Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f;

        // �ƶ�����
        Vector3 ���� = transform.right * h + ����ǰ�� * v;
        ����.y = 0;
        if (����.magnitude > 0.1f) ����.Normalize();

        // �ٶȼ���
        bool �����ܲ� = ���㹻����();
        �����ܲ� = Input.GetKey(KeyCode.LeftShift) && �����ܲ� && ������;
        float �ƶ��ٶ� = ���ڶ��� ? �����ٶ� :
                     (�����ܲ� ? �ܲ��ٶ� : �����ٶ�);

        Ŀ���ٶ� = ���� * �ƶ��ٶ�;
        ƽ���ٶ�();
    }

    private void ƽ���ٶ�()
    {
        Vector3 ˮƽ�ٶ� = new Vector3(�ٶ�.x, 0, �ٶ�.z);

        if (������)
        {
            �ٶ�.x = Mathf.MoveTowards(ˮƽ�ٶ�.x, Ŀ���ٶ�.x, 30 * Time.deltaTime);
            �ٶ�.z = Mathf.MoveTowards(ˮƽ�ٶ�.z, Ŀ���ٶ�.z, 30 * Time.deltaTime);
        }
        else
        {
            // ����
            float ���� = 30 * Time.deltaTime;
            float ���� = Mathf.Max(0, ˮƽ�ٶ�.magnitude - ����);
            Vector3 ���� = ˮƽ�ٶ�.normalized;
            �ٶ�.x = ����.x * ����;
            �ٶ�.z = ����.z * ����;
        }
    }
    #endregion

    #region ��Ծ������
    private void ������Ծ()
    {
        // �����������߼�
        if (Input.GetKeyDown(KeyCode.Space) && �ѽӵ� && !���ڹ���)
        {
            bool �Ƕ��� = ���ڶ���;

            // ����Ƿ����㹻����
            if (!���㹻����(�Ƕ���))
                return;

            // ���ͷ���Ƿ����ϰ���
            if (�Ƕ���)
            {
                // ������Ҫ����ռ䣬����ܷ���ȫվ��
                if (!����ܷ�վ��())
                {
                    Debug.Log("ͷ�����ϰ���޷�������");
                    return;
                }
            }
            else
            {
                // ��ͨ��Ծ���
                if (!�����Ծ�ռ�())
                {
                    Debug.Log("ͷ�����ϰ���޷���Ծ��");
                    return;
                }
            }

            // Ӧ����Ծ�����������ߣ�
            �ٶ�.y = �Ƕ��� ? ��Ծ���� + ������������ : ��Ծ����;
            �ѽӵ� = false;

            // ������Ծ��Ч
            if (��Ծ��Ч != null)
                ��ƵԴ.PlayOneShot(��Ծ��Ч);

            // ��������
            ������Ծ����(�Ƕ���);

            // ����ʱִ��������
            if (�Ƕ���)
            {
                StopAllCoroutines();
                StartCoroutine(�����������());
            }
        }
        else if (!�ѽӵ�)
        {
            �ٶ�.y += �������ٶ� * Time.deltaTime;
            if (�ٶ�.y < -25f) �ٶ�.y = -25f; // �ն��ٶ�
        }
        else if (�ٶ�.y < 0)
        {
            �ٶ�.y = -1f; // ��������
        }
    }

    // �����������ͨ��Ծ�ռ�
    private bool �����Ծ�ռ�()
    {
        Vector3 ԭ�� = transform.position + ��ɫ������.center + Vector3.up * ���ƫ��;
        float ���߶� = 0.5f; // ��ͨ��Ծ��Ҫ�Ķ���ռ�
        int ������ = ~���Բ�;

        return !Physics.SphereCast(ԭ��, ���뾶, Vector3.up, out _, ���߶�, ������);
    }
    #endregion

    #region ������վ��
    private void ���¶���״̬()
    {
        if (Input.GetKeyDown(���°���) && �ѽӵ� && !���ڹ���)
        {
            if (���ڶ���)
            {
                if (����ܷ�վ��()) StartCoroutine(���ɵ�վ��());
                else Debug.Log("ͷ�����ϰ���޷�վ����");
            }
            else
            {
                StartCoroutine(���ɵ�����());
            }
        }
    }

    public bool ����ܷ�վ��()
    {
        Vector3 ԭ�� = transform.position + ��ɫ������.center + Vector3.up * ���ƫ��;
        float ���߶� = վ���߶� - ��ɫ������.height + 0.1f;
        int ������ = ~���Բ�;

        return !Physics.SphereCast(ԭ��, ���뾶, Vector3.up, out _, ���߶�, ������);
    }

    // ����������������ɣ��������������
    private IEnumerator �����������()
    {
        // ֪ͨ����ϵͳ����׼������
        ����������.SetBool(����_���ڶ���׼��, true);

        ���ڹ��� = true;
        ���ɽ��� = 0;
        float ��ʼ�߶� = ��ɫ������.height;
        float ��ʼ���Y = ������.localPosition.y;

        // ����������ɸ���
        float ��������ʱ�� = ����ʱ�� * 0.6f;

        while (���ɽ��� < 1)
        {
            ���ɽ��� += Time.deltaTime / ��������ʱ��;
            float t = Mathf.SmoothStep(0, 1, ���ɽ���);

            // ������ײ��
            ��ɫ������.height = Mathf.Lerp(��ʼ�߶�, վ���߶�, t);
            ��ɫ������.center = new Vector3(��ʼ����.x, ��ɫ������.height / 2, ��ʼ����.z);

            // �������λ��
            ������.localPosition = new Vector3(0, Mathf.Lerp(��ʼ���Y, վ�����Y, t), 0);
            yield return null;
        }

        // ����״̬
        ���ڶ��� = false;
        ���ڹ��� = false;
        ����������.SetBool(����_���ڶ���׼��, false);
    }

    private IEnumerator ���ɵ�����()
    {
        ���ڹ��� = true;
        ���ɽ��� = 0;
        float ��ʼ�߶� = ��ɫ������.height;
        float ��ʼ���Y = ������.localPosition.y;

        while (���ɽ��� < 1)
        {
            ���ɽ��� += Time.deltaTime / ����ʱ��;
            float t = Mathf.SmoothStep(0, 1, ���ɽ���);

            // ������ײ��
            ��ɫ������.height = Mathf.Lerp(��ʼ�߶�, ���¸߶�, t);
            ��ɫ������.center = new Vector3(��ʼ����.x, ��ɫ������.height / 2, ��ʼ����.z);

            // �������λ��
            ������.localPosition = new Vector3(0, Mathf.Lerp(��ʼ���Y, �������Y, t), 0);
            yield return null;
        }

        // ����״̬
        ���ڶ��� = true;
        ���ڹ��� = false;
    }

    private IEnumerator ���ɵ�վ��()
    {
        ���ڹ��� = true;
        ���ɽ��� = 0;
        float ��ʼ�߶� = ��ɫ������.height;
        float ��ʼ���Y = ������.localPosition.y;

        while (���ɽ��� < 1)
        {
            // �����м���ϰ���
            if (!����ܷ�վ��())
            {
                ���ڹ��� = false;
                yield break;
            }

            ���ɽ��� += Time.deltaTime / ����ʱ��;
            float t = Mathf.SmoothStep(0, 1, ���ɽ���);

            // ������ײ��
            ��ɫ������.height = Mathf.Lerp(��ʼ�߶�, վ���߶�, t);
            ��ɫ������.center = new Vector3(��ʼ����.x, ��ɫ������.height / 2, ��ʼ����.z);

            // �������λ��
            ������.localPosition = new Vector3(0, Mathf.Lerp(��ʼ���Y, վ�����Y, t), 0);
            yield return null;
        }

        // ����״̬
        ���ڶ��� = false;
        ���ڹ��� = false;
    }

    public void ����վ��״̬()
    {
        ��ɫ������.height = վ���߶�;
        ��ɫ������.center = new Vector3(��ʼ����.x, վ���߶� / 2, ��ʼ����.z);
        ���ڶ��� = false;
        ���ڹ��� = false;
        ������.localPosition = new Vector3(0, վ�����Y, 0);
    }
    #endregion

    #region ����Ч������
    private void ���°���Ч��()
    {
        if (!�����ѳ�ʼ�� || !���ð���) return;

        // ���ݶ���״̬����Ŀ�갵��ǿ��
        float Ŀ��ǿ�� = ���ڶ��� ? ���°���ǿ�� : վ������ǿ��;

        // ƽ�����ɰ���ǿ��
        ��ǰ����ǿ�� = Mathf.Lerp(��ǰ����ǿ��, Ŀ��ǿ��,
                                             ���ǹ����ٶ� * Time.deltaTime);

        // Ӧ�õ�����
        ���°��ǲ���();
    }

    private void ���°��ǲ���()
    {
        if (���ǲ��� == null || !���ð���) return;

        // ��ȷ���Ʋ���͸����
        if (���ǲ���.HasProperty("_BaseColor"))
        {
            Color ��ɫ = ���ǲ���.GetColor("_BaseColor");
            ��ɫ.a = ��ǰ����ǿ��;
            ���ǲ���.SetColor("_BaseColor", ��ɫ);
        }
        else if (���ǲ���.HasProperty("_Color"))
        {
            Color ��ɫ = ���ǲ���.color;
            ��ɫ.a = ��ǰ����ǿ��;
            ���ǲ���.color = ��ɫ;
        }
        else
        {
            Debug.LogWarning("���ǲ��ʲ�֧����ɫ͸���ȵ�����Ч�����ܲ�����Ԥ��");
        }
    }
    #endregion

    #region ������UI����
    private void OnGUI()
    {
        // ֻ��ϵͳ����������ʱ���ư���
        if (����ϵͳ���� && ���ð��� && ���ǲ��� != null && ���ǲ���.mainTexture != null)
        {
            // ��ȡ������ɫ
            Color ������ɫ = Color.black;
            if (���ǲ���.HasProperty("_BaseColor"))
            {
                ������ɫ = ���ǲ���.GetColor("_BaseColor");
            }
            else if (���ǲ���.HasProperty("_Color"))
            {
                ������ɫ = ���ǲ���.color;
            }

            // ǿ��Ӧ�õ�ǰ����İ���ǿ��
            ������ɫ.a = ��ǰ����ǿ��;

            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height),
                           ���ǲ���.mainTexture as Texture,
                           ScaleMode.StretchToFill,
                           true,
                           0,
                           ������ɫ,
                           0, 0);
        }

        // ����������
        ����������();
    }

    private void ����������()
    {
        if (!���������ѳ�ʼ��)
        {
            ��ʼ����������();
        }

        // ������������
        float �������� = Mathf.Clamp01(��ǰ���� / �������);

        // ����������λ�ã���Ļ����ƫ�£�
        float ������X = (Screen.width - ���������) / 2;
        float ������Y = (Screen.height / 2) + ����ƫ��Y;

        // ������������䲿��
        Rect ������� = new Rect(
            ������X,
            ������Y,
            ��������� * ��������,
            �������߶�
        );
        GUI.DrawTexture(�������, ��������);

        // �������"I"��λ��
        float ����X = ������X + (��������� * ��������) - ������� / 2;
        float �����߶� = �������߶� * �����߶ȱ���;
        float ����Y = ������Y + (�������߶� - �����߶�) / 2;

        // ���Ƹ��� "I"
        Rect �������� = new Rect(
            ����X,
            ����Y,
            �������,
            �����߶�
        );
        GUI.DrawTexture(��������, ��������);

        // ���ƿ̶���
        ���ƿ̶�(������X, ������Y, ��������� * ��������, �������߶�);
    }

    private void ���ƿ̶�(float x, float y, float width, float height)
    {
        if (�̶����� <= 1 || width <= 0) return;

        float ��� = width / �̶�����;
        float �̶ȸ߶� = height * �̶ȸ߶ȱ���;
        float �̶�Y = y + (height - �̶ȸ߶�) / 2;

        for (int i = 1; i < �̶�����; i++)
        {
            float �̶�X = x + i * ���;

            // ȷ���̶Ȳ�������ǰ����������
            if (�̶�X > x + width) break;

            // ���ƿ̶���
            Rect �̶����� = new Rect(
                �̶�X - �̶ȿ�� / 2,
                �̶�Y,
                �̶ȿ��,
                �̶ȸ߶�
            );
            GUI.DrawTexture(�̶�����, ��������);
        }
    }
    #endregion

    #region ��������
    private void ���¶���()
    {
        bool ���ڲ��� = ������ && �ѽӵ� && !���ڶ���;
        ����������.SetBool(����_���ڲ���, ���ڲ���);
        ����������.SetBool(����_���ڶ���, ���ڶ���);
        ����������.SetBool(����_������Ծ, !�ѽӵ�);

        // ֪ͨ״̬����ǰ�ٶȣ���״̬�жϣ�
        if (״̬�� != null)
        {
            Vector3 ˮƽ�ٶ� = new Vector3(�ٶ�.x, 0, �ٶ�.z);
            ״̬��.SetVelocity(ˮƽ�ٶ�.magnitude, �ٶ�.y);
        }
    }
    #endregion

    #region ���Կ��ӻ�
    private void OnDrawGizmos()
    {
        if (��ɫ������ == null) return;

        // ��������
        Gizmos.color = �ѽӵ� ? Color.green : Color.red;
        Vector3 ԭ�� = transform.position + ��ɫ������.center -
                       Vector3.up * (��ɫ������.height / 2 - ��ɫ������.radius);
        Gizmos.DrawWireSphere(ԭ�� + Vector3.down * ���������, ��ɫ������.radius);

        // ͷ�����
        if (���ڶ��� || ���ڹ���)
        {
            Vector3 ͷ��ԭ�� = transform.position + ��ɫ������.center + Vector3.up * ���ƫ��;
            float ���߶� = վ���߶� - ��ɫ������.height + 0.1f;
            Gizmos.color = ����ܷ�վ��() ? Color.green : Color.red;
            Gizmos.DrawWireSphere(ͷ��ԭ��, ���뾶);
            Gizmos.DrawLine(ͷ��ԭ��, ͷ��ԭ�� + Vector3.up * ���߶�);
        }
        else if (!�ѽӵ�)
        {
            // ��Ծ�ռ�����ӻ�
            Vector3 ͷ��ԭ�� = transform.position + ��ɫ������.center + Vector3.up * ���ƫ��;
            Gizmos.color = �����Ծ�ռ�() ? Color.green : Color.red;
            Gizmos.DrawWireSphere(ͷ��ԭ��, ���뾶);
            Gizmos.DrawLine(ͷ��ԭ��, ͷ��ԭ�� + Vector3.up * 0.5f);
        }

        // �ƶ�����
        if (������)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, Ŀ���ٶ�.normalized * 2f);
        }
    }
    #endregion

    #region �ⲿ�ӿ�
    // ��Ծ��������״̬�����ã�
    public void Jump(bool isCrouchJump = false)
    {
        if (!�ѽӵ� || ���ڹ���) return;

        �ٶ�.y = isCrouchJump ? ��Ծ���� + ������������ : ��Ծ����;
        �ѽӵ� = false;

        if (��Ծ��Ч != null)
            ��ƵԴ.PlayOneShot(��Ծ��Ч);

        ������Ծ����(isCrouchJump);

        if (isCrouchJump && ���ڶ���)
        {
            StopAllCoroutines();
            StartCoroutine(�����������());
        }
    }

    // ���Ľӿ�
    public bool ���ڶ���״̬() => ���ڶ���;
    public bool ���ڹ���״̬() => ���ڹ���;
    public bool �ܹ�վ��() => ����ܷ�վ��();
    public void ����վ��()
    {
        if (����ܷ�վ��())
        {
            StopAllCoroutines();
            ����վ��״̬();
        }
    }

    // Ӣ�Ľӿ�
    public bool IsTransitioning() => ���ڹ���״̬();
    public bool IsCrouching() => ���ڶ���״̬();
    public bool CanStand() => �ܹ�վ��();
    public void StandUpImmediate() => ����վ��();
    #endregion
}
