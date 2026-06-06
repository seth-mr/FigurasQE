using System;
using System.Collections.Generic;

namespace MicroservicioFiguras.Models;

public partial class Level
{
    public int IdLevel { get; set; }

    public string? Name { get; set; }

    public int? Difficulty { get; set; }

    public virtual ICollection<LevelResult> LevelResults { get; set; } = new List<LevelResult>();
}
