using System;
using System.Collections.Generic;

namespace IFixZoneWeb.Models.Entities;

public partial class Order
{
    public int OrderId { get; set; }

    public string? OrderCode { get; set; }

    public int UserId { get; set; }

    public decimal? TotalAmount { get; set; }

    public string? Status { get; set; }

    public DateTime? OrderDate { get; set; }

    public string? RecipientName { get; set; }

    public string? RecipientPhone { get; set; }

    public string? RecipientAddress { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<OrderStatusHistory> OrderStatusHistories { get; set; } = new List<OrderStatusHistory>();

    public virtual User User { get; set; } = null!;
}
