using System.Globalization;
using KastamonuMeetingRoom.Data;
using KastamonuMeetingRoom.Models;
using MeetingRoomProject.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Veritabanı
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 🔹 Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 🔹 Cookie Güvenliği
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
});

// 🔹 Email
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();

// 🔹 reCAPTCHA servisi
builder.Services.AddSingleton<ReCaptchaService>();

// 🔹 MVC + Razor
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// ⭐ ⭐ **SESSION AKTİF (Manuel CAPTCHA için şart)** ⭐ ⭐
builder.Services.AddSession();

// 🔹 Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "global",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 50,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
});

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

// 🔹 Dil Ayarı
var cultureInfo = new CultureInfo("tr-TR");
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(cultureInfo),
    SupportedCultures = new List<CultureInfo> { cultureInfo },
    SupportedUICultures = new List<CultureInfo> { cultureInfo }
};
app.UseRequestLocalization(localizationOptions);

CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// 🔹 HTTPS + HSTS
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// 🔥 IDENTITY (LOGIN / LOGOUT İÇİN ŞART)
app.MapRazorPages();

// 🔥 ADMIN AREA ROUTE
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}"
);

// 🔥 DEFAULT ROUTE
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// 🌟 ADMIN ROL VE KULLANICI
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = { "Admin", "User" };

    foreach (var role in roles)
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));

    string adminEmail = "admin@kastamonu.com";
    string adminPassword = "Admin123!";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FullName = "Sistem Yöneticisi"
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);

        if (result.Succeeded)
            await userManager.AddToRoleAsync(adminUser, "Admin");
        else
            foreach (var error in result.Errors)
                Console.WriteLine($"Admin oluşturulamadı: {error.Description}");
    }
}

app.Run();
