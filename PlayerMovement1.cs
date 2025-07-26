using UnityEngine;
using System.Collections;
using UnityEngine.Assertions;
using InventorySystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    #region 基础参数
    [Tooltip("起跳音效")] public AudioClip 跳跃音效;
    [Tooltip("落地音效")] public AudioClip 落地音效;
    [Tooltip("步行速度")] public float 步行速度 = 5f;
    [Tooltip("跑步速度")] public float 跑步速度 = 8f;
    [Tooltip("跳跃力度")] public float 跳跃力度 = 7f;
    [Tooltip("蹲跳额外力度")] public float 蹲跳额外力度 = 2f; // 新增：蹲跳额外高度
    [Tooltip("重力加速度")] public float 重力加速度 = -19.62f;
    #endregion

    #region 碰撞箱与蹲下
    [Tooltip("站立碰撞箱高度")] public float 站立高度 = 2.3f;
    [Tooltip("蹲下碰撞箱高度")] public float 蹲下高度 = 0.9f;
    [Tooltip("蹲下移动速度")] public float 蹲下速度 = 3f;
    [Tooltip("蹲下按键")] public KeyCode 蹲下按键 = KeyCode.LeftControl;
    [Tooltip("蹲站过渡时间")] public float 过渡时间 = 0.2f;
    #endregion

    #region 暗角效果设置
    [Header("蹲下暗角效果")]
    [Tooltip("是否启用暗角效果")] public bool 启用暗角 = true;
    [Tooltip("暗角材质球")]
    public Material 暗角材质;
    [Tooltip("蹲下时暗角强度")] public float 蹲下暗角强度 = 0.8f;
    [Tooltip("站立时暗角强度")] public float 站立暗角强度 = 0f;
    [Tooltip("暗角过渡速度")] public float 暗角过渡速度 = 5f;
    #endregion

    #region 体力系统设置
    [Header("体力系统设置")]
    [Tooltip("最大体力值")] public float 最大体力 = 100f;
    [Tooltip("跑步消耗速率")] public float 跑步消耗速率 = 15f;
    [Tooltip("跳跃消耗值")] public float 跳跃消耗值 = 20f;
    [Tooltip("蹲跳额外消耗")] public float 蹲跳额外消耗 = 10f; // 新增：蹲跳额外消耗
    [Tooltip("体力恢复速率")] public float 恢复速率 = 8f;
    [Tooltip("恢复延迟时间(秒)")] public float 恢复延迟 = 1f;
    [Tooltip("体力条宽度")] public float 体力条宽度 = 200f;
    [Tooltip("体力条高度")] public float 体力条高度 = 8f;
    [Tooltip("体力条颜色")] public Color 体力条颜色 = Color.white;
    [Tooltip("刻度数量")] public int 刻度数量 = 5;
    [Tooltip("刻度宽度")] public float 刻度宽度 = 1f;
    [Tooltip("刻度高度比例")] public float 刻度高度比例 = 0.7f;
    [Tooltip("根部I宽度")] public float 根部宽度 = 6f;
    [Tooltip("根部I高度比例")] public float 根部高度比例 = 1.5f;
    [Tooltip("体力条中心偏移Y值")] public float 中心偏移Y = -150f;
    #endregion

    #region 头顶检测
    [Tooltip("检测半径")] public float 检测半径 = 0.3f;
    [Tooltip("检测偏移量")] public float 检测偏移 = 0.2f;
    [Tooltip("忽略层")] public LayerMask 忽略层;
    #endregion

    #region 相机设置
    [Tooltip("玩家相机")] public Transform 玩家相机;
    [Tooltip("鼠标灵敏度")] public float 鼠标灵敏度 = 2f;
    [Tooltip("站立相机Y轴位置")] public float 站立相机Y = 1.6f;
    [Tooltip("蹲下相机Y轴位置")] public float 蹲下相机Y = 0.8f;
    #endregion

    #region 地面检测
    [Tooltip("地面检测距离")] public float 地面检测距离 = 0.1f;
    [Tooltip("地面层")] public LayerMask 地面层;
    #endregion

    #region 私有变量
    private CharacterController 角色控制器;
    private Animator 动画控制器;
    private AudioSource 音频源;
    private Vector3 速度;
    private float 俯仰角;
    private bool 已接地;
    private bool 之前接地;
    private bool 已落地;
    private Vector3 目标速度;
    private bool 有输入;
    private Vector3 身体前方;

    // 蹲下状态
    private bool 正在蹲下;
    private bool 正在过渡;
    private float 过渡进度;
    private Vector3 初始中心;

    // 暗角效果
    private float 当前暗角强度;
    private bool 暗角已初始化 = false;
    private bool 暗角系统可用 = false;

    // 体力系统变量
    private float 当前体力;
    private float 恢复计时器;
    private bool 正在跑步;
    private Texture2D 体力纹理;
    private bool 体力纹理已初始化 = false;

    // 新增：状态机引用
    private CharacterStateMachine 状态机;

    // 背包系统
    private Inventory 背包系统;
    #endregion

    #region 动画哈希
    private static readonly int 动画_正在步行 = Animator.StringToHash("IsWalking");
    private static readonly int 动画_正在蹲下 = Animator.StringToHash("IsCrouching");
    private static readonly int 动画_正在跳跃 = Animator.StringToHash("IsJumping");
    private static readonly int 动画_正在蹲跳准备 = Animator.StringToHash("IsCrouchJumpPrepare"); // 新增
    #endregion

    #region 初始化
    private void Awake()
    {
        角色控制器 = GetComponent<CharacterController>();
        动画控制器 = GetComponent<Animator>();
        音频源 = gameObject.AddComponent<AudioSource>();
        初始中心 = new Vector3(角色控制器.center.x, 0, 角色控制器.center.z);

        // 初始化体力
        当前体力 = 最大体力;
        恢复计时器 = 0;

        // 验证必要组件
        Assert.IsNotNull(角色控制器, "缺少CharacterController组件！");
        Assert.IsNotNull(玩家相机, "请指定玩家相机！");

        // 初始化暗角效果
        初始化暗角();

        // 获取状态机组件
        状态机 = GetComponent<CharacterStateMachine>();
        if (状态机 == null)
        {
            状态机 = FindObjectOfType<CharacterStateMachine>();
            if (状态机 == null)
            {
                Debug.LogWarning("未找到CharacterStateMachine组件，状态管理可能异常");
            }
        }

        // 获取Inventory组件
        背包系统 = GetComponent<Inventory>();
        if (背包系统 == null)
        {
            背包系统 = FindObjectOfType<Inventory>();
        }
    }

    private void Start()
    {
        设置站立状态();
        锁定鼠标();

        // 初始化层掩码
        if (地面层.value == 0) 地面层 = LayerMask.GetMask("Terrain");
        if (忽略层.value == 0) 忽略层 = 1 << gameObject.layer;
    }

    private void 初始化暗角()
    {
        // 如果禁用暗角效果，直接返回
        if (!启用暗角)
        {
            暗角系统可用 = false;
            return;
        }

        // 尝试获取或创建暗角材质
        if (暗角材质 == null)
        {
            // 优先尝试获取UI专用透明着色器
            Shader 透明着色器 = Shader.Find("UI/Unlit/Transparent");
            // 如果找不到UI着色器，再尝试普通Unlit透明着色器
            if (透明着色器 == null)
            {
                透明着色器 = Shader.Find("Unlit/Transparent");
            }

            // 如果仍然找不到着色器，禁用暗角效果
            if (透明着色器 == null)
            {
                Debug.LogWarning("找不到合适的透明着色器，暗角效果将禁用。请确保项目中存在Unlit/Transparent或UI/Unlit/Transparent着色器。");
                暗角系统可用 = false;
                return;
            }

            // 创建材质并设置基本属性
            暗角材质 = new Material(透明着色器);
            暗角材质.name = "Auto-Created Vignette Material";

            // 创建暗角纹理
            Texture2D 暗角纹理 = new Texture2D(256, 256);
            for (int y = 0; y < 256; y++)
            {
                for (int x = 0; x < 256; x++)
                {
                    // 计算距离中心的距离（0-1范围）
                    float dx = (x / 255f) - 0.5f;
                    float dy = (y / 255f) - 0.5f;
                    float 距离 = Mathf.Sqrt(dx * dx + dy * dy) * 2f;

                    // 边缘更暗，中心透明的径向渐变
                    float alpha = Mathf.Lerp(0, 1, 距离);
                    暗角纹理.SetPixel(x, y, new Color(0, 0, 0, alpha));
                }
            }
            暗角纹理.Apply();
            暗角材质.mainTexture = 暗角纹理;
        }

        // 验证材质是否可用
        if (暗角材质 == null || 暗角材质.shader == null)
        {
            Debug.LogWarning("暗角材质无效，暗角效果将禁用");
            暗角系统可用 = false;
            return;
        }

        // 初始化暗角强度为站立状态
        当前暗角强度 = 站立暗角强度;
        更新暗角材质();

        暗角已初始化 = true;
        暗角系统可用 = true;
    }

    private void 初始化体力纹理()
    {
        体力纹理 = new Texture2D(1, 1);
        体力纹理.SetPixel(0, 0, 体力条颜色);
        体力纹理.Apply();
        体力纹理已初始化 = true;
    }

    private void 锁定鼠标()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    #endregion

    #region 核心更新
    private void Update()
    {
        // 如果背包打开，只更新必要状态，不处理输入
        if (背包系统 != null && 背包系统.isInventoryOpen)
        {
            更新接地状态();
            更新动画();
            return;
        }

        更新视角();
        更新接地状态();

        // 移动逻辑
        更新蹲下状态();
        更新移动();
        更新跳跃();

        // 体力系统更新
        更新体力();

        角色控制器.Move(速度 * Time.deltaTime);
        更新动画();

        // 只在系统可用时更新暗角效果
        if (暗角系统可用)
        {
            更新暗角效果();
        }
    }
    #endregion

    #region 体力系统逻辑
    private void 更新体力()
    {
        // 跑步时消耗体力
        if (正在跑步 && 当前体力 > 0)
        {
            当前体力 -= 跑步消耗速率 * Time.deltaTime;
            恢复计时器 = 恢复延迟;
        }
        // 恢复体力
        else if (当前体力 < 最大体力 && !正在跑步)
        {
            恢复计时器 -= Time.deltaTime;
            if (恢复计时器 <= 0)
            {
                当前体力 += 恢复速率 * Time.deltaTime;
                if (当前体力 > 最大体力)
                    当前体力 = 最大体力;
            }
        }
    }

    private bool 有足够体力(bool 是蹲跳 = false)
    {
        if (是蹲跳)
        {
            return 当前体力 > 跳跃消耗值 + 蹲跳额外消耗;
        }
        return 当前体力 > 0;
    }

    private void 消耗跳跃体力(bool 是蹲跳 = false)
    {
        if (是蹲跳)
        {
            当前体力 -= 跳跃消耗值 + 蹲跳额外消耗;
        }
        else
        {
            当前体力 -= 跳跃消耗值;
        }

        if (当前体力 < 0) 当前体力 = 0;
        恢复计时器 = 恢复延迟;
    }
    #endregion

    #region 相机旋转
    private void 更新视角()
    {
        float 鼠标X = Input.GetAxis("Mouse X") * 鼠标灵敏度;
        float 鼠标Y = Input.GetAxis("Mouse Y") * 鼠标灵敏度;

        // 角色水平旋转
        transform.Rotate(Vector3.up * 鼠标X);

        // 相机垂直旋转
        俯仰角 = Mathf.Clamp(俯仰角 - 鼠标Y, -90f, 90f);
        玩家相机.localRotation = Quaternion.Euler(俯仰角, 0, 0);

        // 保存水平方向
        身体前方 = transform.forward;
        身体前方.y = 0;
        身体前方.Normalize();
    }
    #endregion

    #region 地面检测
    private void 更新接地状态()
    {
        之前接地 = 已接地;
        已接地 = 角色控制器.isGrounded;

        // 二次检测
        if (!已接地)
        {
            Vector3 原点 = transform.position + 角色控制器.center -
                           Vector3.up * (角色控制器.height / 2 - 角色控制器.radius);
            已接地 = Physics.SphereCast(原点, 角色控制器.radius,
                                          Vector3.down, out _, 地面检测距离, 地面层);
        }

        // 落地检测
        已落地 = !之前接地 && 已接地 && 速度.y < -1f;
        if (已落地 && 落地音效 != null)
            音频源.PlayOneShot(落地音效);
    }
    #endregion

    #region 基础移动
    private void 更新移动()
    {
        if (正在过渡) return;

        // 输入检测
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        有输入 = Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f;

        // 移动方向
        Vector3 方向 = transform.right * h + 身体前方 * v;
        方向.y = 0;
        if (方向.magnitude > 0.1f) 方向.Normalize();

        // 速度计算
        bool 可以跑步 = 有足够体力();
        正在跑步 = Input.GetKey(KeyCode.LeftShift) && 可以跑步 && 有输入;
        float 移动速度 = 正在蹲下 ? 蹲下速度 :
                     (正在跑步 ? 跑步速度 : 步行速度);

        目标速度 = 方向 * 移动速度;
        平滑速度();
    }

    private void 平滑速度()
    {
        Vector3 水平速度 = new Vector3(速度.x, 0, 速度.z);

        if (有输入)
        {
            速度.x = Mathf.MoveTowards(水平速度.x, 目标速度.x, 30 * Time.deltaTime);
            速度.z = Mathf.MoveTowards(水平速度.z, 目标速度.z, 30 * Time.deltaTime);
        }
        else
        {
            // 减速
            float 减速 = 30 * Time.deltaTime;
            float 速率 = Mathf.Max(0, 水平速度.magnitude - 减速);
            Vector3 方向 = 水平速度.normalized;
            速度.x = 方向.x * 速率;
            速度.z = 方向.z * 速率;
        }
    }
    #endregion

    #region 跳跃与重力
    private void 更新跳跃()
    {
        // 新增：蹲跳逻辑
        if (Input.GetKeyDown(KeyCode.Space) && 已接地 && !正在过渡)
        {
            bool 是蹲跳 = 正在蹲下;

            // 检查是否有足够体力
            if (!有足够体力(是蹲跳))
                return;

            // 检查头顶是否有障碍物
            if (是蹲跳)
            {
                // 蹲跳需要更多空间，检查能否完全站立
                if (!检查能否站立())
                {
                    Debug.Log("头顶有障碍物，无法蹲跳！");
                    return;
                }
            }
            else
            {
                // 普通跳跃检查
                if (!检查跳跃空间())
                {
                    Debug.Log("头顶有障碍物，无法跳跃！");
                    return;
                }
            }

            // 应用跳跃力（蹲跳更高）
            速度.y = 是蹲跳 ? 跳跃力度 + 蹲跳额外力度 : 跳跃力度;
            已接地 = false;

            // 播放跳跃音效
            if (跳跃音效 != null)
                音频源.PlayOneShot(跳跃音效);

            // 消耗体力
            消耗跳跃体力(是蹲跳);

            // 蹲跳时执行起身动画
            if (是蹲跳)
            {
                StopAllCoroutines();
                StartCoroutine(蹲跳起身过渡());
            }
        }
        else if (!已接地)
        {
            速度.y += 重力加速度 * Time.deltaTime;
            if (速度.y < -25f) 速度.y = -25f; // 终端速度
        }
        else if (速度.y < 0)
        {
            速度.y = -1f; // 地面吸附
        }
    }

    // 新增：检查普通跳跃空间
    private bool 检查跳跃空间()
    {
        Vector3 原点 = transform.position + 角色控制器.center + Vector3.up * 检测偏移;
        float 检测高度 = 0.5f; // 普通跳跃需要的额外空间
        int 层掩码 = ~忽略层;

        return !Physics.SphereCast(原点, 检测半径, Vector3.up, out _, 检测高度, 层掩码);
    }
    #endregion

    #region 蹲下与站立
    private void 更新蹲下状态()
    {
        if (Input.GetKeyDown(蹲下按键) && 已接地 && !正在过渡)
        {
            if (正在蹲下)
            {
                if (检查能否站立()) StartCoroutine(过渡到站立());
                else Debug.Log("头顶有障碍物，无法站立！");
            }
            else
            {
                StartCoroutine(过渡到蹲下());
            }
        }
    }

    public bool 检查能否站立()
    {
        Vector3 原点 = transform.position + 角色控制器.center + Vector3.up * 检测偏移;
        float 检测高度 = 站立高度 - 角色控制器.height + 0.1f;
        int 层掩码 = ~忽略层;

        return !Physics.SphereCast(原点, 检测半径, Vector3.up, out _, 检测高度, 层掩码);
    }

    // 新增：蹲跳起身过渡（更快的起身动画）
    private IEnumerator 蹲跳起身过渡()
    {
        // 通知动画系统正在准备蹲跳
        动画控制器.SetBool(动画_正在蹲跳准备, true);

        正在过渡 = true;
        过渡进度 = 0;
        float 起始高度 = 角色控制器.height;
        float 起始相机Y = 玩家相机.localPosition.y;

        // 蹲跳起身过渡更快
        float 蹲跳过渡时间 = 过渡时间 * 0.6f;

        while (过渡进度 < 1)
        {
            过渡进度 += Time.deltaTime / 蹲跳过渡时间;
            float t = Mathf.SmoothStep(0, 1, 过渡进度);

            // 更新碰撞箱
            角色控制器.height = Mathf.Lerp(起始高度, 站立高度, t);
            角色控制器.center = new Vector3(初始中心.x, 角色控制器.height / 2, 初始中心.z);

            // 更新相机位置
            玩家相机.localPosition = new Vector3(0, Mathf.Lerp(起始相机Y, 站立相机Y, t), 0);
            yield return null;
        }

        // 最终状态
        正在蹲下 = false;
        正在过渡 = false;
        动画控制器.SetBool(动画_正在蹲跳准备, false);
    }

    private IEnumerator 过渡到蹲下()
    {
        正在过渡 = true;
        过渡进度 = 0;
        float 起始高度 = 角色控制器.height;
        float 起始相机Y = 玩家相机.localPosition.y;

        while (过渡进度 < 1)
        {
            过渡进度 += Time.deltaTime / 过渡时间;
            float t = Mathf.SmoothStep(0, 1, 过渡进度);

            // 更新碰撞箱
            角色控制器.height = Mathf.Lerp(起始高度, 蹲下高度, t);
            角色控制器.center = new Vector3(初始中心.x, 角色控制器.height / 2, 初始中心.z);

            // 更新相机位置
            玩家相机.localPosition = new Vector3(0, Mathf.Lerp(起始相机Y, 蹲下相机Y, t), 0);
            yield return null;
        }

        // 最终状态
        正在蹲下 = true;
        正在过渡 = false;
    }

    private IEnumerator 过渡到站立()
    {
        正在过渡 = true;
        过渡进度 = 0;
        float 起始高度 = 角色控制器.height;
        float 起始相机Y = 玩家相机.localPosition.y;

        while (过渡进度 < 1)
        {
            // 过程中检测障碍物
            if (!检查能否站立())
            {
                正在过渡 = false;
                yield break;
            }

            过渡进度 += Time.deltaTime / 过渡时间;
            float t = Mathf.SmoothStep(0, 1, 过渡进度);

            // 更新碰撞箱
            角色控制器.height = Mathf.Lerp(起始高度, 站立高度, t);
            角色控制器.center = new Vector3(初始中心.x, 角色控制器.height / 2, 初始中心.z);

            // 更新相机位置
            玩家相机.localPosition = new Vector3(0, Mathf.Lerp(起始相机Y, 站立相机Y, t), 0);
            yield return null;
        }

        // 最终状态
        正在蹲下 = false;
        正在过渡 = false;
    }

    public void 设置站立状态()
    {
        角色控制器.height = 站立高度;
        角色控制器.center = new Vector3(初始中心.x, 站立高度 / 2, 初始中心.z);
        正在蹲下 = false;
        正在过渡 = false;
        玩家相机.localPosition = new Vector3(0, 站立相机Y, 0);
    }
    #endregion

    #region 暗角效果控制
    private void 更新暗角效果()
    {
        if (!暗角已初始化 || !启用暗角) return;

        // 根据蹲下状态更新目标暗角强度
        float 目标强度 = 正在蹲下 ? 蹲下暗角强度 : 站立暗角强度;

        // 平滑过渡暗角强度
        当前暗角强度 = Mathf.Lerp(当前暗角强度, 目标强度,
                                             暗角过渡速度 * Time.deltaTime);

        // 应用到材质
        更新暗角材质();
    }

    private void 更新暗角材质()
    {
        if (暗角材质 == null || !启用暗角) return;

        // 精确控制材质透明度
        if (暗角材质.HasProperty("_BaseColor"))
        {
            Color 颜色 = 暗角材质.GetColor("_BaseColor");
            颜色.a = 当前暗角强度;
            暗角材质.SetColor("_BaseColor", 颜色);
        }
        else if (暗角材质.HasProperty("_Color"))
        {
            Color 颜色 = 暗角材质.color;
            颜色.a = 当前暗角强度;
            暗角材质.color = 颜色;
        }
        else
        {
            Debug.LogWarning("暗角材质不支持颜色透明度调整，效果可能不符合预期");
        }
    }
    #endregion

    #region 体力条UI绘制
    private void OnGUI()
    {
        // 只在系统可用且启用时绘制暗角
        if (暗角系统可用 && 启用暗角 && 暗角材质 != null && 暗角材质.mainTexture != null)
        {
            // 获取材质颜色
            Color 材质颜色 = Color.black;
            if (暗角材质.HasProperty("_BaseColor"))
            {
                材质颜色 = 暗角材质.GetColor("_BaseColor");
            }
            else if (暗角材质.HasProperty("_Color"))
            {
                材质颜色 = 暗角材质.color;
            }

            // 强制应用当前计算的暗角强度
            材质颜色.a = 当前暗角强度;

            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height),
                           暗角材质.mainTexture as Texture,
                           ScaleMode.StretchToFill,
                           true,
                           0,
                           材质颜色,
                           0, 0);
        }

        // 绘制体力条
        绘制体力条();
    }

    private void 绘制体力条()
    {
        if (!体力纹理已初始化)
        {
            初始化体力纹理();
        }

        // 计算体力比例
        float 体力比例 = Mathf.Clamp01(当前体力 / 最大体力);

        // 计算体力条位置（屏幕中心偏下）
        float 体力条X = (Screen.width - 体力条宽度) / 2;
        float 体力条Y = (Screen.height / 2) + 中心偏移Y;

        // 绘制体力条填充部分
        Rect 填充区域 = new Rect(
            体力条X,
            体力条Y,
            体力条宽度 * 体力比例,
            体力条高度
        );
        GUI.DrawTexture(填充区域, 体力纹理);

        // 计算根部"I"的位置
        float 根部X = 体力条X + (体力条宽度 * 体力比例) - 根部宽度 / 2;
        float 根部高度 = 体力条高度 * 根部高度比例;
        float 根部Y = 体力条Y + (体力条高度 - 根部高度) / 2;

        // 绘制根部 "I"
        Rect 根部区域 = new Rect(
            根部X,
            根部Y,
            根部宽度,
            根部高度
        );
        GUI.DrawTexture(根部区域, 体力纹理);

        // 绘制刻度线
        绘制刻度(体力条X, 体力条Y, 体力条宽度 * 体力比例, 体力条高度);
    }

    private void 绘制刻度(float x, float y, float width, float height)
    {
        if (刻度数量 <= 1 || width <= 0) return;

        float 间隔 = width / 刻度数量;
        float 刻度高度 = height * 刻度高度比例;
        float 刻度Y = y + (height - 刻度高度) / 2;

        for (int i = 1; i < 刻度数量; i++)
        {
            float 刻度X = x + i * 间隔;

            // 确保刻度不超过当前体力条长度
            if (刻度X > x + width) break;

            // 绘制刻度线
            Rect 刻度区域 = new Rect(
                刻度X - 刻度宽度 / 2,
                刻度Y,
                刻度宽度,
                刻度高度
            );
            GUI.DrawTexture(刻度区域, 体力纹理);
        }
    }
    #endregion

    #region 动画更新
    private void 更新动画()
    {
        bool 正在步行 = 有输入 && 已接地 && !正在蹲下;
        动画控制器.SetBool(动画_正在步行, 正在步行);
        动画控制器.SetBool(动画_正在蹲下, 正在蹲下);
        动画控制器.SetBool(动画_正在跳跃, !已接地);

        // 通知状态机当前速度（供状态判断）
        if (状态机 != null)
        {
            Vector3 水平速度 = new Vector3(速度.x, 0, 速度.z);
            状态机.SetVelocity(水平速度.magnitude, 速度.y);
        }
    }
    #endregion

    #region 调试可视化
    private void OnDrawGizmos()
    {
        if (角色控制器 == null) return;

        // 地面检测球
        Gizmos.color = 已接地 ? Color.green : Color.red;
        Vector3 原点 = transform.position + 角色控制器.center -
                       Vector3.up * (角色控制器.height / 2 - 角色控制器.radius);
        Gizmos.DrawWireSphere(原点 + Vector3.down * 地面检测距离, 角色控制器.radius);

        // 头顶检测
        if (正在蹲下 || 正在过渡)
        {
            Vector3 头部原点 = transform.position + 角色控制器.center + Vector3.up * 检测偏移;
            float 检测高度 = 站立高度 - 角色控制器.height + 0.1f;
            Gizmos.color = 检查能否站立() ? Color.green : Color.red;
            Gizmos.DrawWireSphere(头部原点, 检测半径);
            Gizmos.DrawLine(头部原点, 头部原点 + Vector3.up * 检测高度);
        }
        else if (!已接地)
        {
            // 跳跃空间检测可视化
            Vector3 头部原点 = transform.position + 角色控制器.center + Vector3.up * 检测偏移;
            Gizmos.color = 检查跳跃空间() ? Color.green : Color.red;
            Gizmos.DrawWireSphere(头部原点, 检测半径);
            Gizmos.DrawLine(头部原点, 头部原点 + Vector3.up * 0.5f);
        }

        // 移动方向
        if (有输入)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, 目标速度.normalized * 2f);
        }
    }
    #endregion

    #region 外部接口
    // 跳跃方法（供状态机调用）
    public void Jump(bool isCrouchJump = false)
    {
        if (!已接地 || 正在过渡) return;

        速度.y = isCrouchJump ? 跳跃力度 + 蹲跳额外力度 : 跳跃力度;
        已接地 = false;

        if (跳跃音效 != null)
            音频源.PlayOneShot(跳跃音效);

        消耗跳跃体力(isCrouchJump);

        if (isCrouchJump && 正在蹲下)
        {
            StopAllCoroutines();
            StartCoroutine(蹲跳起身过渡());
        }
    }

    // 中文接口
    public bool 正在蹲下状态() => 正在蹲下;
    public bool 正在过渡状态() => 正在过渡;
    public bool 能够站立() => 检查能否站立();
    public void 立即站立()
    {
        if (检查能否站立())
        {
            StopAllCoroutines();
            设置站立状态();
        }
    }

    // 英文接口
    public bool IsTransitioning() => 正在过渡状态();
    public bool IsCrouching() => 正在蹲下状态();
    public bool CanStand() => 能够站立();
    public void StandUpImmediate() => 立即站立();
    #endregion
}
