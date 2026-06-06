using System;
using System.Collections.Generic;

namespace MicroservicioFiguras.Models;

public partial class LevelResult
{
    public int IdResult { get; set; }

    public int IdSession { get; set; }

    public int IdLevel { get; set; }

    public int? FinishingTime { get; set; }

    public int? Attempts { get; set; }

    public int? Fails { get; set; }

    public bool? Completed { get; set; }

    public virtual Level IdLevelNavigation { get; set; } = null!;

    public virtual Session IdSessionNavigation { get; set; } = null!;
}
