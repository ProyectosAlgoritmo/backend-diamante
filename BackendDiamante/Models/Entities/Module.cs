using System;
using System.Collections.Generic;

namespace BackendDiamante.Models.Entities;

public partial class Module
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Submodule> Submodules { get; set; } = new List<Submodule>();
}
