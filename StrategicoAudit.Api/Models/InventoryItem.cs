namespace StrategicoAudit.Api.Models;

public class InventoryItem
{
    public long Id { get; set; }
    public string WarehouseId { get; set; } = default!;
    public string Sku { get; set; } = default!;
    public int OnHand { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
