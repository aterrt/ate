using UnityEngine;

public class UpperBodyLookByBones : MonoBehaviour
{
    [Header("骨骼引用")]
    [Tooltip("脊柱骨骼（mixamorig:Spine）")]
    public Transform spineBone;
    [Tooltip("上脊柱骨骼（mixamorig:Spine1）")]
    public Transform spine1Bone;
    [Tooltip("颈部骨骼（mixamorig:Spine2）")]
    public Transform spine2Bone;
    [Tooltip("头部骨骼（mixamorig:Head）- 可选")]
    public Transform headBone;

    [Header("相机与角色根节点")]
    public Camera mainCamera;      // 主相机引用
    public Transform playerRoot;   // 角色根节点（控制左右转向）

    [Header("旋转参数设置")]
    [Range(-90, 0)] public float maxDownAngle = -45f; // 最大低头角度（负角度）
    [Range(0, 90)] public float maxUpAngle = 60f;     // 最大抬头角度（正角度）
    public float verticalSensitivity = 2f;            // 鼠标垂直灵敏度
    public float horizontalSensitivity = 2f;          // 鼠标水平灵敏度
    [Tooltip("骨骼旋转平滑度（值越大越平滑）")]
    public float rotationSmoothness = 10f;            // 平滑过渡参数

    private float currentYRotation; // 存储当前Y轴旋转角度（上下）
    private float currentXRotation; // 存储当前X轴旋转角度（左右）
    private float targetYRotation;  // 目标Y轴旋转角度（用于平滑过渡）

    private void Awake()
    {
        // 自动获取组件引用（如果未手动赋值）
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (playerRoot == null)
            playerRoot = transform;

        // 锁定光标到屏幕中心
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // 获取鼠标输入
        float mouseX = Input.GetAxis("Mouse X") * horizontalSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * verticalSensitivity;

        // 1. 处理左右旋转（控制角色根节点转向）
        currentXRotation += mouseX;
        playerRoot.localRotation = Quaternion.Euler(0, currentXRotation, 0);

        // 2. 处理上下旋转（计算目标角度）
        targetYRotation -= mouseY; // 鼠标上移时抬头（角度增大）
        targetYRotation = Mathf.Clamp(targetYRotation, maxDownAngle, maxUpAngle);

        // 平滑过渡到目标角度（减少骨骼旋转的生硬感）
        currentYRotation = Mathf.Lerp(currentYRotation, targetYRotation, Time.deltaTime * rotationSmoothness);

        // 3. 同步相机旋转
        mainCamera.transform.localRotation = Quaternion.Euler(currentYRotation, 0, 0);

        // 4. 直接控制上半身骨骼旋转（核心逻辑）
        ApplyRotationToBones();
    }

    // 将旋转角度应用到上半身骨骼
    private void ApplyRotationToBones()
    {
        // 计算骨骼旋转角度（可根据需要调整各骨骼的旋转比例）
        float spineRotation = currentYRotation * 0.5f;    // 脊柱旋转幅度减半
        float spine1Rotation = currentYRotation * 0.7f;   // 上脊柱旋转70%
        float spine2Rotation = currentYRotation * 0.9f;   // 颈部旋转90%
        float headRotation = currentYRotation;            // 头部完全跟随

        // 应用旋转到各骨骼（只旋转X轴，避免左右偏移）
        if (spineBone != null)
            spineBone.localRotation = Quaternion.Euler(spineRotation, 0, 0);

        if (spine1Bone != null)
            spine1Bone.localRotation = Quaternion.Euler(spine1Rotation, 0, 0);

        if (spine2Bone != null)
            spine2Bone.localRotation = Quaternion.Euler(spine2Rotation, 0, 0);

        if (headBone != null)
            headBone.localRotation = Quaternion.Euler(headRotation, 0, 0);
    }

    // 可选：重置旋转（如暂停游戏时）
    public void ResetRotation()
    {
        currentYRotation = 0;
        targetYRotation = 0;
        currentXRotation = 0;
        playerRoot.localRotation = Quaternion.identity;
        mainCamera.transform.localRotation = Quaternion.identity;

        if (spineBone != null) spineBone.localRotation = Quaternion.identity;
        if (spine1Bone != null) spine1Bone.localRotation = Quaternion.identity;
        if (spine2Bone != null) spine2Bone.localRotation = Quaternion.identity;
        if (headBone != null) headBone.localRotation = Quaternion.identity;
    }
}
