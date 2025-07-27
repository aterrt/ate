using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace InventorySystem
{
    public class Inventory : MonoBehaviour
    {
        [Header("��������")]
        public ItemManager itemManager;                  // ��Ʒ������
        public int gridWidth = 10;                       // ���������ȣ�������
        public int gridHeight = 8;                       // ��������߶ȣ�������
        public KeyCode inventoryKey = KeyCode.Tab;       // ��/�رձ����İ���
        public float slotSize = 60f;                     // �������Ӵ�С�����أ�
        public float slotSpacing = 5f;                   // ���Ӽ�ࣨ���أ�
        public GUIStyle itemNameStyle;                   // ��Ʒ������ʾ��ʽ
        public GUIStyle tooltipStyle;                    // ��Ʒ��ʾ��ʽ

        [Header("װ����λ������")]
        public Rect headSlotRect = new Rect(150, 100, 60, 60);    // ͷ��װ����λ��
        public Rect chestSlotRect = new Rect(150, 180, 60, 80);   // ����װ����λ��
        public Rect beltSlotRect = new Rect(150, 280, 60, 40);    // ����װ����λ��
        public Rect footSlotRect = new Rect(150, 340, 60, 60);    // Ь��װ����λ��

        [Header("����ֵ����")]
        public float maxHealth = 100f;
        public float currentHealth;

        private List<InventorySlot> slots = new List<InventorySlot>();
        public bool isInventoryOpen = false;
        private InventorySlot draggedSlot = new InventorySlot();
        private int draggedItemUniqueID = -1;
        private Rect dragRect;
        private Transform playerTransform;
        private Rect inventoryWindowRect;
        private Rect playerAreaRect;
        private Rect backpackAreaRect;
        private Rect dropZoneRect;
        private int nextUniqueID = 1;
        private Texture2D whiteTexture;

        // װ��ϵͳ���
        private Dictionary<EquipmentSlot, Item> equippedItems = new Dictionary<EquipmentSlot, Item>();
        private Dictionary<EquipmentSlot, int> equippedItemUniqueIDs = new Dictionary<EquipmentSlot, int>();
        public UnityEvent<EquipmentSlot, Item> onEquip;  // װ���¼�
        public UnityEvent<EquipmentSlot> onUnequip;      // ж���¼�

        // ����������չ���
        private List<Item> usedBackpacks = new List<Item>(); // ��ʹ�õı����б�

        // ��ת�������
        private int draggedItemRotation = 0;

        // ��ק״̬����
        private Vector2 dragStartPosition;
        private bool isPotentialDrag = false;
        private bool isDragging = false;
        private bool isCtrlDrop = false;

        // ���񲼾ּ��㻺��
        private float gridTotalWidth;
        private float gridTotalHeight;
        private float gridStartX;
        private float gridStartY;

        // Ԥ������ر���
        private Rect previewRect;
        private Color previewColor;
        private bool showPreview = false;

        // ����ֵ�仯�¼�
        public delegate void HealthChanged(float current, float max);
        public event HealthChanged OnHealthChanged;

        private void Awake()
        {
            currentHealth = maxHealth;
            InitializeEquipmentSlots();
            ClearInventory();
            playerTransform = transform;
            InitializeWhiteTexture();
        }

        private void InitializeEquipmentSlots()
        {
            equippedItems = new Dictionary<EquipmentSlot, Item>
            {
                { EquipmentSlot.Head, null },
                { EquipmentSlot.Chest, null },
                { EquipmentSlot.Belt, null },
                { EquipmentSlot.Foot, null }
            };

            equippedItemUniqueIDs = new Dictionary<EquipmentSlot, int>
            {
                { EquipmentSlot.Head, -1 },
                { EquipmentSlot.Chest, -1 },
                { EquipmentSlot.Belt, -1 },
                { EquipmentSlot.Foot, -1 }
            };
        }

        private void Start()
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            InitializeFullscreenLayout();
        }

        private void Update()
        {
            // ������ת����
            if (isDragging && draggedItemUniqueID != -1 && draggedSlot.itemID != 0)
            {
                Item draggedItem = itemManager.GetItemByID(draggedSlot.itemID);
                if (draggedItem != null && draggedItem.allowRotation && Input.GetKeyDown(KeyCode.R))
                {
                    draggedItemRotation = (draggedItemRotation + 1) % 2;
                }
            }

            // ��Ļ�ߴ�仯ʱ���¼��㲼��
            if (inventoryWindowRect.width != Screen.width || inventoryWindowRect.height != Screen.height)
            {
                InitializeFullscreenLayout();
            }

            // ��������/�ر�
            if (Input.GetKeyDown(inventoryKey))
            {
                isInventoryOpen = !isInventoryOpen;
                Cursor.visible = isInventoryOpen;
                Cursor.lockState = isInventoryOpen ? CursorLockMode.None : CursorLockMode.Locked;
            }

            UpdateDragState();
        }

        private void UpdateDragState()
        {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetMouseButton(0))
            {
                if (isDragging)
                {
                    ResetDragState();
                }
                return;
            }

            if (!isDragging && Input.GetMouseButtonDown(0))
            {
                isPotentialDrag = true;
                dragStartPosition = Input.mousePosition;
                isCtrlDrop = false;
            }
            else if (isPotentialDrag && Input.GetMouseButtonUp(0))
            {
                float distance = Vector2.Distance(Input.mousePosition, dragStartPosition);

                if (distance < 8f) // ��ק��ֵ
                {
                    ResetDragState();
                }

                isPotentialDrag = false;
            }
            else if (isPotentialDrag && !isDragging)
            {
                float distance = Vector2.Distance(Input.mousePosition, dragStartPosition);

                if (distance >= 8f) // ��ק��ֵ
                {
                    isDragging = true;
                }
            }
            else if (isDragging && Input.GetMouseButtonUp(0))
            {
                ResetDragState();
            }
        }

        private void ResetDragState()
        {
            isDragging = false;
            isPotentialDrag = false;
            isCtrlDrop = false;
            showPreview = false; // ����Ԥ��״̬
        }

        public void ClearInventory()
        {
            slots.Clear();
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    slots.Add(new InventorySlot(0, x, y));
                }
            }
            nextUniqueID = 1;
        }

        public bool AddItemByID(int itemID, int count = 1)
        {
            Item item = itemManager.GetItemByID(itemID);
            if (item == null) return false;

            bool result = true;
            for (int i = 0; i < count; i++)
            {
                if (!AddItem(item))
                {
                    result = false;
                    break;
                }
            }
            return result;
        }

        public bool TryEquipOrAddItem(Item item)
        {
            // �������⴦�� - ֱ��ʹ����������
            if (item.itemType == ItemType.Backpack)
            {
                return UseBackpack(item);
            }

            // װ�����ʹ���
            if (item.itemType == ItemType.Armor && item.equipSlot != EquipmentSlot.None)
            {
                if (equippedItems[item.equipSlot] == null)
                {
                    EquipItem(item.equipSlot, item);
                    return true;
                }
            }

            // ��ͨ��Ʒ��ӵ�����
            return AddItem(item);
        }

        // ����ʹ���߼� - ר�Ŵ���������
        private bool UseBackpack(Item backpack)
        {
            if (backpack == null || backpack.itemType != ItemType.Backpack) return false;

            // ����Ƿ���ʹ�ù��ñ���
            if (usedBackpacks.Contains(backpack))
            {
                Debug.LogWarning("�ñ�����ʹ�ù�");
                return false;
            }

            // ��������
            IncreaseCapacity(backpack.addWidth, backpack.addHeight);
            usedBackpacks.Add(backpack);

            // ��ӵ�������Ϊ��ʹ����Ʒ
            return AddItem(backpack);
        }

        private bool AddItem(Item item)
        {
            if (item == null || !item.IsValidSize()) return false;

            for (int rot = 0; rot < (item.allowRotation ? 2 : 1); rot++)
            {
                int requiredWidth = rot == 0 ? item.gridSize.x : item.gridSize.y;
                int requiredHeight = rot == 0 ? item.gridSize.y : item.gridSize.x;

                for (int y = 0; y <= gridHeight - requiredHeight; y++)
                {
                    for (int x = 0; x <= gridWidth - requiredWidth; x++)
                    {
                        bool spaceAvailable = true;
                        for (int dy = 0; dy < requiredHeight; dy++)
                        {
                            for (int dx = 0; dx < requiredWidth; dx++)
                            {
                                int index = (y + dy) * gridWidth + (x + dx);
                                if (index >= slots.Count || !slots[index].IsEmpty())
                                {
                                    spaceAvailable = false;
                                    break;
                                }
                            }
                            if (!spaceAvailable) break;
                        }

                        if (spaceAvailable)
                        {
                            PlaceItemAtPosition(item, x, y, nextUniqueID, rot);
                            nextUniqueID++;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool IsSpaceAvailable(int x, int y, int width, int height, int excludeParentID = -1)
        {
            if (x < 0 || y < 0 || x + width > gridWidth || y + height > gridHeight)
            {
                return false;
            }

            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    int index = (y + dy) * gridWidth + (x + dx);
                    if (index >= slots.Count)
                    {
                        return false;
                    }
                    if (slots[index].parentID != excludeParentID && !slots[index].IsEmpty())
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private void PlaceItemAtPosition(Item item, int x, int y, int uniqueID, int rotation)
        {
            int width = rotation == 0 ? item.gridSize.x : item.gridSize.y;
            int height = rotation == 0 ? item.gridSize.y : item.gridSize.x;

            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    int index = (y + dy) * gridWidth + (x + dx);
                    if (index < slots.Count)
                    {
                        int itemID = (dy == 0 && dx == 0) ? item.itemID : 0;
                        slots[index] = new InventorySlot(itemID, x, y, uniqueID, rotation);
                    }
                }
            }
        }

        public bool RemoveItemByUniqueID(int uniqueID)
        {
            List<int> indicesToClear = new List<int>();

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].parentID == uniqueID)
                {
                    indicesToClear.Add(i);
                }
            }

            if (indicesToClear.Count == 0) return false;

            // ����Ƿ��Ǳ�����Ʒ
            Item removedItem = GetItemByUniqueID(uniqueID);
            if (removedItem != null && removedItem.itemType == ItemType.Backpack)
            {
                if (usedBackpacks.Contains(removedItem))
                {
                    // �Ƴ�����ʱ��������
                    DecreaseCapacity(removedItem.addWidth, removedItem.addHeight);
                    usedBackpacks.Remove(removedItem);
                }
            }

            foreach (int index in indicesToClear)
            {
                slots[index] = new InventorySlot(0, slots[index].x, slots[index].y);
            }

            return true;
        }

        private int GetItemUniqueIDAtPosition(int x, int y)
        {
            int index = y * gridWidth + x;
            if (index < 0 || index >= slots.Count) return -1;
            return slots[index].parentID;
        }

        private Vector2Int GetItemFirstSlotPosition(int uniqueID)
        {
            foreach (var slot in slots)
            {
                if (slot.parentID == uniqueID && slot.itemID != 0)
                {
                    return new Vector2Int(slot.x, slot.y);
                }
            }
            return new Vector2Int(-1, -1);
        }

        private int GetItemRotation(int uniqueID)
        {
            foreach (var slot in slots)
            {
                if (slot.parentID == uniqueID && slot.itemID != 0)
                {
                    return slot.rotation;
                }
            }
            return 0;
        }

        private Item GetItemByUniqueID(int uniqueID)
        {
            foreach (var slot in slots)
            {
                if (slot.parentID == uniqueID && slot.itemID != 0)
                {
                    return itemManager.GetItemByID(slot.itemID);
                }
            }
            return null;
        }

        // װ����Ʒ
        public void EquipItem(EquipmentSlot slot, Item item)
        {
            if (item == null) return;

            // ����ò�λ����װ������ж�²���ӵ�����
            if (equippedItems[slot] != null)
            {
                Item oldItem = equippedItems[slot];
                int oldUniqueID = equippedItemUniqueIDs[slot];

                // ��װ�����Ƴ�
                equippedItems[slot] = null;
                equippedItemUniqueIDs[slot] = -1;

                // ��ӵ�����
                AddItem(oldItem);
            }

            // �����װ������λ
            equippedItems[slot] = item;
            equippedItemUniqueIDs[slot] = nextUniqueID++;

            // ����װ���¼�
            onEquip?.Invoke(slot, item);
        }

        // ж��װ��
        public void UnequipItem(EquipmentSlot slot)
        {
            if (equippedItems[slot] != null)
            {
                // ��װ����ӵ�����
                AddItem(equippedItems[slot]);

                // ��װ�����Ƴ�
                equippedItems[slot] = null;
                equippedItemUniqueIDs[slot] = -1;

                // ����ж���¼�
                onUnequip?.Invoke(slot);
            }
        }

        /// <summary>
        /// ���ӱ�������
        /// </summary>
        public void IncreaseCapacity(int addWidth, int addHeight)
        {
            // ��¼��ǰ����
            int oldWidth = gridWidth;
            int oldHeight = gridHeight;

            // ��������ֵ
            gridWidth += addWidth;
            gridHeight += addHeight;

            // �����µĴ洢�ṹ������������Ʒ
            List<InventorySlot> newSlots = new List<InventorySlot>();

            // ������¸���
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    newSlots.Add(new InventorySlot(0, x, y));
                }
            }

            // ���ƾɸ����е���Ʒ
            for (int y = 0; y < oldHeight; y++)
            {
                for (int x = 0; x < oldWidth; x++)
                {
                    int oldIndex = y * oldWidth + x;
                    int newIndex = y * gridWidth + x;

                    if (oldIndex < slots.Count && newIndex < newSlots.Count)
                    {
                        newSlots[newIndex] = slots[oldIndex];
                    }
                }
            }

            // �滻Ϊ�µĴ洢�ṹ
            slots = newSlots;

            // ���³�ʼ������
            InitializeFullscreenLayout();

            Debug.Log($"�������������ӵ�: {gridWidth}x{gridHeight}");
        }

        /// <summary>
        /// ���ٱ�������
        /// </summary>
        public void DecreaseCapacity(int removeWidth, int removeHeight)
        {
            // ȷ����������С����Сֵ
            int minWidth = 5;
            int minHeight = 4;

            gridWidth = Mathf.Max(minWidth, gridWidth - removeWidth);
            gridHeight = Mathf.Max(minHeight, gridHeight - removeHeight);

            // �����µĴ洢�ṹ
            List<InventorySlot> newSlots = new List<InventorySlot>();
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    newSlots.Add(new InventorySlot(0, x, y));
                }
            }

            // ���ƿ��Ա�������Ʒ
            foreach (var slot in slots)
            {
                if (slot.itemID != 0 && slot.x < gridWidth && slot.y < gridHeight)
                {
                    Item item = itemManager.GetItemByID(slot.itemID);
                    if (item != null)
                    {
                        int width = slot.rotation == 0 ? item.gridSize.x : item.gridSize.y;
                        int height = slot.rotation == 0 ? item.gridSize.y : item.gridSize.x;

                        // �����Ʒ�Ƿ������������з���
                        if (slot.x + width <= gridWidth && slot.y + height <= gridHeight)
                        {
                            PlaceItemAtPosition(item, slot.x, slot.y, slot.parentID, slot.rotation);
                        }
                        else
                        {
                            // �޷����µ���Ʒ��������
                            DropItem(item);
                        }
                    }
                }
            }

            // �滻Ϊ�µĴ洢�ṹ
            slots = newSlots;

            // ���³�ʼ������
            InitializeFullscreenLayout();

            Debug.Log($"���������Ѽ��ٵ�: {gridWidth}x{gridHeight}");
        }

        private void OnGUI()
        {
            if (isInventoryOpen && itemManager != null)
            {
                GUI.depth = -10;
                GUI.Window(0, inventoryWindowRect, DrawFullscreenInventory, "");
            }

            if (draggedSlot.itemID != 0 && draggedItemUniqueID != -1 && isDragging)
            {
                GUI.depth = -20;
                DrawDraggedItem();
                DrawPlacementPreview(); // ���Ʒ���Ԥ����
            }
        }

        private void DrawFullscreenInventory(int windowID)
        {
            DrawPlayerArea();
            DrawBackpackArea();
            DrawDropZone();
            HandleDropZoneInput();
        }

        private void HandleDropZoneInput()
        {
            if (isInventoryOpen && draggedItemUniqueID != -1 && Event.current.type == EventType.MouseUp && Event.current.button == 0)
            {
                Vector2 mousePos = Event.current.mousePosition;
                mousePos.y = Screen.height - mousePos.y;

                if (dropZoneRect.Contains(mousePos))
                {
                    Item draggedItem = itemManager.GetItemByID(draggedSlot.itemID);
                    if (draggedItem != null && draggedItem.canBeDropped)
                    {
                        DropItem(draggedItem);
                        RemoveItemByUniqueID(draggedItemUniqueID);

                        draggedSlot = new InventorySlot();
                        draggedItemUniqueID = -1;
                        draggedItemRotation = 0;
                        ResetDragState();
                        Event.current.Use();
                    }
                }
            }
        }

        private void DrawPlayerArea()
        {
            GUI.Box(playerAreaRect, "", GUI.skin.window);
            DrawEquipmentSlots();

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 24;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(playerAreaRect.x, 10, playerAreaRect.width, 30), "��ɫ��Ϣ", titleStyle);
        }

        // ����װ����
        private void DrawEquipmentSlots()
        {
            // ����װ��������������ڵ�λ��
            float slotX = playerAreaRect.x + (playerAreaRect.width - 60) / 2;

            // ͷ��װ����
            headSlotRect.x = slotX;
            GUI.Box(headSlotRect, "ͷ��", GUI.skin.window);
            if (equippedItems[EquipmentSlot.Head] != null)
            {
                DrawEquippedItem(headSlotRect, equippedItems[EquipmentSlot.Head]);
                HandleEquipmentSlotInput(headSlotRect, EquipmentSlot.Head);
            }

            // ����װ����
            chestSlotRect.x = slotX;
            GUI.Box(chestSlotRect, "����", GUI.skin.window);
            if (equippedItems[EquipmentSlot.Chest] != null)
            {
                DrawEquippedItem(chestSlotRect, equippedItems[EquipmentSlot.Chest]);
                HandleEquipmentSlotInput(chestSlotRect, EquipmentSlot.Chest);
            }

            // ����װ����
            beltSlotRect.x = slotX;
            GUI.Box(beltSlotRect, "����", GUI.skin.window);
            if (equippedItems[EquipmentSlot.Belt] != null)
            {
                DrawEquippedItem(beltSlotRect, equippedItems[EquipmentSlot.Belt]);
                HandleEquipmentSlotInput(beltSlotRect, EquipmentSlot.Belt);
            }

            // Ь��װ����
            footSlotRect.x = slotX;
            GUI.Box(footSlotRect, "Ь��", GUI.skin.window);
            if (equippedItems[EquipmentSlot.Foot] != null)
            {
                DrawEquippedItem(footSlotRect, equippedItems[EquipmentSlot.Foot]);
                HandleEquipmentSlotInput(footSlotRect, EquipmentSlot.Foot);
            }
        }

        // ������װ������Ʒ
        private void DrawEquippedItem(Rect slotRect, Item item)
        {
            if (item.icon != null)
            {
                Rect iconRect = new Rect(
                    slotRect.x + 2,
                    slotRect.y + 2,
                    slotRect.width - 4,
                    slotRect.height - 4
                );
                GUI.DrawTexture(iconRect, item.icon.texture);
            }
            else
            {
                GUI.Label(slotRect, item.itemName, itemNameStyle);
            }
        }

        // ����װ��������
        private void HandleEquipmentSlotInput(Rect slotRect, EquipmentSlot slot)
        {
            Event currentEvent = Event.current;
            if (slotRect.Contains(currentEvent.mousePosition))
            {
                // ������ж��װ��
                if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
                {
                    UnequipItem(slot);
                    currentEvent.Use();
                }
            }
        }

        private void DrawBackpackArea()
        {
            Event currentEvent = Event.current;

            GUI.Box(backpackAreaRect, "", GUI.skin.window);

            gridTotalWidth = gridWidth * (slotSize + slotSpacing) + slotSpacing;
            gridTotalHeight = gridHeight * (slotSize + slotSpacing) + slotSpacing;
            gridStartX = backpackAreaRect.x + (backpackAreaRect.width - gridTotalWidth - dropZoneRect.width) / 2;
            gridStartY = backpackAreaRect.y + 50;

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 24;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            float titleX = gridStartX + 20;
            float titleY = gridStartY - 40;
            GUI.Label(new Rect(titleX, titleY, 200, 30), "������Ʒ", titleStyle);

            // ���Ƹ��ӱ���
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    Rect slotRect = new Rect(
                        gridStartX + x * (slotSize + slotSpacing) + slotSpacing,
                        gridStartY + y * (slotSize + slotSpacing) + slotSpacing,
                        slotSize,
                        slotSize
                    );
                    GUI.Box(slotRect, "", GUI.skin.window);
                }
            }

            // ������Ʒ
            HashSet<int> drawnItems = new HashSet<int>();
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    int index = y * gridWidth + x;
                    InventorySlot slot = slots[index];

                    if (slot.itemID != 0 && !drawnItems.Contains(slot.parentID))
                    {
                        Item item = itemManager.GetItemByID(slot.itemID);
                        if (item != null)
                        {
                            // ����Ƿ���������ק����Ʒ
                            bool isBeingDragged = isDragging && draggedItemUniqueID == slot.parentID;

                            // ��קʱ������ԭʼλ�õ���Ʒ
                            if (isBeingDragged)
                            {
                                drawnItems.Add(slot.parentID);
                                continue;
                            }

                            int rotation = slot.rotation;
                            int displayWidth = rotation == 0 ? item.gridSize.x : item.gridSize.y;
                            int displayHeight = rotation == 0 ? item.gridSize.y : item.gridSize.x;

                            Rect firstSlotRect = new Rect(
                                gridStartX + x * (slotSize + slotSpacing) + slotSpacing,
                                gridStartY + y * (slotSize + slotSpacing) + slotSpacing,
                                slotSize,
                                slotSize
                            );

                            Rect itemArea = new Rect(
                                firstSlotRect.x,
                                firstSlotRect.y,
                                slotSize * displayWidth + slotSpacing * (displayWidth - 1),
                                slotSize * displayHeight + slotSpacing * (displayHeight - 1)
                            );

                            // ������Ʒ����
                            DrawItemBackground(item, firstSlotRect, displayWidth, displayHeight);
                            // ������Ʒͼ��
                            DrawItemIcon(item, firstSlotRect, displayWidth, displayHeight, rotation);
                            // ������Ʒ����
                            DrawItemName(item, firstSlotRect);

                            bool itemHandled = HandleItemAreaInput(itemArea, x, y, currentEvent, item, slot.parentID);
                            if (itemHandled)
                            {
                                currentEvent.Use();
                            }

                            drawnItems.Add(slot.parentID);
                        }
                    }
                }
            }

            // ����ղ�λ����
            HandleEmptySlotsInput(currentEvent, gridStartX, gridStartY);
        }

        private void DrawItemBackground(Item item, Rect firstSlotRect, int displayWidth, int displayHeight)
        {
            Rect backgroundRect = new Rect(
                firstSlotRect.x,
                firstSlotRect.y,
                slotSize * displayWidth + slotSpacing * (displayWidth - 1),
                slotSize * displayHeight + slotSpacing * (displayHeight - 1)
            );

            Color itemColor = item.GetRarityColor();
            GUI.color = itemColor;
            GUI.DrawTexture(backgroundRect, whiteTexture);

            GUI.color = Color.white;
            GUI.Box(backgroundRect, "", GUI.skin.window);
            GUI.color = Color.white;
        }

        private void DrawItemIcon(Item item, Rect firstSlotRect, int displayWidth, int displayHeight, int rotation)
        {
            if (item.icon == null) return;

            Rect iconRect = new Rect(
                firstSlotRect.x,
                firstSlotRect.y,
                slotSize * displayWidth + slotSpacing * (displayWidth - 1),
                slotSize * displayHeight + slotSpacing * (displayHeight - 1)
            );

            Matrix4x4 matrix = GUI.matrix;
            Vector2 pivot = new Vector2(iconRect.x + iconRect.width / 2, iconRect.y + iconRect.height / 2);
            GUIUtility.ScaleAroundPivot(Vector2.one, pivot);

            if (rotation == 1)
            {
                GUIUtility.RotateAroundPivot(90, pivot);
            }

            Rect adjustedRect = iconRect;
            if (rotation == 1)
            {
                adjustedRect.width = iconRect.height;
                adjustedRect.height = iconRect.width;
                adjustedRect.x = pivot.x - adjustedRect.width / 2;
                adjustedRect.y = pivot.y - adjustedRect.height / 2;
            }

            GUI.DrawTexture(adjustedRect, item.icon.texture);
            GUI.matrix = matrix;
        }

        private void DrawItemName(Item item, Rect firstSlotRect)
        {
            GUI.Label(firstSlotRect, item.itemName, itemNameStyle);
        }

        private bool HandleItemAreaInput(Rect itemArea, int x, int y, Event currentEvent, Item item, int uniqueID)
        {
            if (!itemArea.Contains(currentEvent.mousePosition))
                return false;

            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && Input.GetKey(KeyCode.LeftControl))
            {
                ResetDragState();
                isCtrlDrop = true;

                DropItem(item);
                RemoveItemByUniqueID(uniqueID);
                return true;
            }

            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && !Input.GetKey(KeyCode.LeftControl))
            {
                isPotentialDrag = true;
                dragStartPosition = currentEvent.mousePosition;
                return true;
            }
            else if (isPotentialDrag && isDragging && draggedItemUniqueID == -1)
            {
                draggedSlot = new InventorySlot(item.itemID, x, y, uniqueID, GetItemRotation(uniqueID));
                draggedItemUniqueID = uniqueID;
                draggedItemRotation = GetItemRotation(uniqueID);
                return true;
            }
            else if (currentEvent.type == EventType.MouseDown && currentEvent.button == 1)
            {
                // �Ҽ�ʹ����Ʒ
                item.Use(this);
                RemoveItemByUniqueID(uniqueID);
                return true;
            }

            return false;
        }

        private void HandleEmptySlotsInput(Event currentEvent, float gridStartX, float gridStartY)
        {
            if (isCtrlDrop)
            {
                return;
            }

            if (currentEvent.type != EventType.MouseUp || currentEvent.button != 0 || !isDragging)
                return;

            if (draggedSlot.itemID == 0 || draggedItemUniqueID == -1)
                return;

            Vector2 mousePos = currentEvent.mousePosition;
            mousePos.y = Screen.height - mousePos.y;
            if (dropZoneRect.Contains(mousePos))
            {
                return;
            }

            // ����Ƿ��ϵ���װ����
            Item draggedItem = itemManager.GetItemByID(draggedSlot.itemID);
            if (draggedItem != null && draggedItem.itemType == ItemType.Armor && draggedItem.equipSlot != EquipmentSlot.None)
            {
                Rect targetSlotRect = GetEquipmentSlotRect(draggedItem.equipSlot);
                if (targetSlotRect.Contains(mousePos))
                {
                    // �ӱ����Ƴ�
                    RemoveItemByUniqueID(draggedItemUniqueID);
                    // װ������Ӧ��λ
                    EquipItem(draggedItem.equipSlot, draggedItem);

                    draggedSlot = new InventorySlot();
                    draggedItemUniqueID = -1;
                    draggedItemRotation = 0;
                    ResetDragState();
                    currentEvent.Use();
                    return;
                }
            }

            int mouseGridX = Mathf.FloorToInt(
                (currentEvent.mousePosition.x - gridStartX - slotSpacing) /
                (slotSize + slotSpacing)
            );
            int mouseGridY = Mathf.FloorToInt(
                (currentEvent.mousePosition.y - gridStartY - slotSpacing) /
                (slotSize + slotSpacing)
            );

            if (draggedItem == null) return;

            int originalX = draggedSlot.x;
            int originalY = draggedSlot.y;
            int originalRotation = draggedSlot.rotation;

            RemoveItemByUniqueID(draggedItemUniqueID);

            int currentWidth = draggedItemRotation == 0 ? draggedItem.gridSize.x : draggedItem.gridSize.y;
            int currentHeight = draggedItemRotation == 0 ? draggedItem.gridSize.y : draggedItem.gridSize.x;

            bool canPlace = IsSpaceAvailable(mouseGridX, mouseGridY, currentWidth, currentHeight, draggedItemUniqueID);

            if (canPlace)
            {
                PlaceItemAtPosition(draggedItem, mouseGridX, mouseGridY, draggedItemUniqueID, draggedItemRotation);
            }
            else
            {
                PlaceItemAtPosition(draggedItem, originalX, originalY, draggedItemUniqueID, originalRotation);
            }

            draggedSlot = new InventorySlot();
            draggedItemUniqueID = -1;
            draggedItemRotation = 0;
            ResetDragState();
            currentEvent.Use();
        }

        private void DrawDraggedItem()
        {
            Item draggedItem = itemManager.GetItemByID(draggedSlot.itemID);
            if (draggedItem == null) return;

            // ������ת��ĳߴ�
            int currentWidth = draggedItemRotation == 0 ? draggedItem.gridSize.x : draggedItem.gridSize.y;
            int currentHeight = draggedItemRotation == 0 ? draggedItem.gridSize.y : draggedItem.gridSize.x;

            // ������ק���δ�С
            float rectWidth = slotSize * currentWidth + slotSpacing * (currentWidth - 1);
            float rectHeight = slotSize * currentHeight + slotSpacing * (currentHeight - 1);

            // ת��������굽GUIϵͳ
            Vector2 mousePos = Input.mousePosition;
            mousePos.y = Screen.height - mousePos.y;

            // ������뵽�����λ��
            float alignedX = mousePos.x - rectWidth / 2;
            float alignedY = mousePos.y - rectHeight / 2;

            // ������ק����
            dragRect = new Rect(alignedX, alignedY, rectWidth, rectHeight);

            // ����ԭʼ����������ת
            Matrix4x4 originalMatrix = GUI.matrix;
            Vector2 pivot = new Vector2(alignedX + rectWidth / 2, alignedY + rectHeight / 2);
            GUIUtility.RotateAroundPivot(draggedItemRotation * 90, pivot);

            // ���Ʊ���
            Color itemColor = draggedItem.GetRarityColor();
            GUI.color = new Color(itemColor.r, itemColor.g, itemColor.b, 0.8f);
            GUI.DrawTexture(dragRect, whiteTexture);

            // ���Ʊ߿�
            GUI.color = new Color(1f, 1f, 1f, 0.8f);
            GUI.Box(dragRect, "", GUI.skin.window);

            // ������Ʒͼ��
            if (draggedItem.icon != null)
            {
                Rect iconRect = dragRect;
                if (draggedItemRotation == 1)
                {
                    iconRect.width = rectHeight;
                    iconRect.height = rectWidth;
                    iconRect.x = pivot.x - iconRect.width / 2;
                    iconRect.y = pivot.y - iconRect.height / 2;
                }
                GUI.color = Color.white;
                GUI.DrawTexture(iconRect, draggedItem.icon.texture);
            }

            // �ָ�ԭʼ�������ɫ
            GUI.matrix = originalMatrix;
            GUI.color = Color.white;
        }

        // ���Ʒ���Ԥ���򣨿ɷ���Ϊ����ɫ�����ɷ���Ϊ��ɫ��
        private void DrawPlacementPreview()
        {
            Item draggedItem = itemManager.GetItemByID(draggedSlot.itemID);
            if (draggedItem == null) return;

            // ������ת��ĳߴ�
            int currentWidth = draggedItemRotation == 0 ? draggedItem.gridSize.x : draggedItem.gridSize.y;
            int currentHeight = draggedItemRotation == 0 ? draggedItem.gridSize.y : draggedItem.gridSize.x;

            // ת�����λ�õ�UI����ϵ��Y����Ҫ��ת��
            Vector2 mousePos = Event.current.mousePosition;
            mousePos.y = Screen.height - mousePos.y;  // ת��ΪGUI����

            // ��������ڱ��������е�������꣨ʹ�����λ�ö��Ǿ���λ�ã�
            // �ؼ��޸���ʹ������ڱ��������������㣬������Ļ��������
            float relativeMouseX = mousePos.x - gridStartX;
            float relativeMouseY = mousePos.y - gridStartY;

            int mouseGridX = Mathf.FloorToInt((relativeMouseX - slotSpacing) / (slotSize + slotSpacing));
            int mouseGridY = Mathf.FloorToInt((relativeMouseY - slotSpacing) / (slotSize + slotSpacing));

            // ����������������Ч��Χ��
            mouseGridX = Mathf.Clamp(mouseGridX, 0, gridWidth - currentWidth);
            mouseGridY = Mathf.Clamp(mouseGridY, 0, gridHeight - currentHeight);

            // ����Ԥ����λ�úʹ�С�����ڵ�ǰ������ʼλ�ã�
            // �ؼ��޸���ʼ�մӵ�ǰ�����gridStartX��gridStartY��ʼ��λ
            float previewX = gridStartX + mouseGridX * (slotSize + slotSpacing) + slotSpacing;
            float previewY = gridStartY + (gridHeight - mouseGridY - currentHeight) * (slotSize + slotSpacing) + slotSpacing;
            float previewWidth = currentWidth * slotSize + (currentWidth - 1) * slotSpacing;
            float previewHeight = currentHeight * slotSize + (currentHeight - 1) * slotSpacing;
            previewRect = new Rect(previewX, previewY, previewWidth, previewHeight);

            // �ж��Ƿ���Է��� - ʹ�����λ���ж�
            bool inBackpackArea = backpackAreaRect.Contains(mousePos);
            bool spaceAvailable = IsSpaceAvailable(mouseGridX, mouseGridY, currentWidth, currentHeight, draggedItemUniqueID);
            bool canPlace = inBackpackArea && spaceAvailable;

            // ����Ԥ����ɫ���ɷ��ã�����ɫ�����ɷ��ã���ɫ��
            previewColor = canPlace ? new Color(0.3f, 1f, 0.3f, 0.5f) : new Color(1f, 0.3f, 0.3f, 0.5f);

            // ���浱ǰGUI��ɫ������Ԥ����
            Color originalColor = GUI.color;
            GUI.color = previewColor;
            GUI.DrawTexture(previewRect, whiteTexture);
            GUI.color = originalColor;

            showPreview = true;
        }


        private void DrawDropZone()
        {
            GUI.color = new Color(1, 0, 0, 0.2f);
            GUI.DrawTexture(dropZoneRect, whiteTexture);
            GUI.color = Color.white;

            GUI.color = Color.red;
            GUI.Box(dropZoneRect, "", GUI.skin.window);
            GUI.color = Color.white;

            GUIStyle dropTextStyle = new GUIStyle(GUI.skin.label);
            dropTextStyle.fontSize = 16;
            dropTextStyle.alignment = TextAnchor.MiddleCenter;
            dropTextStyle.normal.textColor = Color.red;

            GUIUtility.RotateAroundPivot(90, new Vector2(dropZoneRect.x + dropZoneRect.width / 2, dropZoneRect.y + dropZoneRect.height / 2));
            GUI.Label(new Rect(
                dropZoneRect.x + dropZoneRect.width / 2 - 50,
                dropZoneRect.y + dropZoneRect.height / 2 - 100,
                200,
                30),
                "�ϵ��˴�����",
                dropTextStyle
            );
            GUIUtility.RotateAroundPivot(-90, new Vector2(dropZoneRect.x + dropZoneRect.width / 2, dropZoneRect.y + dropZoneRect.height / 2));
        }

        private Rect GetEquipmentSlotRect(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Head: return headSlotRect;
                case EquipmentSlot.Chest: return chestSlotRect;
                case EquipmentSlot.Belt: return beltSlotRect;
                case EquipmentSlot.Foot: return footSlotRect;
                default: return Rect.zero;
            }
        }

        private void DropItem(Item item)
        {
            if (item == null || !item.canBeDropped)
            {
                Debug.LogError("��Ʒ�޷�����");
                return;
            }

            // ȷ�����λ����Ч
            if (playerTransform == null)
            {
                playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
                if (playerTransform == null)
                {
                    Debug.LogError("�޷���ȡ���λ�ã��޷�������Ʒ��");
                    return;
                }
            }

            // �����ǰ��������Ʒ
            Vector3 dropPos = playerTransform.position + playerTransform.forward * 1.5f;
            dropPos.y = playerTransform.position.y + 0.5f;

            GameObject dropObj = null;
            if (item.itemPrefab != null)
            {
                dropObj = Instantiate(item.itemPrefab, dropPos, Quaternion.identity);

                // ȷ��Ԥ��������ײ��
                EnsureColliderExists(dropObj);
            }
            else
            {
                dropObj = new GameObject(item.itemName);
                dropObj.transform.position = dropPos;

                // �����ײ��
                SphereCollider collider = dropObj.AddComponent<SphereCollider>();
                collider.radius = 0.3f;
                collider.isTrigger = true;
            }

            if (item.useCustomDropScale)
            {
                dropObj.transform.localScale = item.dropScale;
            }

            // ���ʰȡ���
            PickupItem pickup = dropObj.GetComponent<PickupItem>() ?? dropObj.AddComponent<PickupItem>();
            pickup.item = item;
            pickup.amount = 1;

            Debug.Log($"��Ʒ�Ѷ���: {item.itemName}");
        }

        /// <summary>
        /// ȷ����������ײ�������û�������
        /// </summary>
        private void EnsureColliderExists(GameObject obj)
        {
            if (obj.GetComponent<Collider>() == null)
            {
                // ���������������״ƥ�����ײ��
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null && renderer.bounds.size.x > 0)
                {
                    // ���ڱ߽��Сѡ����ʵ���ײ��
                    Vector3 size = renderer.bounds.size;
                    if (Mathf.Abs(size.x - size.y) < 0.1f && Mathf.Abs(size.x - size.z) < 0.1f)
                    {
                        SphereCollider sphere = obj.AddComponent<SphereCollider>();
                        sphere.radius = size.x / 2;
                        sphere.isTrigger = true;
                    }
                    else
                    {
                        BoxCollider box = obj.AddComponent<BoxCollider>();
                        box.size = size;
                        box.isTrigger = true;
                    }
                }
                else
                {
                    // Ĭ�����������ײ��
                    SphereCollider collider = obj.AddComponent<SphereCollider>();
                    collider.radius = 0.3f;
                    collider.isTrigger = true;
                }
            }
            else
            {
                // ȷ��������ײ���Ǵ�����
                Collider existingCollider = obj.GetComponent<Collider>();
                existingCollider.isTrigger = true;
            }
        }

        // ����
        public void Heal(float amount)
        {
            if (currentHealth <= 0)
                return; // ����״̬�޷�����

            float previousHealth = currentHealth;
            currentHealth += amount;

            // ȷ������ֵ���ᳬ�����ֵ
            if (currentHealth > maxHealth)
                currentHealth = maxHealth;

            // ����ʵ��������
            float actualHeal = currentHealth - previousHealth;
            if (actualHeal > 0)
            {
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
            }
        }

        private void InitializeWhiteTexture()
        {
            if (whiteTexture == null)
            {
                whiteTexture = new Texture2D(1, 1);
                whiteTexture.SetPixel(0, 0, Color.white);
                whiteTexture.Apply();
            }
        }

        private void InitializeFullscreenLayout()
        {
            inventoryWindowRect = new Rect(0, 0, Screen.width, Screen.height);

            float playerAreaWidth = Screen.width * 0.3f;
            playerAreaRect = new Rect(0, 0, playerAreaWidth, Screen.height);

            backpackAreaRect = new Rect(
                playerAreaWidth,
                0,
                Screen.width - playerAreaWidth,
                Screen.height
            );

            float dropZoneWidth = backpackAreaRect.width * 0.15f;
            dropZoneRect = new Rect(
                backpackAreaRect.x + backpackAreaRect.width - dropZoneWidth,
                0,
                dropZoneWidth,
                Screen.height
            );
        }

        private void OnDestroy()
        {
            // ������������
            if (whiteTexture != null)
            {
                Destroy(whiteTexture);
            }
        }
    }
}
