# 📘 IFixZone – Website bán linh kiện & dụng cụ sửa chữa điện tử

## 1. Giới thiệu
IFixZone là hệ thống website bán linh kiện và dụng cụ sửa chữa điện tử, hỗ trợ:
- Quản lý sản phẩm, danh mục
- Giỏ hàng, đặt hàng
- Đánh giá sản phẩm
- Quản lý người dùng & phân quyền
- Thống kê đơn hàng, doanh thu

Hệ thống được xây dựng bằng ASP.NET MVC, SQL Server, HTML, CSS, Bootstrap, JavaScript.

---

## 2. Công nghệ sử dụng
- Backend: ASP.NET MVC (.NET)
- Frontend: HTML, CSS, Bootstrap, JavaScript, jQuery (Ajax)
- Database: SQL Server
- ORM: Entity Framework
- Công cụ thiết kế: diagrams.net (draw.io)

---

## 3. Hướng dẫn triển khai

### 3.1 Yêu cầu môi trường
- Windows 10+
- Visual Studio 2019/2022
- SQL Server 2019+
- SQL Server Management Studio (SSMS)

### 3.2 Cài đặt cơ sở dữ liệu
1. Mở SQL Server Management Studio
2. Chạy file script IFixZone_DB.sql để tạo database và dữ liệu mẫu

### 3.3 Cấu hình kết nối
Trong appsettings.json hoặc Web.config:

```
Server=.;Database=IFixZone_DB;Trusted_Connection=True;
```

### 3.4 Chạy ứng dụng
- Mở project bằng Visual Studio
- Restore NuGet packages
- Nhấn F5 để chạy
- Truy cập http://localhost:xxxx

---

## 4. Tài khoản đăng nhập

### 4.1 Admin
- Username: admin
- Password: Dinh123

### 4.2 Staff
- Username: staff1
- Password: Dinh123

### 4.3 Customer
- Username: Dinhdc
- Password: Dinh123

---

## 5. Ghi chú
- Dữ liệu mẫu phục vụ học tập
- Có thể mở rộng thêm thanh toán online và gửi email

---

## 6. Tác giả
IFixZone Project – Đồ án môn học
