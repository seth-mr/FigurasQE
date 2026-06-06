using FigurasQE_WebClient.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FigurasQE_WebClient.Pages;

public class Level6Model : PageModel
{
    public LevelAnswer Answer { get; private set; } = new LevelAnswer();
    public string NextLevelRoute { get; set; }

    public void OnGet()
    {
        Answer.Left = 4;
        Answer.Right = 3;
        Answer.Total = Answer.Left + Answer.Right;
        NextLevelRoute = "/Levels/LevelsCatalog";
    }
}

