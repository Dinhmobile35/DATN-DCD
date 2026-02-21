using IFixZoneWeb.Models.Entities;
using IFixZoneWeb.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IFixZoneWeb.Controllers
{
    [Authorize] // 🔐 BẮT BUỘC ĐĂNG NHẬP
    public class CheckoutController : Controller
    {
        private readonly AppDbContext _context;

        public CheckoutController(AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // 🔑 LẤY USER ID TỪ COOKIE AUTH
        // =====================================================
        private int? GetUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdStr, out int id) ? id : null;
        }

        // =====================================================
        // 🛒 CHECKOUT PAGE (GIỎ HÀNG / BUY NOW)
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Index(CheckoutViewModel model)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(userId.Value);

            // ================= BUY NOW =================
            if (model.IsBuyNow)
            {
                model.User = user;
                return View(model);
            }

            // ================= CART CHECKOUT =================
            var cart = await GetCurrentCart(userId.Value);
            if (cart == null || !cart.CartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "Cart");
            }

            var selectedItems = cart.CartItems
                .Where(i => model.SelectedCartItemIds.Contains(i.CartItemId))
                .ToList();

            if (!selectedItems.Any())
            {
                TempData["Error"] = "Bạn chưa chọn sản phẩm nào để thanh toán.";
                return RedirectToAction("Index", "Cart");
            }

            model.Cart = cart;
            model.User = user;
            model.RecipientName = user?.FullName ?? "";
            model.RecipientPhone = user?.Phone ?? "";
            model.RecipientAddress = user?.Address ?? "";

            return View(model);
        }

        // =====================================================
        // 💳 XỬ LÝ ĐẶT HÀNG (POST)
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Process(CheckoutViewModel model)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(userId.Value);

            // =================================================
            // ⚡ BUY NOW – KIỂM TRA & TRỪ KHO
            // =================================================
            if (model.IsBuyNow && model.BuyNowProduct != null)
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductId == model.BuyNowProduct.ProductId);

                if (product == null)
                {
                    TempData["Error"] = "Sản phẩm không tồn tại.";
                    return RedirectToAction("Index", "Home");
                }

                // 🔒 CHECK TỒN KHO REALTIME (SERVER-SIDE)
                if (product.Stock < model.BuyNowQuantity)
                {
                    TempData["Error"] =
                        $"Sản phẩm chỉ còn {product.Stock} chiếc, vui lòng giảm số lượng.";
                    return RedirectToAction("Details", "Product",
                        new { id = product.ProductId });
                }

                // 🔥 CẬP NHẬT THÔNG TIN USER
                user.FullName = model.RecipientName;
                user.Phone = model.RecipientPhone;
                user.Address = model.RecipientAddress;

                // 🔥 TRỪ TỒN KHO (CHỈ TRỪ TẠI ĐÂY)
                product.Stock -= model.BuyNowQuantity;

                // 🔥 TẠO ĐƠN HÀNG
                var order = new Order
                {
                    UserId = userId.Value,
                    OrderCode = "DH" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                    TotalAmount = product.Price * model.BuyNowQuantity,
                    Status = "Mới",
                    OrderDate = DateTime.Now,
                    RecipientName = model.RecipientName,
                    RecipientPhone = model.RecipientPhone,
                    RecipientAddress = model.RecipientAddress
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                _context.OrderDetails.Add(new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = product.ProductId,
                    Quantity = model.BuyNowQuantity,
                    UnitPrice = product.Price
                });

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Đặt hàng thành công! Mã đơn: {order.OrderCode}";
                return RedirectToAction("OrderSuccess");
            }

            // =================================================
            // 🛒 CART CHECKOUT – KIỂM TRA & TRỪ KHO
            // =================================================
            var cart = await GetCurrentCart(userId.Value);
            if (cart == null || !cart.CartItems.Any())
                return RedirectToAction("Index", "Cart");

            var selectedItems = cart.CartItems
                .Where(i => model.SelectedCartItemIds.Contains(i.CartItemId))
                .ToList();

            if (!selectedItems.Any())
            {
                TempData["Error"] = "Không có sản phẩm nào được chọn.";
                return RedirectToAction("Index", "Cart");
            }

            // 🔒 CHECK TỒN KHO TẤT CẢ ITEM
            foreach (var item in selectedItems)
            {
                if (item.Product.Stock < item.Quantity)
                {
                    TempData["Error"] =
                        $"Sản phẩm \"{item.Product.ProductName}\" không đủ tồn kho.";
                    return RedirectToAction("Index", "Cart");
                }
            }

            user.FullName = model.RecipientName;
            user.Phone = model.RecipientPhone;
            user.Address = model.RecipientAddress;

            var newOrder = new Order
            {
                UserId = userId.Value,
                OrderCode = "DH" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                TotalAmount = selectedItems.Sum(i => i.Quantity * i.Product.Price),
                Status = "Mới",
                OrderDate = DateTime.Now,
                RecipientName = model.RecipientName,
                RecipientPhone = model.RecipientPhone,
                RecipientAddress = model.RecipientAddress
            };

            _context.Orders.Add(newOrder);
            await _context.SaveChangesAsync();

            foreach (var item in selectedItems)
            {
                item.Product.Stock -= item.Quantity;

                _context.OrderDetails.Add(new OrderDetail
                {
                    OrderId = newOrder.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price
                });
            }

            _context.CartItems.RemoveRange(selectedItems);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đặt hàng thành công! Mã đơn: {newOrder.OrderCode}";
            return RedirectToAction("OrderSuccess");
        }

        // =====================================================
        // ✅ TRANG THÀNH CÔNG
        // =====================================================
        public IActionResult OrderSuccess() => View();

        // =====================================================
        // 🛍️ LẤY GIỎ HÀNG HIỆN TẠI
        // =====================================================
        private async Task<Cart?> GetCurrentCart(int userId)
        {
            return await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        // =====================================================
        // ⚡ BUY NOW – KHỞI TẠO CHECKOUT
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> BuyNow(int productId, int quantity = 1)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account",
                    new { returnUrl = Url.Action("BuyNow", "Checkout", new { productId, quantity }) });

            if (quantity < 1) quantity = 1;

            var user = await _context.Users.FindAsync(userId.Value);
            var product = await _context.Products.FindAsync(productId);

            if (product == null)
            {
                TempData["Error"] = "Sản phẩm không tồn tại.";
                return RedirectToAction("Index", "Home");
            }

            if (product.Stock < quantity)
            {
                TempData["Error"] = $"Chỉ còn {product.Stock} sản phẩm trong kho.";
                return RedirectToAction("Details", "Product", new { id = productId });
            }

            var model = new CheckoutViewModel
            {
                IsBuyNow = true,
                User = user,
                RecipientName = user?.FullName ?? "",
                RecipientPhone = user?.Phone ?? "",
                RecipientAddress = user?.Address ?? "",
                BuyNowProduct = product,
                BuyNowQuantity = quantity,
                BuyNowTotal = product.Price * quantity
            };

            return View("Index", model);
        }
        [HttpGet]
        public async Task<IActionResult> VietQr(int orderId)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                TempData["Error"] = "Đơn hàng không tồn tại";
                return RedirectToAction("Index", "Home");
            }

            var qrUrl =
                $"https://img.vietqr.io/image/MB-0399355483-compact.png" +
                $"?amount={order.TotalAmount:0}" +
                $"&addInfo={Uri.EscapeDataString(order.OrderCode)}" +
                $"&accountName={Uri.EscapeDataString("DINH CONG DINH")}";

            ViewBag.QrUrl = qrUrl;
            ViewBag.OrderCode = order.OrderCode;
            ViewBag.Amount = order.TotalAmount;

            return View();
        }

    }
}
