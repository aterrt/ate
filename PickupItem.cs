using UnityEngine;

namespace InventorySystem
{
    [RequireComponent(typeof(Collider))]
    public class PickupItem : MonoBehaviour
    {
        [Header("物品设置")]
        public Item item;                  // 关联的物品数据
        public int amount = 1;             // 拾取数量
        public float pickupRange = 2f;     // 拾取范围
        public bool useCollision = true;   // 是否启用碰撞检测

        [Header("视觉设置")]
        public bool useCustomModel = false; // 是否使用自定义模型
        public GameObject customModel;      // 自定义模型

        private Inventory inventorySystem;  // 背包系统引用
        private Transform playerTransform;  // 玩家位置引用
        private Vector3 originalPosition;   // 初始位置
        private bool isPickedUp = false;    // 是否已被拾取
        private Renderer itemRenderer;      // 物品渲染器
        private Collider itemCollider;      // 物品碰撞器
        private TextMesh pickupText;        // 拾取提示文本

        private void Awake()
        {
            // 自动添加并配置Collider组件
            EnsureColliderExists();

            // 获取组件引用
            itemRenderer = GetComponent<Renderer>();
            itemCollider = GetComponent<Collider>();

            // 根据设置启用/禁用碰撞器
            if (itemCollider != null)
            {
                itemCollider.enabled = useCollision;
                // 确保碰撞器是触发器
                if (itemCollider is BoxCollider boxCollider)
                    boxCollider.isTrigger = true;
                else if (itemCollider is SphereCollider sphereCollider)
                    sphereCollider.isTrigger = true;
                else if (itemCollider is CapsuleCollider capsuleCollider)
                    capsuleCollider.isTrigger = true;
            }

            // 创建拾取提示文本
            CreatePickupText();

            // 处理自定义模型
            if (useCustomModel && customModel != null)
            {
                // 禁用默认渲染器（如果存在）
                if (itemRenderer != null)
                    itemRenderer.enabled = false;

                // 实例化自定义模型作为子物体
                GameObject modelInstance = Instantiate(customModel, transform);
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;

                // 获取新的渲染器引用
                itemRenderer = modelInstance.GetComponent<Renderer>();
            }

            originalPosition = transform.position;
        }

        /// <summary>
        /// 创建拾取提示文本
        /// </summary>
        private void CreatePickupText()
        {
            GameObject textObject = new GameObject("PickupText");
            textObject.transform.parent = transform;
            textObject.transform.localPosition = new Vector3(0, 0.5f, 0); // 稍微高于物品
            textObject.transform.localRotation = Quaternion.identity;

            pickupText = textObject.AddComponent<TextMesh>();
            pickupText.anchor = TextAnchor.MiddleCenter;
            pickupText.alignment = TextAlignment.Center;
            pickupText.fontSize = 12;
            pickupText.color = Color.white;
            pickupText.text = ""; // 初始隐藏
            pickupText.characterSize = 0.1f;
        }

        /// <summary>
        /// 确保物体有Collider组件（自动添加）
        /// </summary>
        private void EnsureColliderExists()
        {
            Collider existingCollider = GetComponent<Collider>();
            if (existingCollider == null)
            {
                // 自动添加SphereCollider作为默认碰撞器
                SphereCollider newCollider = gameObject.AddComponent<SphereCollider>();
                newCollider.radius = 0.3f;  // 设置合适的半径
                newCollider.isTrigger = true; // 确保是触发器
            }
        }

        private void Start()
        {
            // 检查物品数据是否已设置
            if (item == null)
            {
                Debug.LogError("PickupItem: 未指定关联的Item数据！请在Inspector中设置物品引用", gameObject);

                // 禁用有问题的对象避免后续错误
                if (itemRenderer != null)
                    itemRenderer.enabled = false;
                if (itemCollider != null)
                    itemCollider.enabled = false;
                if (pickupText != null)
                    pickupText.text = "";

                return;
            }

            // 查找背包系统
            FindInventorySystem();

            // 查找玩家
            FindPlayer();
        }

        private void Update()
        {
            // 如果未设置物品数据，不执行任何逻辑
            if (item == null || isPickedUp)
                return;



            // 更新拾取提示显示
            UpdatePickupText();

            // 自动拾取检测
            if (playerTransform != null && inventorySystem != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                if (distanceToPlayer <= pickupRange && Input.GetKeyDown(KeyCode.E))
                {
                    Pickup();
                }
            }
        }

        /// <summary>
        /// 更新拾取提示文本显示
        /// </summary>
        private void UpdatePickupText()
        {
            if (pickupText == null || item == null) return;

            if (playerTransform != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                bool inRange = distanceToPlayer <= pickupRange;

                pickupText.text = inRange ? $"按E拾取 {item.itemName}" : "";

                // 始终面向玩家
                
                {
                    
                }
            }
        }



        /// <summary>
        /// 查找背包系统
        /// </summary>
        private void FindInventorySystem()
        {
            if (inventorySystem == null)
            {
                // 尝试从玩家身上查找
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    inventorySystem = player.GetComponent<Inventory>();
                }

                // 如果没找到，尝试查找场景中任何背包系统
                if (inventorySystem == null)
                {
                    Inventory[] inventories = FindObjectsOfType<Inventory>();
                    if (inventories.Length > 0)
                    {
                        inventorySystem = inventories[0];
                    }
                    else
                    {
                        Debug.LogError("PickupItem: 场景中未找到Inventory组件！", gameObject);
                    }
                }
            }
        }

        /// <summary>
        /// 查找玩家
        /// </summary>
        private void FindPlayer()
        {
            if (playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerTransform = player.transform;
                }
                else
                {
                    Debug.LogWarning("PickupItem: 未找到玩家（标签为Player的对象）", gameObject);
                }
            }
        }

        /// <summary>
        /// 当玩家进入触发器
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (useCollision && other.CompareTag("Player") && !isPickedUp && item != null)
            {
                playerTransform = other.transform;
            }
        }

        /// <summary>
        /// 当玩家离开触发器
        /// </summary>
        private void OnTriggerExit(Collider other)
        {
            if (useCollision && other.CompareTag("Player") && pickupText != null)
            {
                pickupText.text = "";
            }
        }

        /// <summary>
        /// 拾取物品逻辑
        /// </summary>
        public void Pickup()
        {
            // 安全检查
            if (inventorySystem == null)
            {
                Debug.LogError("PickupItem: 背包系统引用为空，无法拾取物品", gameObject);
                FindInventorySystem(); // 再次尝试查找
                return;
            }

            if (item == null)
            {
                Debug.LogError("PickupItem: 物品数据为空，无法拾取", gameObject);
                return;
            }

            if (isPickedUp)
                return;

            // 尝试自动装备或添加到背包
            bool success = inventorySystem.TryEquipOrAddItem(item);

            if (success)
            {
                isPickedUp = true;
                // 隐藏物品
                if (itemRenderer != null)
                    itemRenderer.enabled = false;
                if (itemCollider != null)
                    itemCollider.enabled = false;
                if (pickupText != null)
                    pickupText.text = "";

                // 关键修改：显示物品价值弹窗
                if (ItemPopupManager.Instance != null)
                {
                    ItemPopupManager.Instance.ShowItemPopup(item);
                }

                // 延迟销毁避免UI错误
                Destroy(gameObject, 0.1f);
            }
            else
            {
                Debug.Log($"无法拾取 {item.itemName}：背包空间不足！");
                // 显示提示信息
                if (pickupText != null)
                {
                    pickupText.text = "背包空间不足";
                    pickupText.color = Color.red;
                }
            }
        }

        /// <summary>
        /// 在编辑器中绘制拾取范围 gizmo
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, pickupRange);
        }
    }
}
