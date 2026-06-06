using System;
using System.Collections.Generic;

namespace MicroservicioFiguras.Models;

public partial class Session
{
    public int IdSession { get; set; }

    public int IdStudent { get; set; }

    public DateTime? BeginningDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? Device { get; set; }

    public virtual Student IdStudentNavigation { get; set; } = null!;

    public virtual ICollection<LevelResult> LevelResults { get; set; } = new List<LevelResult>();
}
