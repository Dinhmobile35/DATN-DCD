using IFixZoneWeb.Models.Entities;

namespace IFixZoneWeb.Models.ViewModels
{
    public class CheckoutViewModel
    {
        public Cart Cart { get; set; } = new Cart();
        public List<int> SelectedCartItemIds { get; set; } = new List<int>(); // Danh sách ID CartItem được chọn
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string RecipientAddress { get; set; } = string.Empty;
        public User? User { get; set; }
    }
}