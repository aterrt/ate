using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace InventorySystem
{
    public class ItemPickupPopup : MonoBehaviour
    {
        [Header("UI组件关联")]
        public Image itemIcon;          // 物品图标（在Inspector中拖入Image组件）
        public TextMeshProUGUI itemNameText;  // 物品名称文本（拖入TextMeshPro组件）
        public TextMeshProUGUI itemValueText; // 物品价值文本（拖入TextMeshPro组件）

        [Header("弹窗设置")]
        public float displayTime = 2f;  // 弹窗显示时间（秒）
        public float slideSpeed = 1f;   // 滑入滑出速度

        private RectTransform rectTransform;
        private Vector2 offScreenPosition;  // 屏幕外位置（右侧）
        private Vector2 onScreenPosition;   // 屏幕内显示位置

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();

            // 初始化位置（根据弹窗宽度计算屏幕外位置）
            offScreenPosition = new Vector2(Screen.width + rectTransform.rect.width, 0);
            onScreenPosition = new Vector2(-200, 0);  // 屏幕右侧向内200像素
        }

        // 初始化弹窗内容
        public void Setup(Item item)
        {
            // 设置物品图标
            if (item.icon != null)
                itemIcon.sprite = item.icon;

            // 设置物品名称（带稀有度颜色）
            itemNameText.text = item.itemName;
            itemNameText.color = item.GetRarityColor();

            // 设置物品价值
            itemValueText.text = $"价值: {item.itemValue}";

            // 开始弹窗动画
            StartCoroutine(AnimatePopup());
        }

        // 弹窗动画（滑入 -> 停留 -> 滑出）
        private IEnumerator AnimatePopup()
        {
            // 初始位置在屏幕外
            rectTransform.anchoredPosition = offScreenPosition;

            // 滑入动画
            float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime * slideSpeed;
                rectTransform.anchoredPosition = Vector2.Lerp(offScreenPosition, onScreenPosition, t);
                yield return null;
            }

            // 停留指定时间
            yield return new WaitForSeconds(displayTime);

            // 滑出动画
            t = 0;
            while (t < 1)
            {
                t += Time.deltaTime * slideSpeed;
                rectTransform.anchoredPosition = Vector2.Lerp(onScreenPosition, offScreenPosition, t);
                yield return null;
            }

            // 动画结束后销毁弹窗
            Destroy(gameObject);
        }
    }
}
