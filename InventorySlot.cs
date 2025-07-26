namespace InventorySystem
{
    public struct InventorySlot
    {
        public int itemID;       // ��ƷID��0��ʾ�գ�
        public int x;            // ��Ʒ���Ͻ�X����
        public int y;            // ��Ʒ���Ͻ�Y����
        public int parentID;     // ������Ʒ��ΨһID�����ڹ���ͬһ��Ʒ�Ķ�����ӣ�
        public int rotation;     // ��ת״̬��0:ԭʼ, 1:��ת90�ȣ�

        // ���캯������parentID��rotation��
        public InventorySlot(int itemID, int x, int y)
        {
            this.itemID = itemID;
            this.x = x;
            this.y = y;
            this.parentID = -1;
            this.rotation = 0;
        }

        // ���캯������parentID��rotation��
        public InventorySlot(int itemID, int x, int y, int parentID, int rotation = 0)
        {
            this.itemID = itemID;
            this.x = x;
            this.y = y;
            this.parentID = parentID;
            this.rotation = rotation;
        }

        // ����λ�Ƿ�Ϊ��
        public bool IsEmpty()
        {
            return itemID == 0 && parentID == -1;
        }
    }
}
