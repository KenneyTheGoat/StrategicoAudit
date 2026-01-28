using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StrategicoAudit.Api.Dtos;

namespace StrategicoAudit.Api.Pages.Admin;

public class InventoryModel : PageModel
{
    private readonly IHttpClientFactory _http;

    public InventoryModel(IHttpClientFactory http)
    {
        _http = http;
    }

    [BindProperty] public string WarehouseId { get; set; } = "W1";
    [BindProperty] public string Sku { get; set; } = "ABC123";
    [BindProperty] public int NewOnHand { get; set; } = 110;
    [BindProperty] public string? Reason { get; set; } = "Damaged";

    public string? Message { get; set; }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var dto = new InventoryAdjustDto
        {
            WarehouseId = WarehouseId,
            Sku = Sku,
            NewOnHand = NewOnHand,
            Reason = Reason
        };

        var client = _http.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/admin/inventory/adjust")
        {
            Content = JsonContent.Create(dto)
        };
        request.Headers.Add("X-Request-Id", Guid.NewGuid().ToString("N"));

        var res = await client.SendAsync(request, ct);

        Message = res.IsSuccessStatusCode
            ? "Adjustment successful (audit logged)."
            : $"Failed: {(int)res.StatusCode} {res.ReasonPhrase}";

        return Page();
    }
}
