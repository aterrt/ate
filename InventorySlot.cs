namespace InventorySystem
{
    public struct InventorySlot
    {
        public int itemID;       // 物品ID（0表示空）
        public int x;            // 物品左上角X坐标
        public int y;            // 物品左上角Y坐标
        public int parentID;     // 所属物品的唯一ID（用于关联同一物品的多个格子）
        public int rotation;     // 旋转状态（0:原始, 1:旋转90度）

        // 构造函数（无parentID和rotation）
        public InventorySlot(int itemID, int x, int y)
        {
            this.itemID = itemID;
            this.x = x;
            this.y = y;
            this.parentID = -1;
            this.rotation = 0;
        }

        // 构造函数（有parentID和rotation）
        public InventorySlot(int itemID, int x, int y, int parentID, int rotation = 0)
        {
            this.itemID = itemID;
            this.x = x;
            this.y = y;
            this.parentID = parentID;
            this.rotation = rotation;
        }

        // 检查槽位是否为空
        public bool IsEmpty()
        {
            return itemID == 0 && parentID == -1;
        }
    }
}
