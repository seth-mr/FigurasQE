using FigurasQE_WebClient.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FigurasQE_WebClient.Pages;

public class Level4Model : PageModel
{
    public LevelAnswer Answer { get; private set; } = new LevelAnswer();
    public string NextLevelRoute { get; set; }

    public void OnGet()
    {
        Answer.Left = 2;
        Answer.Right = 2;
        Answer.Total = Answer.Left + Answer.Right;
        NextLevelRoute = "/Levels/Level5/Level5";
    }
}

