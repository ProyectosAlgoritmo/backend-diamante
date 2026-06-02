using System;
using System.Collections.Generic;

namespace BackendDiamante.Models.Entities;

public partial class CostCenter
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public int Areas { get; set; }

    public int CompanyId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual ICollection<CostCenterOperator> CostCenterOperators { get; set; } = new List<CostCenterOperator>();
}
