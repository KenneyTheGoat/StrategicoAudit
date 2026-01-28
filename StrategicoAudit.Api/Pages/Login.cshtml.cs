using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StrategicoAudit.Api.Pages;

public record DemoActor(long UserId, string Name, string Role);

public class LoginModel : PageModel
{
    public string? Message { get; set; }

    public IActionResult OnGet()
    {
        Message = "Choose a mode.";
        return Page();
    }

    public IActionResult OnPost(string mode)
    {
        DemoActor actor = mode == "admin"
            ? new DemoActor(9001, "Admin Viewer", "ADMIN")
            : new DemoActor(1001, "Warehouse User", "USER");

        var json = JsonSerializer.Serialize(actor);
        Response.Cookies.Append("demo_actor", json, new CookieOptions
        {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Lax
        });

        return mode == "admin"
            ? Redirect("/admin/audit")
            : Redirect("/user/inventory-adjust");
    }
}
