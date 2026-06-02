using System;
using System.Collections.Generic;

namespace BackendDiamante.Models.Entities;

public partial class Sector
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Operator> Operators { get; set; } = new List<Operator>();
}
