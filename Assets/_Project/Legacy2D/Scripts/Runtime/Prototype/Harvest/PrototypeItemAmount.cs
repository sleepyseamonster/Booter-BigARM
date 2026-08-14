namespace BooterBigArm.Runtime
{
    public struct PrototypeItemAmount
    {
        public string ItemId;
        public int Amount;

        public PrototypeItemAmount(string itemId, int amount)
        {
            ItemId = itemId ?? string.Empty;
            Amount = amount < 0 ? 0 : amount;
        }
    }
}
