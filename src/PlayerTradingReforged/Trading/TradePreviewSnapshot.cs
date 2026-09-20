namespace PlayerTradingReforged.Trading;

// Ship the main inventory and its reserved slots together so the preview cannot mix two states.
internal sealed class TradePreviewSnapshot
{
    public TradePreviewSnapshot(Inventory available, Inventory reserved) { Available = available; Reserved = reserved; }
    public Inventory Available { get; }
    public Inventory Reserved { get; }
    public string Encode()
    {
        string available = TradeInventory.SavePreview(Available);
        string reserved = TradeInventory.SavePreview(Reserved);
        string result = available + "|" + reserved;
        return available.Length > 0 && reserved.Length > 0 && result.Length <= TradeInventory.MaxPackageBytes * 2 ? result : "";
    }
    public static TradePreviewSnapshot? Parse(string text)
    {
        if (text.Length > TradeInventory.MaxPackageBytes * 2) return null;
        int separator = text.IndexOf('|');
        if (separator < 1 || text.IndexOf('|', separator + 1) >= 0 ||
            !TradeInventory.TryLoadPreview(text.Substring(0, separator), out var available) ||
            !TradeInventory.TryLoadPreview(text.Substring(separator + 1), out var reserved) ||
            available.GetWidth() != reserved.GetWidth() || available.GetHeight() != reserved.GetHeight()) return null;
        foreach (var item in reserved.GetAllItems())
        {
            if (item.m_equipped || item.m_shared.m_questItem) return null;
            var remaining = available.GetItemAt(item.m_gridPos.x, item.m_gridPos.y);
            if (remaining != null && (!remaining.IsSameType(item) ||
                item.m_stack > remaining.m_shared.m_maxStackSize - remaining.m_stack)) return null;
        }
        return new TradePreviewSnapshot(available, reserved);
    }
}
