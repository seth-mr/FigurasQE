namespace FQE.AdminClient.Models;

public class AdminLogFilterCatalog
{
    public List<string> Services { get; set; } = [];
    public List<string> Types { get; set; } = [];
    public List<string> EntityTypes { get; set; } = [];
    public List<string> Actions { get; set; } = [];
    public List<string> StatusClasses { get; set; } = [];
}