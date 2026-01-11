using System;
using System.Collections.Generic;

namespace IFixZoneWeb.Models.Entities;

public partial class ProductSpecification
{
    public int SpecId { get; set; }

    public int ProductId { get; set; }

    public string? SpecName { get; set; }

    public string? SpecValue { get; set; }

    public virtual Product Product { get; set; } = null!;
}
