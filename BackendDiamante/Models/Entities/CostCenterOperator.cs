using System;
using System.Collections.Generic;

namespace BackendDiamante.Models.Entities;

public partial class CostCenterOperator
{
    public int Id { get; set; }

    public int CostCenterId { get; set; }

    public int OperatorId { get; set; }

    public DateTime AssignedAt { get; set; }

    public virtual CostCenter CostCenter { get; set; } = null!;

    public virtual Operator Operator { get; set; } = null!;
}
