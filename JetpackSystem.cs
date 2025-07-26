using UnityEngine;
using System.Collections;

public class JetpackSystem : MonoBehaviour
{
    [Tooltip("是否启用喷气")] public bool enableJetpack = false;

    #region 喷气参数
    [Tooltip("基础速度")] public float jetSpeed = 15f;
    [Tooltip("空中倍率")] public float airMultiplier = 1.5f;
    [Tooltip("持续时间")] public float jetDuration = 0.5f;
    [Tooltip("冷却时间")] public float cooldown = 5f;
    [Tooltip("喷气按键")] public KeyCode jetKey = KeyCode.V;
    [Tooltip("粒子效果")] public ParticleSystem jetParticles;
    [Tooltip("喷气音效")] public AudioClip jetSound;
    [Tooltip("落地音效")] public AudioClip landingSound;
    #endregion

    #region 轨迹与抖动
    [Tooltip("初始抬升力")] public float initialLift = 2f;
    [Tooltip("轨迹弯曲度")] public float curveStrength = 0.3f;
    [Tooltip("高度曲线")] public AnimationCurve heightCurve;
    [Tooltip("前向抖动")] public float forwardJitter = 0.3f;
    [Tooltip("后向抖动")] public float backwardJitter = 0.4f;
    [Tooltip("左右抖动")] public float sideJitter = 0.35f;
    [Tooltip("斜向倍率")] public float diagonalMultiplier = 0.8f;
    [Tooltip("抖动频率")] public float jitterFreq = 20f;
    #endregion

    #region 序列帧效果
    [Tooltip("序列帧图片")] public Texture2D[] sequenceFrames;
    [Tooltip("帧率")] public float frameRate = 10f;
    [Tooltip("初始大小")] public float startSize = 1f;
    [Tooltip("最终大小")] public float endSize = 1.2f;
    [Tooltip("颜色叠加")] public Color frameTint = new Color(1, 1, 1, 0.8f);
    #endregion

    #region 外部引用
    [Tooltip("角色控制器")] public CharacterController controller;
    [Tooltip("玩家移动脚本")] public PlayerMovement movement;
    [Tooltip("玩家相机")] public Transform playerCamera;
    #endregion

    #region 私有变量
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

    // 序列帧
    private int frameIndex;
    private float frameTimer;
    private bool isPlayingSequence;
    #endregion

    #region 初始化
    private void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        audioSource = gameObject.AddComponent<AudioSource>();
        remainingTime = jetDuration;

        // 初始化高度曲线
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

    #region 外部接口
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
        float t = residualTimer / 3f; // 残留持续3秒
        return Mathf.Lerp(1.5f, 1f, t);
    }

    public void OnLanding()
    {
        if (landingSound != null && !isJeting)
            audioSource.PlayOneShot(landingSound);
    }
    #endregion

    #region 喷气逻辑
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

        // 播放音效
        if (jetSound != null)
        {
            if (stopAudioCoroutine != null) StopCoroutine(stopAudioCoroutine);
            audioSource.clip = jetSound;
            audioSource.Play();
        }

        // 喷气时自动站立
        if (movement != null && movement.IsCrouching() && movement.CanStand())
        {
            movement.StandUpImmediate();
        }

        // 开始序列帧
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

        // 输入方向
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        jetDir = playerCamera.right * h + playerCamera.forward * v;
        jetDir.y = 0;
        if (jetDir.magnitude < 0.1f) jetDir = playerCamera.forward;
        else jetDir.Normalize();

        // 速度计算
        float speed = jetSpeed * (isGrounded ? 1 : airMultiplier);
        velocity.x = jetDir.x * speed;
        velocity.z = jetDir.z * speed;

        // 高度控制
        float heightOffset = heightCurve.Evaluate(jetProgress) * curveStrength;
        velocity.y = initialLift * (1 - jetProgress) + heightOffset;

        // 镜头抖动
        AddJitter();

        if (remainingTime <= 0) EndJet();
    }

    private void EndJet()
    {
        isJeting = false;
        if (jetParticles != null) jetParticles.Stop();
        hasResidual = true;
        residualTimer = 0;

        // 停止音效
        if (audioSource.isPlaying)
        {
            float remaining = audioSource.clip.length - audioSource.time;
            stopAudioCoroutine = StartCoroutine(StopAudioAfter(remaining));
        }
    }
    #endregion

    #region 镜头抖动
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

    #region 冷却与残留
    private void UpdateCooldown()
    {
        if (cooldownRemaining > 0) cooldownRemaining -= Time.deltaTime;
        else if (!isJeting && remainingTime <= 0) cooldownRemaining = cooldown;

        // 残留加速
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

    #region 序列帧效果
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

        // 冷却显示
        if (cooldownRemaining > 0)
        {
            Rect bg = new Rect(20, Screen.height - 60, 200, 30);
            Rect fill = new Rect(20, Screen.height - 60, 200 * (1 - cooldownRemaining / cooldown), 30);
            GUI.Box(bg, "");
            GUI.Box(fill, $"冷却: {Mathf.Ceil(cooldownRemaining)}s");
        }
    }
    #endregion

    #region 调试
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
