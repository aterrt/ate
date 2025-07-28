using UnityEngine;

namespace InventorySystem
{
    [RequireComponent(typeof(Collider))]
    public class PickupItem : MonoBehaviour
    {
        [Header("��Ʒ����")]
        public Item item;                  // ��������Ʒ����
        public int amount = 1;             // ʰȡ����
        public float pickupRange = 2f;     // ʰȡ��Χ
        public bool useCollision = true;   // �Ƿ�������ײ���

        [Header("�Ӿ�����")]
        public bool useCustomModel = false; // �Ƿ�ʹ���Զ���ģ��
        public GameObject customModel;      // �Զ���ģ��

        private Inventory inventorySystem;  // ����ϵͳ����
        private Transform playerTransform;  // ���λ������
        private Vector3 originalPosition;   // ��ʼλ��
        private bool isPickedUp = false;    // �Ƿ��ѱ�ʰȡ
        private Renderer itemRenderer;      // ��Ʒ��Ⱦ��
        private Collider itemCollider;      // ��Ʒ��ײ��
        private TextMesh pickupText;        // ʰȡ��ʾ�ı�

        private void Awake()
        {
            // �Զ���Ӳ�����Collider���
            EnsureColliderExists();

            // ��ȡ�������
            itemRenderer = GetComponent<Renderer>();
            itemCollider = GetComponent<Collider>();

            // ������������/������ײ��
            if (itemCollider != null)
            {
                itemCollider.enabled = useCollision;
                // ȷ����ײ���Ǵ�����
                if (itemCollider is BoxCollider boxCollider)
                    boxCollider.isTrigger = true;
                else if (itemCollider is SphereCollider sphereCollider)
                    sphereCollider.isTrigger = true;
                else if (itemCollider is CapsuleCollider capsuleCollider)
                    capsuleCollider.isTrigger = true;
            }

            // ����ʰȡ��ʾ�ı�
            CreatePickupText();

            // �����Զ���ģ��
            if (useCustomModel && customModel != null)
            {
                // ����Ĭ����Ⱦ����������ڣ�
                if (itemRenderer != null)
                    itemRenderer.enabled = false;

                // ʵ�����Զ���ģ����Ϊ������
                GameObject modelInstance = Instantiate(customModel, transform);
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;

                // ��ȡ�µ���Ⱦ������
                itemRenderer = modelInstance.GetComponent<Renderer>();
            }

            originalPosition = transform.position;
        }

        /// <summary>
        /// ����ʰȡ��ʾ�ı�
        /// </summary>
        private void CreatePickupText()
        {
            GameObject textObject = new GameObject("PickupText");
            textObject.transform.parent = transform;
            textObject.transform.localPosition = new Vector3(0, 0.5f, 0); // ��΢������Ʒ
            textObject.transform.localRotation = Quaternion.identity;

            pickupText = textObject.AddComponent<TextMesh>();
            pickupText.anchor = TextAnchor.MiddleCenter;
            pickupText.alignment = TextAlignment.Center;
            pickupText.fontSize = 12;
            pickupText.color = Color.white;
            pickupText.text = ""; // ��ʼ����
            pickupText.characterSize = 0.1f;
        }

        /// <summary>
        /// ȷ��������Collider������Զ���ӣ�
        /// </summary>
        private void EnsureColliderExists()
        {
            Collider existingCollider = GetComponent<Collider>();
            if (existingCollider == null)
            {
                // �Զ����SphereCollider��ΪĬ����ײ��
                SphereCollider newCollider = gameObject.AddComponent<SphereCollider>();
                newCollider.radius = 0.3f;  // ���ú��ʵİ뾶
                newCollider.isTrigger = true; // ȷ���Ǵ�����
            }
        }

        private void Start()
        {
            // �����Ʒ�����Ƿ�������
            if (item == null)
            {
                Debug.LogError("PickupItem: δָ��������Item���ݣ�����Inspector��������Ʒ����", gameObject);

                // ����������Ķ�������������
                if (itemRenderer != null)
                    itemRenderer.enabled = false;
                if (itemCollider != null)
                    itemCollider.enabled = false;
                if (pickupText != null)
                    pickupText.text = "";

                return;
            }

            // ���ұ���ϵͳ
            FindInventorySystem();

            // �������
            FindPlayer();
        }

        private void Update()
        {
            // ���δ������Ʒ���ݣ���ִ���κ��߼�
            if (item == null || isPickedUp)
                return;



            // ����ʰȡ��ʾ��ʾ
            UpdatePickupText();

            // �Զ�ʰȡ���
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
        /// ����ʰȡ��ʾ�ı���ʾ
        /// </summary>
        private void UpdatePickupText()
        {
            if (pickupText == null || item == null) return;

            if (playerTransform != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                bool inRange = distanceToPlayer <= pickupRange;

                pickupText.text = inRange ? $"��Eʰȡ {item.itemName}" : "";

                // ʼ���������
                
                {
                    
                }
            }
        }



        /// <summary>
        /// ���ұ���ϵͳ
        /// </summary>
        private void FindInventorySystem()
        {
            if (inventorySystem == null)
            {
                // ���Դ�������ϲ���
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    inventorySystem = player.GetComponent<Inventory>();
                }

                // ���û�ҵ������Բ��ҳ������κα���ϵͳ
                if (inventorySystem == null)
                {
                    Inventory[] inventories = FindObjectsOfType<Inventory>();
                    if (inventories.Length > 0)
                    {
                        inventorySystem = inventories[0];
                    }
                    else
                    {
                        Debug.LogError("PickupItem: ������δ�ҵ�Inventory�����", gameObject);
                    }
                }
            }
        }

        /// <summary>
        /// �������
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
                    Debug.LogWarning("PickupItem: δ�ҵ���ң���ǩΪPlayer�Ķ���", gameObject);
                }
            }
        }

        /// <summary>
        /// ����ҽ��봥����
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (useCollision && other.CompareTag("Player") && !isPickedUp && item != null)
            {
                playerTransform = other.transform;
            }
        }

        /// <summary>
        /// ������뿪������
        /// </summary>
        private void OnTriggerExit(Collider other)
        {
            if (useCollision && other.CompareTag("Player") && pickupText != null)
            {
                pickupText.text = "";
            }
        }

        /// <summary>
        /// ʰȡ��Ʒ�߼�
        /// </summary>
        public void Pickup()
        {
            // ��ȫ���
            if (inventorySystem == null)
            {
                Debug.LogError("PickupItem: ����ϵͳ����Ϊ�գ��޷�ʰȡ��Ʒ", gameObject);
                FindInventorySystem(); // �ٴγ��Բ���
                return;
            }

            if (item == null)
            {
                Debug.LogError("PickupItem: ��Ʒ����Ϊ�գ��޷�ʰȡ", gameObject);
                return;
            }

            if (isPickedUp)
                return;

            // �����Զ�װ������ӵ�����
            bool success = inventorySystem.TryEquipOrAddItem(item);

            if (success)
            {
                isPickedUp = true;
                // ������Ʒ
                if (itemRenderer != null)
                    itemRenderer.enabled = false;
                if (itemCollider != null)
                    itemCollider.enabled = false;
                if (pickupText != null)
                    pickupText.text = "";

                // �ؼ��޸ģ���ʾ��Ʒ��ֵ����
                if (ItemPopupManager.Instance != null)
                {
                    ItemPopupManager.Instance.ShowItemPopup(item);
                }

                // �ӳ����ٱ���UI����
                Destroy(gameObject, 0.1f);
            }
            else
            {
                Debug.Log($"�޷�ʰȡ {item.itemName}�������ռ䲻�㣡");
                // ��ʾ��ʾ��Ϣ
                if (pickupText != null)
                {
                    pickupText.text = "�����ռ䲻��";
                    pickupText.color = Color.red;
                }
            }
        }

        /// <summary>
        /// �ڱ༭���л���ʰȡ��Χ gizmo
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, pickupRange);
        }
    }
}
