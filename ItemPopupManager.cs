using UnityEngine;
using System.Collections; // ������������ռ�����
using System.Collections.Generic;

namespace InventorySystem
{
    public class ItemPopupManager : MonoBehaviour
    {
        public static ItemPopupManager Instance;  // ����ʵ��

        [Header("����Ԥ����")]
        public GameObject popupPrefab;  // ��Inspector��������ĵ���Ԥ����
        public Transform canvasTransform;  // ����UI��Canvas Transform

        private Queue<Item> itemQueue = new Queue<Item>();
        private bool isShowingPopup = false;

        private void Awake()
        {
            // ȷ��ȫ��ֻ��һ��������
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        // �ⲿ���ã������Ʒ����������
        public void ShowItemPopup(Item item)
        {
            if (item != null)
            {
                itemQueue.Enqueue(item);
                if (!isShowingPopup)
                    StartCoroutine(ShowNextPopup());
            }
        }

        // ��ʾ�����е���һ������
        private IEnumerator ShowNextPopup()
        {
            if (itemQueue.Count == 0)
            {
                isShowingPopup = false;
                yield break;
            }

            isShowingPopup = true;
            Item currentItem = itemQueue.Dequeue();

            // ��������ʵ��
            GameObject popup = Instantiate(popupPrefab, canvasTransform);
            ItemPickupPopup popupScript = popup.GetComponent<ItemPickupPopup>();

            // ���õ�������
            if (popupScript != null)
                popupScript.Setup(currentItem);

            // �ȴ�������ʾ��ϣ���ʾʱ�� + ����ʱ�䣩
            yield return new WaitForSeconds(popupScript.displayTime + 2f / popupScript.slideSpeed);

            // ��ʾ��һ������
            StartCoroutine(ShowNextPopup());
        }
    }
}
