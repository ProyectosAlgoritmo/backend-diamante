using System;
using System.Collections.Generic;

namespace BackendDiamante.Models.Entities;

public partial class Company
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<CostCenter> CostCenters { get; set; } = new List<CostCenter>();
}
