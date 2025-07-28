using UnityEngine;

namespace InventorySystem
{
    public enum ItemType
    {
        Consumable,   // ����Ʒ
        Tool,         // ����
        Weapon,       // ����
        Armor,        // װ��
        Material,     // ����
        Backpack,     // ����(��չ������)
        Other         // ����
    }

    public enum Rarity
    {
        Common,       // ��ͨ����ɫ��
        Uncommon,     // �Ƿ�����ɫ��
        Rare,         // ϡ�У���ɫ��
        Epic,         // ʷʫ����ɫ��
        Legendary,    // ��˵����ɫ��
        Secret        // ���ܼ�����ɫ��
    }

    // װ��λ��ö��
    public enum EquipmentSlot
    {
        None,         // δװ��
        Head,         // ͷ��
        Chest,        // �ز�
        Belt,         // ����
        Foot          // �Ų�
    }

    [CreateAssetMenu(fileName = "New Item", menuName = "Inventory System/Item")]
    public class Item : ScriptableObject
    {
        [Header("������Ϣ")]
        public int itemID;                 // ��ƷID
        public string itemName;            // ��Ʒ����
        [TextArea] public string description; // ��Ʒ����
        public Sprite icon;                // ��Ʒͼ��
        public GameObject itemPrefab;      // ��ƷԤ����
        public ItemType itemType;          // ��Ʒ����
        public Rarity rarity;              // ϡ�ж�

        [Header("װ������(��Armor��Ч)")]
        public EquipmentSlot equipSlot = EquipmentSlot.None; // װ��λ��
        public float performanceBoost;     // ��������ֵ

        [Header("ռ�ÿռ�")]
        public Vector2Int gridSize = new Vector2Int(1, 1); // ��Ʒ�ڱ�����ռ�õ������С
        public bool allowRotation = true;  // �Ƿ�������ת

        [Header("��������")]
        public bool canBeDropped = true;   // �Ƿ���Զ���
        public bool useCustomDropScale = false; // �Ƿ�ʹ���Զ��嶪������
        public Vector3 dropScale = Vector3.one; // ����ʱ������

        [Header("������չ����(��Backpack��Ч)")]
        public int addWidth = 2;           // ���ӵĿ��
        public int addHeight = 2;          // ���ӵĸ߶�

        [Header("����Ʒ����(��Consumable��Ч)")]
        public int healAmount;             // ������
        public bool canBeUsedInCombat = true; // �Ƿ������ս����ʹ��

        [Header("��������(��Tool��Ч)")]
        public int durability;             // ��ǰ�;�
        public int maxDurability;          // ����;�

        [Header("��������")]
        [HideInInspector] public int itemValue; // ��Ʒ��ֵ������Ʒ���Զ����ɣ�

        /// <summary>
        /// ��ʼ��ʱ�Զ�������Ʒ��ֵ
        /// </summary>
        private void OnEnable()
        {
            GenerateFixedValueByRarity();
        }

        /// <summary>
        /// ����ϡ�ж����ɹ̶������ֵ
        /// </summary>
        private void GenerateFixedValueByRarity()
        {
            // ʹ��itemID��Ϊ������ӣ�ȷ��ͬһID��Ʒ��ֵ�̶�
            System.Random random = new System.Random(itemID);

            switch (rarity)
            {
                case Rarity.Common:
                    itemValue = random.Next(4, 10); // 4-9
                    break;
                case Rarity.Uncommon:
                    itemValue = random.Next(7, 16); // 7-15
                    break;
                case Rarity.Rare:
                    itemValue = random.Next(13, 23); // 13-22
                    break;
                case Rarity.Epic:
                    itemValue = random.Next(20, 39); // 20-38
                    break;
                case Rarity.Legendary:
                    itemValue = random.Next(39, 61); // 39-60
                    break;
                case Rarity.Secret:  // �������ܼ�����ɫ����ֵ��Χ
                    itemValue = random.Next(80, 150); // 61-100�������е�����
                    break;
                default:
                    itemValue = 0;
                    break;
            }
        }

        /// <summary>
        /// �����Ʒ�ߴ��Ƿ���Ч
        /// </summary>
        public bool IsValidSize()
        {
            return gridSize.x > 0 && gridSize.y > 0;
        }

        /// <summary>
        /// ��ȡϡ�жȶ�Ӧ����ɫ���������ܼ���ɫ��
        /// </summary>
        public Color GetRarityColor()
        {
            switch (rarity)
            {
                case Rarity.Common: return new Color(0.2f, 0.2f, 0.2f, 0.8f); // ��ɫ����͸���ȣ�
                case Rarity.Uncommon: return new Color(0, 0.8f, 0, 0.8f); // ����ɫ��������ɫ���ͶȲ�����͸���ȣ�
                case Rarity.Rare: return new Color(0, 0.2f, 0.8f, 0.7f); // ����ɫ
                case Rarity.Epic: return new Color(0.5f, 0, 0.8f, 0.7f); // ����ɫ
                case Rarity.Legendary: return new Color(0.9f, 0.4f, 0, 0.9f); // ����ɫ
                case Rarity.Secret: return new Color(0.9f, 0, 0, 0.6f); // ����ɫ
                default: return new Color(0.8f, 0.8f, 0.8f, 0.7f); // Ĭ�ϵ���ɫ
            }
        }

        /// <summary>
        /// ʹ����Ʒ
        /// </summary>
        public virtual void Use(Inventory inventory)
        {
            switch (itemType)
            {
                case ItemType.Consumable:
                    UseConsumable(inventory);
                    break;
                case ItemType.Tool:
                    UseTool(inventory);
                    break;
                case ItemType.Backpack:
                    UseBackpack(inventory);
                    break;
                case ItemType.Armor:
                    UseArmor(inventory);
                    break;
            }
        }

        /// <summary>
        /// ʹ��װ��(װ������Ӧ��λ)
        /// </summary>
        protected virtual void UseArmor(Inventory inventory)
        {
            if (equipSlot != EquipmentSlot.None)
            {
                inventory.EquipItem(equipSlot, this);
                Debug.Log($"װ����{this.itemName}������{performanceBoost}������");
            }
        }

        /// <summary>
        /// ʹ������Ʒ
        /// </summary>
        protected virtual void UseConsumable(Inventory inventory)
        {
            if (healAmount > 0)
            {
                inventory.Heal(healAmount);
                Debug.Log($"ʹ����{itemName}���ָ���{healAmount}������ֵ");
            }
            else
            {
                Debug.Log($"ʹ����{itemName}");
            }
        }

        /// <summary>
        /// ʹ�ù���
        /// </summary>
        protected virtual void UseTool(Inventory inventory)
        {
            Debug.Log($"ʹ���˹���: {itemName}��ʣ���;�: {durability}/{maxDurability}");
            durability--;

            // ����;ö�Ϊ0��ִ�����߼�
            if (durability <= 0)
            {
                Debug.Log($"{itemName}�Ѿ���!");
            }
        }

        /// <summary>
        /// ʹ�ñ���(��չ��������)
        /// </summary>
        protected virtual void UseBackpack(Inventory inventory)
        {
            if (inventory != null)
            {
                inventory.IncreaseCapacity(addWidth, addHeight);
                Debug.Log($"ʹ����{itemName}����������������{addWidth}x{addHeight}");
            }
        }
    }
}
