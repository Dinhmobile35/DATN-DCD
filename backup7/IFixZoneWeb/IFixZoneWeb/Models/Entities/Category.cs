using System;
using System.Collections.Generic;

namespace IFixZoneWeb.Models.Entities;

public partial class Category
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public int? ParentId { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
