using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace InventorySystem
{
    public class Inventory : MonoBehaviour
    {
        [Header("基础设置")]
        public ItemManager itemManager;                  // 物品管理器
        public int gridWidth = 10;                       // 背包网格宽度（列数）
        public int gridHeight = 8;                       // 背包网格高度（行数）
        public KeyCode inventoryKey = KeyCode.Tab;       // 打开/关闭背包的按键
        public float slotSize = 60f;                     // 单个格子大小（像素）
        public float slotSpacing = 5f;                   // 格子间距（像素）
        public GUIStyle itemNameStyle;                   // 物品名称显示样式
        public GUIStyle tooltipStyle;                    // 物品提示样式

        [Header("装备槽位置设置")]
        public Rect headSlotRect = new Rect(150, 100, 60, 60);    // 头部装备槽位置
        public Rect chestSlotRect = new Rect(150, 180, 60, 80);   // 上身装备槽位置
        public Rect beltSlotRect = new Rect(150, 280, 60, 40);    // 腰带装备槽位置
        public Rect footSlotRect = new Rect(150, 340, 60, 60);    // 鞋子装备槽位置

        [Header("生命值设置")]
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

        // 装备系统相关
        private Dictionary<EquipmentSlot, Item> equippedItems = new Dictionary<EquipmentSlot, Item>();
        private Dictionary<EquipmentSlot, int> equippedItemUniqueIDs = new Dictionary<EquipmentSlot, int>();
        public UnityEvent<EquipmentSlot, Item> onEquip;  // 装备事件
        public UnityEvent<EquipmentSlot> onUnequip;      // 卸下事件

        // 背包容量扩展相关
        private List<Item> usedBackpacks = new List<Item>(); // 已使用的背包列表

        // 旋转功能相关
        private int draggedItemRotation = 0;

        // 拖拽状态跟踪
        private Vector2 dragStartPosition;
        private bool isPotentialDrag = false;
        private bool isDragging = false;
        private bool isCtrlDrop = false;

        // 网格布局计算缓存
        private float gridTotalWidth;
        private float gridTotalHeight;
        private float gridStartX;
        private float gridStartY;

        // 预览框相关变量
        private Rect previewRect;
        private Color previewColor;
        private bool showPreview = false;

        // 生命值变化事件
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
            // 处理旋转输入
            if (isDragging && draggedItemUniqueID != -1 && draggedSlot.itemID != 0)
            {
                Item draggedItem = itemManager.GetItemByID(draggedSlot.itemID);
                if (draggedItem != null && draggedItem.allowRotation && Input.GetKeyDown(KeyCode.R))
                {
                    draggedItemRotation = (draggedItemRotation + 1) % 2;
                }
            }

            // 屏幕尺寸变化时重新计算布局
            if (inventoryWindowRect.width != Screen.width || inventoryWindowRect.height != Screen.height)
            {
                InitializeFullscreenLayout();
            }

            // 处理背包打开/关闭
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

                if (distance < 8f) // 拖拽阈值
                {
                    ResetDragState();
                }

                isPotentialDrag = false;
            }
            else if (isPotentialDrag && !isDragging)
            {
                float distance = Vector2.Distance(Input.mousePosition, dragStartPosition);

                if (distance >= 8f) // 拖拽阈值
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
            showPreview = false; // 重置预览状态
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
            // 背包特殊处理 - 直接使用增加容量
            if (item.itemType == ItemType.Backpack)
            {
                return UseBackpack(item);
            }

            // 装备类型处理
            if (item.itemType == ItemType.Armor && item.equipSlot != EquipmentSlot.None)
            {
                if (equippedItems[item.equipSlot] == null)
                {
                    EquipItem(item.equipSlot, item);
                    return true;
                }
            }

            // 普通物品添加到背包
            return AddItem(item);
        }

        // 背包使用逻辑 - 专门处理背包扩容
        private bool UseBackpack(Item backpack)
        {
            if (backpack == null || backpack.itemType != ItemType.Backpack) return false;

            // 检查是否已使用过该背包
            if (usedBackpacks.Contains(backpack))
            {
                Debug.LogWarning("该背包已使用过");
                return false;
            }

            // 增加容量
            IncreaseCapacity(backpack.addWidth, backpack.addHeight);
            usedBackpacks.Add(backpack);

            // 添加到背包作为已使用物品
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

            // 检查是否是背包物品
            Item removedItem = GetItemByUniqueID(uniqueID);
            if (removedItem != null && removedItem.itemType == ItemType.Backpack)
            {
                if (usedBackpacks.Contains(removedItem))
                {
                    // 移除背包时减少容量
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

        // 装备物品
        public void EquipItem(EquipmentSlot slot, Item item)
        {
            if (item == null) return;

            // 如果该槽位已有装备，先卸下并添加到背包
            if (equippedItems[slot] != null)
            {
                Item oldItem = equippedItems[slot];
                int oldUniqueID = equippedItemUniqueIDs[slot];

                // 从装备槽移除
                equippedItems[slot] = null;
                equippedItemUniqueIDs[slot] = -1;

                // 添加到背包
                AddItem(oldItem);
            }

            // 添加新装备到槽位
            equippedItems[slot] = item;
            equippedItemUniqueIDs[slot] = nextUniqueID++;

            // 触发装备事件
            onEquip?.Invoke(slot, item);
        }

        // 卸下装备
        public void UnequipItem(EquipmentSlot slot)
        {
            if (equippedItems[slot] != null)
            {
                // 将装备添加到背包
                AddItem(equippedItems[slot]);

                // 从装备槽移除
                equippedItems[slot] = null;
                equippedItemUniqueIDs[slot] = -1;

                // 触发卸下事件
                onUnequip?.Invoke(slot);
            }
        }

        /// <summary>
        /// 增加背包容量
        /// </summary>
        public void IncreaseCapacity(int addWidth, int addHeight)
        {
            // 记录当前容量
            int oldWidth = gridWidth;
            int oldHeight = gridHeight;

            // 增加容量值
            gridWidth += addWidth;
            gridHeight += addHeight;

            // 创建新的存储结构并复制现有物品
            List<InventorySlot> newSlots = new List<InventorySlot>();

            // 先填充新格子
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    newSlots.Add(new InventorySlot(0, x, y));
                }
            }

            // 复制旧格子中的物品
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

            // 替换为新的存储结构
            slots = newSlots;

            // 重新初始化布局
            InitializeFullscreenLayout();

            Debug.Log($"背包容量已增加到: {gridWidth}x{gridHeight}");
        }

        /// <summary>
        /// 减少背包容量
        /// </summary>
        public void DecreaseCapacity(int removeWidth, int removeHeight)
        {
            // 确保容量不会小于最小值
            int minWidth = 5;
            int minHeight = 4;

            gridWidth = Mathf.Max(minWidth, gridWidth - removeWidth);
            gridHeight = Mathf.Max(minHeight, gridHeight - removeHeight);

            // 创建新的存储结构
            List<InventorySlot> newSlots = new List<InventorySlot>();
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    newSlots.Add(new InventorySlot(0, x, y));
                }
            }

            // 复制可以保留的物品
            foreach (var slot in slots)
            {
                if (slot.itemID != 0 && slot.x < gridWidth && slot.y < gridHeight)
                {
                    Item item = itemManager.GetItemByID(slot.itemID);
                    if (item != null)
                    {
                        int width = slot.rotation == 0 ? item.gridSize.x : item.gridSize.y;
                        int height = slot.rotation == 0 ? item.gridSize.y : item.gridSize.x;

                        // 检查物品是否能在新容量中放下
                        if (slot.x + width <= gridWidth && slot.y + height <= gridHeight)
                        {
                            PlaceItemAtPosition(item, slot.x, slot.y, slot.parentID, slot.rotation);
                        }
                        else
                        {
                            // 无法放下的物品将被丢弃
                            DropItem(item);
                        }
                    }
                }
            }

            // 替换为新的存储结构
            slots = newSlots;

            // 重新初始化布局
            InitializeFullscreenLayout();

            Debug.Log($"背包容量已减少到: {gridWidth}x{gridHeight}");
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
                DrawPlacementPreview(); // 绘制放置预览框
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
            GUI.Label(new Rect(playerAreaRect.x, 10, playerAreaRect.width, 30), "角色信息", titleStyle);
        }

        // 绘制装备槽
        private void DrawEquipmentSlots()
        {
            // 计算装备槽在玩家区域内的位置
            float slotX = playerAreaRect.x + (playerAreaRect.width - 60) / 2;

            // 头部装备槽
            headSlotRect.x = slotX;
            GUI.Box(headSlotRect, "头部", GUI.skin.window);
            if (equippedItems[EquipmentSlot.Head] != null)
            {
                DrawEquippedItem(headSlotRect, equippedItems[EquipmentSlot.Head]);
                HandleEquipmentSlotInput(headSlotRect, EquipmentSlot.Head);
            }

            // 上身装备槽
            chestSlotRect.x = slotX;
            GUI.Box(chestSlotRect, "上身", GUI.skin.window);
            if (equippedItems[EquipmentSlot.Chest] != null)
            {
                DrawEquippedItem(chestSlotRect, equippedItems[EquipmentSlot.Chest]);
                HandleEquipmentSlotInput(chestSlotRect, EquipmentSlot.Chest);
            }

            // 腰带装备槽
            beltSlotRect.x = slotX;
            GUI.Box(beltSlotRect, "腰带", GUI.skin.window);
            if (equippedItems[EquipmentSlot.Belt] != null)
            {
                DrawEquippedItem(beltSlotRect, equippedItems[EquipmentSlot.Belt]);
                HandleEquipmentSlotInput(beltSlotRect, EquipmentSlot.Belt);
            }

            // 鞋子装备槽
            footSlotRect.x = slotX;
            GUI.Box(footSlotRect, "鞋子", GUI.skin.window);
            if (equippedItems[EquipmentSlot.Foot] != null)
            {
                DrawEquippedItem(footSlotRect, equippedItems[EquipmentSlot.Foot]);
                HandleEquipmentSlotInput(footSlotRect, EquipmentSlot.Foot);
            }
        }

        // 绘制已装备的物品
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

        // 处理装备槽输入
        private void HandleEquipmentSlotInput(Rect slotRect, EquipmentSlot slot)
        {
            Event currentEvent = Event.current;
            if (slotRect.Contains(currentEvent.mousePosition))
            {
                // 左键点击卸下装备
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
            GUI.Label(new Rect(titleX, titleY, 200, 30), "背包物品", titleStyle);

            // 绘制格子背景
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

            // 绘制物品
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
                            // 检查是否是正在拖拽的物品
                            bool isBeingDragged = isDragging && draggedItemUniqueID == slot.parentID;

                            // 拖拽时不绘制原始位置的物品
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

                            // 绘制物品背景
                            DrawItemBackground(item, firstSlotRect, displayWidth, displayHeight);
                            // 绘制物品图标
                            DrawItemIcon(item, firstSlotRect, displayWidth, displayHeight, rotation);
                            // 绘制物品名称
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

            // 处理空槽位输入
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
                // 右键使用物品
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

            // 检查是否拖到了装备槽
            Item draggedItem = itemManager.GetItemByID(draggedSlot.itemID);
            if (draggedItem != null && draggedItem.itemType == ItemType.Armor && draggedItem.equipSlot != EquipmentSlot.None)
            {
                Rect targetSlotRect = GetEquipmentSlotRect(draggedItem.equipSlot);
                if (targetSlotRect.Contains(mousePos))
                {
                    // 从背包移除
                    RemoveItemByUniqueID(draggedItemUniqueID);
                    // 装备到对应槽位
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

            // 计算旋转后的尺寸
            int currentWidth = draggedItemRotation == 0 ? draggedItem.gridSize.x : draggedItem.gridSize.y;
            int currentHeight = draggedItemRotation == 0 ? draggedItem.gridSize.y : draggedItem.gridSize.x;

            // 计算拖拽矩形大小
            float rectWidth = slotSize * currentWidth + slotSpacing * (currentWidth - 1);
            float rectHeight = slotSize * currentHeight + slotSpacing * (currentHeight - 1);

            // 转换鼠标坐标到GUI系统
            Vector2 mousePos = Input.mousePosition;
            mousePos.y = Screen.height - mousePos.y;

            // 计算对齐到网格的位置
            float alignedX = mousePos.x - rectWidth / 2;
            float alignedY = mousePos.y - rectHeight / 2;

            // 设置拖拽矩形
            dragRect = new Rect(alignedX, alignedY, rectWidth, rectHeight);

            // 保存原始矩阵并设置旋转
            Matrix4x4 originalMatrix = GUI.matrix;
            Vector2 pivot = new Vector2(alignedX + rectWidth / 2, alignedY + rectHeight / 2);
            GUIUtility.RotateAroundPivot(draggedItemRotation * 90, pivot);

            // 绘制背景
            Color itemColor = draggedItem.GetRarityColor();
            GUI.color = new Color(itemColor.r, itemColor.g, itemColor.b, 0.8f);
            GUI.DrawTexture(dragRect, whiteTexture);

            // 绘制边框
            GUI.color = new Color(1f, 1f, 1f, 0.8f);
            GUI.Box(dragRect, "", GUI.skin.window);

            // 绘制物品图标
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

            // 恢复原始矩阵和颜色
            GUI.matrix = originalMatrix;
            GUI.color = Color.white;
        }

        // 绘制放置预览框（可放置为淡绿色，不可放置为红色）
        private void DrawPlacementPreview()
        {
            Item draggedItem = itemManager.GetItemByID(draggedSlot.itemID);
            if (draggedItem == null) return;

            // 计算旋转后的尺寸
            int currentWidth = draggedItemRotation == 0 ? draggedItem.gridSize.x : draggedItem.gridSize.y;
            int currentHeight = draggedItemRotation == 0 ? draggedItem.gridSize.y : draggedItem.gridSize.x;

            // 转换鼠标位置到UI坐标系（Y轴需要翻转）
            Vector2 mousePos = Event.current.mousePosition;
            mousePos.y = Screen.height - mousePos.y;  // 转换为GUI坐标

            // 计算鼠标在背包网格中的相对坐标（使用相对位置而非绝对位置）
            // 关键修复：使用相对于背包区域的坐标计算，而非屏幕绝对坐标
            float relativeMouseX = mousePos.x - gridStartX;
            float relativeMouseY = mousePos.y - gridStartY;

            int mouseGridX = Mathf.FloorToInt((relativeMouseX - slotSpacing) / (slotSize + slotSpacing));
            int mouseGridY = Mathf.FloorToInt((relativeMouseY - slotSpacing) / (slotSize + slotSpacing));

            // 限制网格坐标在有效范围内
            mouseGridX = Mathf.Clamp(mouseGridX, 0, gridWidth - currentWidth);
            mouseGridY = Mathf.Clamp(mouseGridY, 0, gridHeight - currentHeight);

            // 计算预览框位置和大小（基于当前网格起始位置）
            // 关键修复：始终从当前计算的gridStartX和gridStartY开始定位
            float previewX = gridStartX + mouseGridX * (slotSize + slotSpacing) + slotSpacing;
            float previewY = gridStartY + (gridHeight - mouseGridY - currentHeight) * (slotSize + slotSpacing) + slotSpacing;
            float previewWidth = currentWidth * slotSize + (currentWidth - 1) * slotSpacing;
            float previewHeight = currentHeight * slotSize + (currentHeight - 1) * slotSpacing;
            previewRect = new Rect(previewX, previewY, previewWidth, previewHeight);

            // 判断是否可以放置 - 使用相对位置判断
            bool inBackpackArea = backpackAreaRect.Contains(mousePos);
            bool spaceAvailable = IsSpaceAvailable(mouseGridX, mouseGridY, currentWidth, currentHeight, draggedItemUniqueID);
            bool canPlace = inBackpackArea && spaceAvailable;

            // 设置预览颜色（可放置：淡绿色，不可放置：红色）
            previewColor = canPlace ? new Color(0.3f, 1f, 0.3f, 0.5f) : new Color(1f, 0.3f, 0.3f, 0.5f);

            // 保存当前GUI颜色并绘制预览框
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
                "拖到此处丢弃",
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
                Debug.LogError("物品无法丢弃");
                return;
            }

            // 确保玩家位置有效
            if (playerTransform == null)
            {
                playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
                if (playerTransform == null)
                {
                    Debug.LogError("无法获取玩家位置，无法丢弃物品！");
                    return;
                }
            }

            // 在玩家前方生成物品
            Vector3 dropPos = playerTransform.position + playerTransform.forward * 1.5f;
            dropPos.y = playerTransform.position.y + 0.5f;

            GameObject dropObj = null;
            if (item.itemPrefab != null)
            {
                dropObj = Instantiate(item.itemPrefab, dropPos, Quaternion.identity);

                // 确保预制体有碰撞器
                EnsureColliderExists(dropObj);
            }
            else
            {
                dropObj = new GameObject(item.itemName);
                dropObj.transform.position = dropPos;

                // 添加碰撞器
                SphereCollider collider = dropObj.AddComponent<SphereCollider>();
                collider.radius = 0.3f;
                collider.isTrigger = true;
            }

            if (item.useCustomDropScale)
            {
                dropObj.transform.localScale = item.dropScale;
            }

            // 添加拾取组件
            PickupItem pickup = dropObj.GetComponent<PickupItem>() ?? dropObj.AddComponent<PickupItem>();
            pickup.item = item;
            pickup.amount = 1;

            Debug.Log($"物品已丢弃: {item.itemName}");
        }

        /// <summary>
        /// 确保物体有碰撞器，如果没有则添加
        /// </summary>
        private void EnsureColliderExists(GameObject obj)
        {
            if (obj.GetComponent<Collider>() == null)
            {
                // 尝试添加与物体形状匹配的碰撞器
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null && renderer.bounds.size.x > 0)
                {
                    // 基于边界大小选择合适的碰撞器
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
                    // 默认添加球形碰撞器
                    SphereCollider collider = obj.AddComponent<SphereCollider>();
                    collider.radius = 0.3f;
                    collider.isTrigger = true;
                }
            }
            else
            {
                // 确保现有碰撞器是触发器
                Collider existingCollider = obj.GetComponent<Collider>();
                existingCollider.isTrigger = true;
            }
        }

        // 治疗
        public void Heal(float amount)
        {
            if (currentHealth <= 0)
                return; // 死亡状态无法治疗

            float previousHealth = currentHealth;
            currentHealth += amount;

            // 确保生命值不会超过最大值
            if (currentHealth > maxHealth)
                currentHealth = maxHealth;

            // 计算实际治疗量
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
            // 清理创建的纹理
            if (whiteTexture != null)
            {
                Destroy(whiteTexture);
            }
        }
    }
}
