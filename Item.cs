using UnityEngine;

namespace InventorySystem
{
    public enum ItemType
    {
        Consumable,   // 消耗品
        Tool,         // 工具
        Weapon,       // 武器
        Armor,        // 装备
        Material,     // 材料
        Backpack,     // 背包(扩展容量用)
        Other         // 其他
    }

    public enum Rarity
    {
        Common,       // 普通（白色）
        Uncommon,     //  uncommon（绿色）
        Rare,         // 稀有（蓝色）
        Epic,         // 史诗（紫色）
        Legendary,    // 传说（金色）
        Red           // 红色
    }

    // 装备位置枚举
    public enum EquipmentSlot
    {
        None,         // 未装备
        Head,         // 头部
        Chest,        // 胸部
        Belt,         // 腰带
        Foot          // 脚部
    }

    [CreateAssetMenu(fileName = "New Item", menuName = "Inventory System/Item")]
    public class Item : ScriptableObject
    {
        [Header("基本信息")]
        public int itemID;                 // 物品ID
        public string itemName;            // 物品名称
        [TextArea] public string description; // 物品描述
        public Sprite icon;                // 物品图标
        public GameObject itemPrefab;      // 物品预制体
        public ItemType itemType;          // 物品类型
        public Rarity rarity;              // 稀有度

        [Header("装备属性(仅Armor生效)")]
        public EquipmentSlot equipSlot = EquipmentSlot.None; // 装备位置
        public float performanceBoost;     // 性能提升值

        [Header("占用空间")]
        public Vector2Int gridSize = new Vector2Int(1, 1); // 物品在背包中占用的网格大小
        public bool allowRotation = true;  // 是否允许旋转

        [Header("丢弃设置")]
        public bool canBeDropped = true;   // 是否可以丢弃
        public bool useCustomDropScale = false; // 是否使用自定义丢弃比例
        public Vector3 dropScale = Vector3.one; // 丢弃时的缩放

        [Header("背包扩展属性(仅Backpack生效)")]
        public int addWidth = 2;           // 增加的宽度
        public int addHeight = 2;          // 增加的高度

        [Header("消耗品属性(仅Consumable生效)")]
        public int healAmount;             // 治疗量
        public bool canBeUsedInCombat = true; // 是否可以在战斗中使用

        [Header("工具属性(仅Tool生效)")]
        public int durability;             // 当前耐久
        public int maxDurability;          // 最大耐久

        [Header("经济属性")]
        [HideInInspector] public int itemValue; // 物品价值（根据品质自动生成）

        /// <summary>
        /// 初始化时自动生成物品价值
        /// </summary>
        private void OnEnable()
        {
            GenerateFixedValueByRarity();
        }

        /// <summary>
        /// 根据稀有度生成固定随机价值
        /// </summary>
        private void GenerateFixedValueByRarity()
        {
            // 使用itemID作为随机种子，确保同一ID物品价值固定
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
                case Rarity.Red:
                    itemValue = random.Next(39, 61); // 39-60
                    break;
                default:
                    itemValue = 0;
                    break;
            }
        }

        /// <summary>
        /// 检查物品尺寸是否有效
        /// </summary>
        public bool IsValidSize()
        {
            return gridSize.x > 0 && gridSize.y > 0;
        }

        /// <summary>
        /// 获取稀有度对应的颜色
        /// </summary>
        public Color GetRarityColor()
        {
            switch (rarity)
            {
                case Rarity.Common: return new Color(0.8f, 0.8f, 0.8f, 0.5f); // 白色
                case Rarity.Uncommon: return new Color(0, 1f, 0, 0.5f); // 绿色
                case Rarity.Rare: return new Color(0, 0.5f, 1f, 0.5f); // 蓝色
                case Rarity.Epic: return new Color(0.6f, 0, 1f, 0.5f); // 紫色
                case Rarity.Legendary: return new Color(1f, 0.5f, 0, 0.5f); // 金色
                case Rarity.Red: return new Color(1f, 0, 0, 0.5f); // 红色
                default: return new Color(0.8f, 0.8f, 0.8f, 0.5f); // 默认灰色
            }
        }

        /// <summary>
        /// 使用物品
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
        /// 使用装备(装备到对应槽位)
        /// </summary>
        protected virtual void UseArmor(Inventory inventory)
        {
            if (equipSlot != EquipmentSlot.None)
            {
                inventory.EquipItem(equipSlot, this);
                Debug.Log($"装备了{this.itemName}，提升{performanceBoost}点性能");
            }
        }

        /// <summary>
        /// 使用消耗品
        /// </summary>
        protected virtual void UseConsumable(Inventory inventory)
        {
            if (healAmount > 0)
            {
                inventory.Heal(healAmount);
                Debug.Log($"使用了{itemName}，恢复了{healAmount}点生命值");
            }
            else
            {
                Debug.Log($"使用了{itemName}");
            }
        }

        /// <summary>
        /// 使用工具
        /// </summary>
        protected virtual void UseTool(Inventory inventory)
        {
            Debug.Log($"使用了工具: {itemName}，剩余耐久: {durability}/{maxDurability}");
            durability--;

            // 如果耐久度为0，执行损坏逻辑
            if (durability <= 0)
            {
                Debug.Log($"{itemName}已经损坏!");
            }
        }

        /// <summary>
        /// 使用背包(扩展背包容量)
        /// </summary>
        protected virtual void UseBackpack(Inventory inventory)
        {
            if (inventory != null)
            {
                inventory.IncreaseCapacity(addWidth, addHeight);
                Debug.Log($"使用了{itemName}，背包容量增加了{addWidth}x{addHeight}");
            }
        }
    }
}
