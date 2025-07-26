using UnityEngine;
using System.Collections;

public class JetpackSystem : MonoBehaviour
{
    [Tooltip("�Ƿ���������")] public bool enableJetpack = false;

    #region ��������
    [Tooltip("�����ٶ�")] public float jetSpeed = 15f;
    [Tooltip("���б���")] public float airMultiplier = 1.5f;
    [Tooltip("����ʱ��")] public float jetDuration = 0.5f;
    [Tooltip("��ȴʱ��")] public float cooldown = 5f;
    [Tooltip("��������")] public KeyCode jetKey = KeyCode.V;
    [Tooltip("����Ч��")] public ParticleSystem jetParticles;
    [Tooltip("������Ч")] public AudioClip jetSound;
    [Tooltip("�����Ч")] public AudioClip landingSound;
    #endregion

    #region �켣�붶��
    [Tooltip("��ʼ̧����")] public float initialLift = 2f;
    [Tooltip("�켣������")] public float curveStrength = 0.3f;
    [Tooltip("�߶�����")] public AnimationCurve heightCurve;
    [Tooltip("ǰ�򶶶�")] public float forwardJitter = 0.3f;
    [Tooltip("���򶶶�")] public float backwardJitter = 0.4f;
    [Tooltip("���Ҷ���")] public float sideJitter = 0.35f;
    [Tooltip("б����")] public float diagonalMultiplier = 0.8f;
    [Tooltip("����Ƶ��")] public float jitterFreq = 20f;
    #endregion

    #region ����֡Ч��
    [Tooltip("����֡ͼƬ")] public Texture2D[] sequenceFrames;
    [Tooltip("֡��")] public float frameRate = 10f;
    [Tooltip("��ʼ��С")] public float startSize = 1f;
    [Tooltip("���մ�С")] public float endSize = 1.2f;
    [Tooltip("��ɫ����")] public Color frameTint = new Color(1, 1, 1, 0.8f);
    #endregion

    #region �ⲿ����
    [Tooltip("��ɫ������")] public CharacterController controller;
    [Tooltip("����ƶ��ű�")] public PlayerMovement movement;
    [Tooltip("������")] public Transform playerCamera;
    #endregion

    #region ˽�б���
    private Vector3 velocity;
    private bool isJeting;
    private float remainingTime;
    private float cooldownRemaining;
    private float jetProgress;
    private Vector3 jetDir;
    private AudioSource audioSource;
    private float pitchAngle;
    private bool hasResidual;
    private float residualTimer;
    private Coroutine stopAudioCoroutine;

    // ����֡
    private int frameIndex;
    private float frameTimer;
    private bool isPlayingSequence;
    #endregion

    #region ��ʼ��
    private void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        audioSource = gameObject.AddComponent<AudioSource>();
        remainingTime = jetDuration;

        // ��ʼ���߶�����
        if (heightCurve == null || heightCurve.keys.Length == 0)
        {
            heightCurve = new AnimationCurve(
                new Keyframe(0, 0),
                new Keyframe(0.5f, 1),
                new Keyframe(1, 0)
            );
        }
    }
    #endregion

    #region �ⲿ�ӿ�
    public void UpdateJetState(Vector3 currentVel, bool isGrounded, float currentPitch, Vector3 forwardDir)
    {
        if (!enableJetpack) return;

        velocity = currentVel;
        pitchAngle = currentPitch;
        UpdateJetpack(isGrounded);
        UpdateCooldown();
        UpdateSequence();
    }

    public Vector3 GetJetVelocity() => velocity;
    public bool IsJeting() => isJeting;
    public float GetResidualMultiplier()
    {
        if (!hasResidual) return 1f;
        float t = residualTimer / 3f; // ��������3��
        return Mathf.Lerp(1.5f, 1f, t);
    }

    public void OnLanding()
    {
        if (landingSound != null && !isJeting)
            audioSource.PlayOneShot(landingSound);
    }
    #endregion

    #region �����߼�
    private void UpdateJetpack(bool isGrounded)
    {
        if (movement != null && movement.IsTransitioning()) return;

        bool canJet = cooldownRemaining <= 0 && remainingTime > 0 && Input.GetKeyDown(jetKey);

        if (canJet && !isJeting)
        {
            StartJet(isGrounded);
        }
        else if (isJeting)
        {
            UpdateJetMovement(isGrounded);
        }
        else if (cooldownRemaining <= 0 && remainingTime <= 0)
        {
            remainingTime = jetDuration;
        }
    }

    private void StartJet(bool isGrounded)
    {
        isJeting = true;
        jetProgress = 0;
        if (jetParticles != null) jetParticles.Play();

        // ������Ч
        if (jetSound != null)
        {
            if (stopAudioCoroutine != null) StopCoroutine(stopAudioCoroutine);
            audioSource.clip = jetSound;
            audioSource.Play();
        }

        // ����ʱ�Զ�վ��
        if (movement != null && movement.IsCrouching() && movement.CanStand())
        {
            movement.StandUpImmediate();
        }

        // ��ʼ����֡
        if (sequenceFrames != null && sequenceFrames.Length > 0)
        {
            isPlayingSequence = true;
            frameIndex = 0;
            frameTimer = 0;
        }
    }

    private void UpdateJetMovement(bool isGrounded)
    {
        remainingTime -= Time.deltaTime;
        jetProgress = 1 - (remainingTime / jetDuration);

        // ���뷽��
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        jetDir = playerCamera.right * h + playerCamera.forward * v;
        jetDir.y = 0;
        if (jetDir.magnitude < 0.1f) jetDir = playerCamera.forward;
        else jetDir.Normalize();

        // �ٶȼ���
        float speed = jetSpeed * (isGrounded ? 1 : airMultiplier);
        velocity.x = jetDir.x * speed;
        velocity.z = jetDir.z * speed;

        // �߶ȿ���
        float heightOffset = heightCurve.Evaluate(jetProgress) * curveStrength;
        velocity.y = initialLift * (1 - jetProgress) + heightOffset;

        // ��ͷ����
        AddJitter();

        if (remainingTime <= 0) EndJet();
    }

    private void EndJet()
    {
        isJeting = false;
        if (jetParticles != null) jetParticles.Stop();
        hasResidual = true;
        residualTimer = 0;

        // ֹͣ��Ч
        if (audioSource.isPlaying)
        {
            float remaining = audioSource.clip.length - audioSource.time;
            stopAudioCoroutine = StartCoroutine(StopAudioAfter(remaining));
        }
    }
    #endregion

    #region ��ͷ����
    private void AddJitter()
    {
        if (playerCamera == null || movement == null) return;

        float jitterTime = Time.time * jitterFreq;
        float hDir = jetDir.x;
        float vDir = jetDir.z;

        float strength = 0;
        Vector2 dir = Vector2.zero;

        if (vDir > 0.1f && Mathf.Abs(vDir) > Mathf.Abs(hDir))
        {
            strength = forwardJitter;
            dir = new Vector2(0, -1);
        }
        else if (vDir < -0.1f && Mathf.Abs(vDir) > Mathf.Abs(hDir))
        {
            strength = backwardJitter;
            dir = new Vector2(0, 1);
        }
        else if (hDir > 0.1f)
        {
            strength = sideJitter;
            dir = new Vector2(-1, 0);
        }
        else if (hDir < -0.1f)
        {
            strength = sideJitter;
            dir = new Vector2(1, 0);
        }
        else if (Mathf.Abs(hDir) > 0.1f && Mathf.Abs(vDir) > 0.1f)
        {
            strength = (forwardJitter + sideJitter) * 0.5f * diagonalMultiplier;
            dir = new Vector2(hDir > 0 ? -1 : 1, vDir > 0 ? -1 : 1).normalized;
        }

        if (strength > 0)
        {
            float jitterX = Mathf.Sin(jitterTime) * strength * dir.x;
            float jitterY = Mathf.Cos(jitterTime) * strength * dir.y;

            movement.transform.Rotate(Vector3.up * jitterX * 0.5f);
            pitchAngle -= jitterY * 0.5f;
            pitchAngle = Mathf.Clamp(pitchAngle, -90f, 90f);
            playerCamera.localRotation = Quaternion.Euler(pitchAngle, 0, 0);
        }
    }
    #endregion

    #region ��ȴ�����
    private void UpdateCooldown()
    {
        if (cooldownRemaining > 0) cooldownRemaining -= Time.deltaTime;
        else if (!isJeting && remainingTime <= 0) cooldownRemaining = cooldown;

        // ��������
        if (hasResidual)
        {
            residualTimer += Time.deltaTime;
            if (residualTimer >= 3f) hasResidual = false;
        }
    }

    private IEnumerator StopAudioAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (audioSource != null) audioSource.Stop();
        stopAudioCoroutine = null;
    }
    #endregion

    #region ����֡Ч��
    private void UpdateSequence()
    {
        if (!isPlayingSequence || sequenceFrames == null || sequenceFrames.Length == 0) return;

        float interval = 1f / frameRate;
        frameTimer += Time.deltaTime;
        if (frameTimer >= interval)
        {
            frameTimer = 0;
            frameIndex++;
            if (frameIndex >= sequenceFrames.Length) isPlayingSequence = false;
        }
    }

    private void OnGUI()
    {
        if (!enableJetpack || !isPlayingSequence || sequenceFrames == null || frameIndex >= sequenceFrames.Length) return;

        GUI.color = frameTint;
        float progress = (float)frameIndex / sequenceFrames.Length;
        float size = Mathf.Lerp(startSize, endSize, progress);

        float w = Screen.width * size;
        float h = Screen.height * size;
        Rect rect = new Rect((Screen.width - w) / 2, (Screen.height - h) / 2, w, h);
        GUI.DrawTexture(rect, sequenceFrames[frameIndex], ScaleMode.StretchToFill);
        GUI.color = Color.white;

        // ��ȴ��ʾ
        if (cooldownRemaining > 0)
        {
            Rect bg = new Rect(20, Screen.height - 60, 200, 30);
            Rect fill = new Rect(20, Screen.height - 60, 200 * (1 - cooldownRemaining / cooldown), 30);
            GUI.Box(bg, "");
            GUI.Box(fill, $"��ȴ: {Mathf.Ceil(cooldownRemaining)}s");
        }
    }
    #endregion

    #region ����
    private void OnDrawGizmos()
    {
        if (isJeting && controller != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, jetDir * 2f);
        }
    }
    #endregion
}
