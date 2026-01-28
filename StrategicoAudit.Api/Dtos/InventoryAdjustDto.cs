namespace StrategicoAudit.Api.Dtos;

public class InventoryAdjustDto
{
    public string Sku { get; set; } = default!;
    public string WarehouseId { get; set; } = default!;
    public int NewOnHand { get; set; }
    public string? Reason { get; set; }
}
