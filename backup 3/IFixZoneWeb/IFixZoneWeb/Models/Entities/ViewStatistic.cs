using System;
using System.Collections.Generic;

namespace IFixZoneWeb.Models.Entities;

public partial class ViewStatistic
{
    public DateOnly? OrderDay { get; set; }

    public int? TotalOrders { get; set; }

    public decimal? Revenue { get; set; }
}
