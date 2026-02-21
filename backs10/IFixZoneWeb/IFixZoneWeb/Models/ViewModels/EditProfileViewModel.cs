using System;
using System.ComponentModel.DataAnnotations;

namespace IFixZoneWeb.Models.ViewModels
{
    public class EditProfileViewModel
    {
        public int Id { get; set; }

        // READONLY
        public string Username { get; set; } = null!;
        public string? Email { get; set; }
        public DateTime? CreatedAt { get; set; }

        // EDIT
        [Required(ErrorMessage = "Họ tên không được để trống")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [RegularExpression("^0[0-9]{8,9}$",
            ErrorMessage = "Số điện thoại phải bắt đầu bằng 0 và có 9–10 số")]
        public string Phone { get; set; } = null!;

        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        public string Address { get; set; } = null!;
    }
}
