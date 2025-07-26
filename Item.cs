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
        Backpack,     // ��������չ�����ã�
        Other         // ����
    }

    public enum Rarity
    {
        Common,       // ��ͨ
        Uncommon,     // ������
        Rare,         // ϡ��
        Epic,         // ʷʫ
        Legendary     // ��˵
    }

    // װ����λö��
    public enum EquipmentSlot
    {
        None,         // ��װ��
        Head,         // ͷ��
        Chest,        // ����
        Belt,         // ����
        Foot          // Ь��
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

        [Header("װ�����ԣ�����Armor������Ч��")]
        public EquipmentSlot equipSlot = EquipmentSlot.None; // װ����λ
        public float performanceBoost;     // ��������ֵ

        [Header("����ռ��")]
        public Vector2Int gridSize = new Vector2Int(1, 1); // ��Ʒ�ڱ�����ռ�õĸ��Ӵ�С
        public bool allowRotation = true;  // �Ƿ�������ת

        [Header("��������")]
        public bool canBeDropped = true;   // �Ƿ���Զ���
        public bool useCustomDropScale = false; // �Ƿ�ʹ���Զ�������
        public Vector3 dropScale = Vector3.one; // ����ʱ������

        [Header("������չ���ԣ�����Backpack������Ч��")]
        public int addWidth = 2;           // ���ӵĿ��
        public int addHeight = 2;          // ���ӵĸ߶�

        [Header("����Ʒ���ԣ�����Consumable������Ч��")]
        public int healAmount;             // ������
        public bool canBeUsedInCombat = true; // �Ƿ������ս����ʹ��

        [Header("�������ԣ�����Tool������Ч��")]
        public int durability;             // �;ö�
        public int maxDurability;          // ����;ö�

        /// <summary>
        /// �����Ʒ�ߴ��Ƿ���Ч
        /// </summary>
        public bool IsValidSize()
        {
            return gridSize.x > 0 && gridSize.y > 0;
        }

        /// <summary>
        /// ��ȡϡ�жȶ�Ӧ����ɫ
        /// </summary>
        public Color GetRarityColor()
        {
            switch (rarity)
            {
                case Rarity.Common: return new Color(0.8f, 0.8f, 0.8f, 0.5f); // ��ɫ
                case Rarity.Uncommon: return new Color(0, 1f, 0, 0.5f); // ��ɫ
                case Rarity.Rare: return new Color(0, 0.5f, 1f, 0.5f); // ��ɫ
                case Rarity.Epic: return new Color(0.6f, 0, 1f, 0.5f); // ��ɫ
                case Rarity.Legendary: return new Color(1f, 0.5f, 0, 0.5f); // ��ɫ
                default: return new Color(0.8f, 0.8f, 0.8f, 0.5f); // Ĭ�ϻ�ɫ
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
        /// ʹ��װ��������װ����
        /// </summary>
        protected virtual void UseArmor(Inventory inventory)
        {
            if (equipSlot != EquipmentSlot.None)
            {
                inventory.EquipItem(equipSlot, this);
                Debug.Log($"������{this.itemName}��������{performanceBoost}������");
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
            Debug.Log($"ʹ���˹��ߣ�{itemName}��ʣ���;öȣ�{durability}/{maxDurability}");
            durability--;

            // ����;ö����꣬����������߼�
            if (durability <= 0)
            {
                Debug.Log($"{itemName}�Ѿ��𻵣�");
            }
        }

        /// <summary>
        /// ʹ�ñ�������չ������
        /// </summary>
        protected virtual void UseBackpack(Inventory inventory)
        {
            if (inventory != null)
            {
                inventory.IncreaseCapacity(addWidth, addHeight);
                Debug.Log($"ʹ����{itemName}����������������{addWidth}x{addHeight}��");
            }
        }
    }
}
