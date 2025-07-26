using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "ItemManager", menuName = "Inventory/Item Manager")]
    public class ItemManager : ScriptableObject
    {
        public List<Item> allItems = new List<Item>(); // ������Ʒ�б�
        private Dictionary<int, Item> itemDictionary = new Dictionary<int, Item>(); // ��ƷID����Ʒ��ӳ��

        private void OnEnable()
        {
            // ��ʼ����Ʒ�ֵ�
            itemDictionary.Clear();

            foreach (var item in allItems)
            {
                if (item != null)
                {
                    // ���ID�Ƿ���Ч
                    if (item.itemID == -1)
                    {
                        Debug.LogError($"��Ʒ {item.name} û��������Ч��itemID��", item);
                        continue;
                    }

                    // ���ID�Ƿ��ظ�
                    if (itemDictionary.ContainsKey(item.itemID))
                    {
                        Debug.LogError($"��ƷID�ظ�: {item.itemID} ���� {item.name} �� {itemDictionary[item.itemID].name}", item);
                    }
                    else
                    {
                        itemDictionary[item.itemID] = item;
                    }
                }
            }
        }

        /// <summary>
        /// ����ID��ȡ��Ʒ
        /// </summary>
        public Item GetItemByID(int itemID)
        {
            if (itemDictionary.TryGetValue(itemID, out Item item))
            {
                return item;
            }

            Debug.LogWarning($"�Ҳ���IDΪ {itemID} ����Ʒ");
            return null;
        }

        /// <summary>
        /// �������ƻ�ȡ��Ʒ�����������������ص�һ��ƥ���
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

            Debug.LogWarning($"�Ҳ�������Ϊ {itemName} ����Ʒ");
            return null;
        }
    }
}
