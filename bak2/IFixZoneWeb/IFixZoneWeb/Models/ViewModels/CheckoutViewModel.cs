using IFixZoneWeb.Models.Entities;

namespace IFixZoneWeb.Models.ViewModels
{
    public class CheckoutViewModel
    {
        public Cart Cart { get; set; } = new Cart(); // Giỏ hàng
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string RecipientAddress { get; set; } = string.Empty;
    }
}