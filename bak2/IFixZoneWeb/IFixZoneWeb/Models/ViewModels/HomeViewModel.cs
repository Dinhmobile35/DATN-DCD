using IFixZoneWeb.Models.Entities;
using System.Collections.Generic;

namespace IFixZoneWeb.Models.ViewModels
{
    public class HomeViewModel
    {
        public List<Category> Categories { get; set; }
        public List<Product> FeaturedProducts { get; set; }
    }
}
