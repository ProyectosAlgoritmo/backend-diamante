using System;
using System.Collections.Generic;

namespace BackendDiamante.Models.Entities;

public partial class Operator
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string Shift { get; set; } = null!;

    public int? SectorId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<CostCenterOperator> CostCenterOperators { get; set; } = new List<CostCenterOperator>();

    public virtual Sector? Sector { get; set; }
}
