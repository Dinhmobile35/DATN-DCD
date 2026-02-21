using IFixZoneWeb.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IFixZoneWeb.ViewComponents
{
    public class CategoryMenuViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public CategoryMenuViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // ✅ 1 QUERY DUY NHẤT
            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            // BUILD TREE CHA → CON → CHÁU
            var tree = BuildTree(null, categories);

            return View(tree);
        }

        // ================== BUILD TREE ĐỆ QUY ==================
        private List<CategoryNode> BuildTree(int? parentId, List<Category> all)
        {
            return all
                .Where(c => c.ParentId == parentId)
                .Select(c => new CategoryNode
                {
                    Category = c,
                    Children = BuildTree(c.CategoryId, all)
                })
                .ToList();
        }
    }

    // ================== VIEW MODEL ==================
    public class CategoryNode
    {
        public Category Category { get; set; } = null!;
        public List<CategoryNode> Children { get; set; } = new();
    }
}
