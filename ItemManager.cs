using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "ItemManager", menuName = "Inventory/Item Manager")]
    public class ItemManager : ScriptableObject
    {
        public List<Item> allItems = new List<Item>(); // 所有物品列表
        private Dictionary<int, Item> itemDictionary = new Dictionary<int, Item>(); // 物品ID到物品的映射

        private void OnEnable()
        {
            // 初始化物品字典
            itemDictionary.Clear();

            foreach (var item in allItems)
            {
                if (item != null)
                {
                    // 检查ID是否有效
                    if (item.itemID == -1)
                    {
                        Debug.LogError($"物品 {item.name} 没有设置有效的itemID！", item);
                        continue;
                    }

                    // 检查ID是否重复
                    if (itemDictionary.ContainsKey(item.itemID))
                    {
                        Debug.LogError($"物品ID重复: {item.itemID} 用于 {item.name} 和 {itemDictionary[item.itemID].name}", item);
                    }
                    else
                    {
                        itemDictionary[item.itemID] = item;
                    }
                }
            }
        }

        /// <summary>
        /// 根据ID获取物品
        /// </summary>
        public Item GetItemByID(int itemID)
        {
            if (itemDictionary.TryGetValue(itemID, out Item item))
            {
                return item;
            }

            Debug.LogWarning($"找不到ID为 {itemID} 的物品");
            return null;
        }

        /// <summary>
        /// 根据名称获取物品（可能有重名，返回第一个匹配项）
        /// </summary>
        public Item GetItemByName(string itemName)
        {
            foreach (var item in allItems)
            {
                if (item != null && item.itemName == itemName)
                {
                    return item;
                }
            }

            Debug.LogWarning($"找不到名称为 {itemName} 的物品");
            return null;
        }
    }
}
