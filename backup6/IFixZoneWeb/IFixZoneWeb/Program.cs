using IFixZoneWeb.Models.Entities;
using IFixZoneWeb.Helpers;              // 🔹 THÊM
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ================= DATABASE =================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// ================= MVC =================
builder.Services.AddControllersWithViews();

// ================= SESSION =================
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Cho phép dùng HttpContext trong View (_Layout)
builder.Services.AddHttpContextAccessor();

// ================= RENDER VIEW TO STRING =================
// 👉 BẮT BUỘC cho Mini Cart
builder.Services.AddScoped<IViewRenderService, ViewRenderService>();

var app = builder.Build();

// ================= MIDDLEWARE =================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ⚠️ Session PHẢI đặt trước Authorization
app.UseSession();

app.UseAuthorization();

// ================= ROUTING =================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
