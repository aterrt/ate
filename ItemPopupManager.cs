using UnityEngine;
using System.Collections; // 新增这个命名空间引用
using System.Collections.Generic;

namespace InventorySystem
{
    public class ItemPopupManager : MonoBehaviour
    {
        public static ItemPopupManager Instance;  // 单例实例

        [Header("弹窗预制体")]
        public GameObject popupPrefab;  // 在Inspector中拖入你的弹窗预制体
        public Transform canvasTransform;  // 拖入UI的Canvas Transform

        private Queue<Item> itemQueue = new Queue<Item>();
        private bool isShowingPopup = false;

        private void Awake()
        {
            // 确保全局只有一个管理器
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        // 外部调用：添加物品到弹窗队列
        public void ShowItemPopup(Item item)
        {
            if (item != null)
            {
                itemQueue.Enqueue(item);
                if (!isShowingPopup)
                    StartCoroutine(ShowNextPopup());
            }
        }

        // 显示队列中的下一个弹窗
        private IEnumerator ShowNextPopup()
        {
            if (itemQueue.Count == 0)
            {
                isShowingPopup = false;
                yield break;
            }

            isShowingPopup = true;
            Item currentItem = itemQueue.Dequeue();

            // 创建弹窗实例
            GameObject popup = Instantiate(popupPrefab, canvasTransform);
            ItemPickupPopup popupScript = popup.GetComponent<ItemPickupPopup>();

            // 设置弹窗内容
            if (popupScript != null)
                popupScript.Setup(currentItem);

            // 等待弹窗显示完毕（显示时间 + 动画时间）
            yield return new WaitForSeconds(popupScript.displayTime + 2f / popupScript.slideSpeed);

            // 显示下一个弹窗
            StartCoroutine(ShowNextPopup());
        }
    }
}
