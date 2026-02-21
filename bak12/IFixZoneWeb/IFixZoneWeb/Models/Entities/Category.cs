using System.Collections.Generic;

namespace IFixZoneWeb.Models.Entities
{
    public partial class Category
    {
        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = null!;

        // FK tự tham chiếu (Danh mục cha)
        public int? ParentId { get; set; }

        // ===== Navigation properties =====

        // 🔹 Danh mục cha
        public virtual Category? Parent { get; set; }

        // 🔹 Danh mục con
        public virtual ICollection<Category> Children { get; set; }
            = new List<Category>();

        // 🔥 SẢN PHẨM THUỘC DANH MỤC (BẮT BUỘC GIỮ)
        public virtual ICollection<Product> Products { get; set; }
            = new List<Product>();
    }
}

