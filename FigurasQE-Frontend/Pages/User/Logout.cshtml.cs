using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class LogoutModel : PageModel
{
    public async Task<IActionResult> OnGet()
    {
        await HttpContext.SignOutAsync("Cookies");
        Response.Cookies.Delete("jwt");

        HttpContext.Session.Clear();

        return RedirectToPage("/User/Login");
    }
}