using IFixZoneWeb.Models.Entities;

namespace IFixZoneWeb.Models.ViewModels
{
    public class CheckoutViewModel
    {
        // ===== CHUNG =====
        public User? User { get; set; }

        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string RecipientAddress { get; set; } = string.Empty;

        // ===== CART MODE =====
        public Cart Cart { get; set; } = new Cart();
        public List<int> SelectedCartItemIds { get; set; } = new();

        // ===== BUY NOW MODE =====
        public bool IsBuyNow { get; set; }          // 🔥 QUAN TRỌNG
        public Product? BuyNowProduct { get; set; }
        public int BuyNowQuantity { get; set; }
        public decimal BuyNowTotal { get; set; }
    }

}