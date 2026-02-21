using IFixZoneWeb.Models.Entities;
using IFixZoneWeb.Helpers;
using Microsoft.AspNetCore.Authentication.Cookies;
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

// ================= AUTHENTICATION (COOKIE) =================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";          // Khi CHƯA login
        options.AccessDeniedPath = "/Account/AccessDenied"; // Khi KHÔNG đủ quyền
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

// ================= AUTHORIZATION =================
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("StaffOnly", policy =>
        policy.RequireRole("Staff", "Admin"));
});

// ================= SESSION (CHO MINI CART) =================
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

// ⚠️ THỨ TỰ BẮT BUỘC – KHÔNG ĐƯỢC ĐẢO
app.UseSession();           // 1️⃣ Session
app.UseAuthentication();    // 2️⃣ Authentication
app.UseAuthorization();     // 3️⃣ Authorization

// ================= ROUTING =================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
