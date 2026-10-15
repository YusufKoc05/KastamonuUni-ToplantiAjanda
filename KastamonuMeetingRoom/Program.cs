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
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null
        )
    );
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

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

// 🔹 Ajanda & Takvim Entegrasyon Servisi
builder.Services.AddSingleton<KastamonuMeetingRoom.Services.CalendarIntegrationService>();

// 🔹 SignalR (Canlı Takvim & Ajanda Senkronizasyonu)
builder.Services.AddSignalR();

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

// 🔥 SIGNALR HUB (CANLI TAKVİM SENKRONİZASYONU)
app.MapHub<KastamonuMeetingRoom.Hubs.CalendarHub>("/calendarHub");

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

// 🌟 VERİTABANI MIGRATION, DİNAMİK ALANLAR & ADMIN ROL VE KULLANICI
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();

        // 🔹 Güvenli Veritabanı Kolon Eklemeleri (Hata vermeden varsa atlar)
        try
        {
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Rooms') AND name = 'Department')
                    ALTER TABLE Rooms ADD Department NVARCHAR(100) NULL DEFAULT 'Rektörlük';
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Rooms') AND name = 'HasProjector')
                    ALTER TABLE Rooms ADD HasProjector BIT NOT NULL DEFAULT 1;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Rooms') AND name = 'HasVideoConference')
                    ALTER TABLE Rooms ADD HasVideoConference BIT NOT NULL DEFAULT 0;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Rooms') AND name = 'HasSmartBoard')
                    ALTER TABLE Rooms ADD HasSmartBoard BIT NOT NULL DEFAULT 1;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Rooms') AND name = 'HasSoundSystem')
                    ALTER TABLE Rooms ADD HasSoundSystem BIT NOT NULL DEFAULT 1;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Rooms') AND name = 'HasDisabledAccess')
                    ALTER TABLE Rooms ADD HasDisabledAccess BIT NOT NULL DEFAULT 1;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Rooms') AND name = 'HasCatering')
                    ALTER TABLE Rooms ADD HasCatering BIT NOT NULL DEFAULT 0;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reservations') AND name = 'MeetingType')
                    ALTER TABLE Reservations ADD MeetingType NVARCHAR(50) NULL DEFAULT 'Genel Toplantı';
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reservations') AND name = 'Attendees')
                    ALTER TABLE Reservations ADD Attendees NVARCHAR(MAX) NULL;
            ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SQL kolon güncelleme bilgisi: {ex.Message}");
        }

        // 🔹 Örnek Kastamonu Üniversitesi Odalarını Ekle / Güncelle
        if (!await context.Rooms.AnyAsync())
        {
            context.Rooms.AddRange(
                new Room { Name = "Rektörlük Senato Salonu", Capacity = 45, Location = "Rektörlük Binası 2. Kat", Department = "Rektörlük", HasProjector = true, HasVideoConference = true, HasSmartBoard = true, HasSoundSystem = true, HasDisabledAccess = true, HasCatering = true },
                new Room { Name = "Ahmet Yesevi Konferans Salonu", Capacity = 180, Location = "Merkezi Derslikler A Blok", Department = "Merkezi Birimler", HasProjector = true, HasVideoConference = true, HasSmartBoard = true, HasSoundSystem = true, HasDisabledAccess = true, HasCatering = false },
                new Room { Name = "Mühendislik Fakültesi Toplantı Salonu", Capacity = 30, Location = "Mühendislik ve Mimarlık Fakültesi", Department = "Mühendislik ve Mimarlık Fakültesi", HasProjector = true, HasVideoConference = false, HasSmartBoard = true, HasSoundSystem = true, HasDisabledAccess = true, HasCatering = false },
                new Room { Name = "İktisadi ve İdari Bilimler Salonu", Capacity = 25, Location = "İİBF Dekanlık Katı", Department = "İktisadi ve İdari Bilimler Fakültesi", HasProjector = true, HasVideoConference = false, HasSmartBoard = false, HasSoundSystem = true, HasDisabledAccess = true, HasCatering = false },
                new Room { Name = "Bilimsel Araştırma Projeleri (BAP) Salonu", Capacity = 20, Location = "Rektörlük Ek Bina", Department = "Rektörlük", HasProjector = true, HasVideoConference = true, HasSmartBoard = true, HasSoundSystem = false, HasDisabledAccess = true, HasCatering = false }
            );
            await context.SaveChangesAsync();
        }

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

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
    catch (Exception ex)
    {
        Console.WriteLine($"Veritabanı başlatma hatası: {ex.Message}");
    }
}

app.Run();
