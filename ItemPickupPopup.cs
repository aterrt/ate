using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace InventorySystem
{
    public class ItemPickupPopup : MonoBehaviour
    {
        [Header("UI�������")]
        public Image itemIcon;          // ��Ʒͼ�꣨��Inspector������Image�����
        public TextMeshProUGUI itemNameText;  // ��Ʒ�����ı�������TextMeshPro�����
        public TextMeshProUGUI itemValueText; // ��Ʒ��ֵ�ı�������TextMeshPro�����

        [Header("��������")]
        public float displayTime = 2f;  // ������ʾʱ�䣨�룩
        public float slideSpeed = 1f;   // ���뻬���ٶ�

        private RectTransform rectTransform;
        private Vector2 offScreenPosition;  // ��Ļ��λ�ã��Ҳࣩ
        private Vector2 onScreenPosition;   // ��Ļ����ʾλ��

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();

            // ��ʼ��λ�ã����ݵ�����ȼ�����Ļ��λ�ã�
            offScreenPosition = new Vector2(Screen.width + rectTransform.rect.width, 0);
            onScreenPosition = new Vector2(-200, 0);  // ��Ļ�Ҳ�����200����
        }

        // ��ʼ����������
        public void Setup(Item item)
        {
            // ������Ʒͼ��
            if (item.icon != null)
                itemIcon.sprite = item.icon;

            // ������Ʒ���ƣ���ϡ�ж���ɫ��
            itemNameText.text = item.itemName;
            itemNameText.color = item.GetRarityColor();

            // ������Ʒ��ֵ
            itemValueText.text = $"��ֵ: {item.itemValue}";

            // ��ʼ��������
            StartCoroutine(AnimatePopup());
        }

        // �������������� -> ͣ�� -> ������
        private IEnumerator AnimatePopup()
        {
            // ��ʼλ������Ļ��
            rectTransform.anchoredPosition = offScreenPosition;

            // ���붯��
            float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime * slideSpeed;
                rectTransform.anchoredPosition = Vector2.Lerp(offScreenPosition, onScreenPosition, t);
                yield return null;
            }

            // ͣ��ָ��ʱ��
            yield return new WaitForSeconds(displayTime);

            // ��������
            t = 0;
            while (t < 1)
            {
                t += Time.deltaTime * slideSpeed;
                rectTransform.anchoredPosition = Vector2.Lerp(onScreenPosition, offScreenPosition, t);
                yield return null;
            }

            // �������������ٵ���
            Destroy(gameObject);
        }
    }
}
