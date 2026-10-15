<div align="center">

# 🏛️ Kastamonu Üniversitesi — Toplantı & Ajanda Yönetim Sistemi
### *KastamonuUni-ToplantiAjanda*

[![.NET Core](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-MVC-512BD4?style=for-the-badge&logo=aspnetcore&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet)
[![SignalR](https://img.shields.io/badge/Real--Time-SignalR-F04D23?style=for-the-badge&logo=signalr&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/signalr)
[![Entity Framework](https://img.shields.io/badge/EF_Core-SQL_Server-blue?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)](https://learn.microsoft.com/ef/core/)
[![Bootstrap](https://img.shields.io/badge/Bootstrap-5.3-7952B3?style=for-the-badge&logo=bootstrap&logoColor=white)](https://getbootstrap.com/)
[![License](https://img.shields.io/badge/License-MIT-green.style?style=for-the-badge)](LICENSE)

<br/>

**Kastamonu Üniversitesi Toplantı ve Ajanda Yönetim Sistemi**, üniversite bünyesindeki rektörlük, fakülteler ve enstitülere ait salonların rezervasyonlarını, akademik ajandayı, anlık salon doluluk durumlarını ve kapı tablet ekranlarını (Kiosk) tek merkezden yönetmek üzere geliştirilmiş yeni nesil, gerçek zamanlı (real-time) kurumsal web platformudur.

[Özellikler](#-öne-çıkan-özellikler) • [Teknolojiler](#-teknoloji-yığını) • [Kurulum](#-hızlı-kurulum) • [Kiosk Kapı Ekranı](#-salon-kapı-ekranı--kiosk-modu) • [Ekran Görüntüleri](#-ekran-görüntüleri) • [İletişim](#-geliştirici--iletişim)

</div>

---

## 🌟 Öne Çıkan Özellikler

### 🏛️ 1. Kurumsal Kimlik & Modern Kullanıcı Arayüzü (UI/UX)
- **Kastamonu Üniversitesi Renk Paleti:** Kurumsal Bordo-Kırmızı (`#B22525`), Asil Siyah (`#111111`) ve Beyaz (`#FFFFFF`) temalı arayüz.
- **🌓 Koyu Mod / Açık Mod (Dark / Light Theme Toggle):** Sağ üstteki güneş/ay düğmesiyle tüm takvim, kartlar, tablolar ve modallar anında koyu antrasit-siyah gece moduna geçer. Tercih `localStorage` üzerinde saklanır.
- **✨ Mikro Etkileşimler & Toast Bildirimleri:** Rezervasyon işlemleri, silmeler ve güncellemeler SweetAlert2 ve modern toast animasyonları ile desteklenmiştir.

### 🟢 2. Canlı Salon Durum Takibi (Pulse Badges)
- **🟢 Şu An Müsait:** O an herhangi bir toplantısı bulunmayan salonlar yeşil nabız (pulse) animasyonu ile gösterilir, hemen rezervasyon oluşturulabilir.
- **🔴 Toplantı Sürüyor:** Salonda aktif toplantı varsa kırmızı canlı nabız yanar, toplantının adı ve kalan süresi (*Örn: Kalan Süre: 28 dk — Senato Kurulu*) anlık hesaplanır.

### 🔄 3. Gerçek Zamanlı Takvim & Ajanda (ASP.NET Core SignalR)
- **Canlı Senkronizasyon (`CalendarHub`):** Bir akademisyen veya yönetici rezervasyon eklediğinde, saat değiştirdiğinde veya iptal ettiğinde, **sayfa yenilenmeksizin** tüm istemcilerdeki FullCalendar takvimi anında güncellenir.
- **🖱️ Sürükle-Bırak ile Saat Güncelleme (FullCalendar Drag & Drop):** Yetkili kullanıcı takvim üzerinde toplantıyı tutarak yeni güne/saate taşıyabilir veya kenarından tutup süresini uzatabilir. Arka planda çakışma kontrolü anlık doğrulanır.
- **⚡ 5 Saniyede Hızlı Rezervasyon:** Takvimde boş bir zaman dilimi seçildiğinde sayfa değiştirmeden doğrudan Bootstrap Modal açılır ve hızlıca kayıt yapılabilir.

### 📺 4. Salon Kapı Ekranı / Tablet Kiosk Modu (`/Room/DoorDisplay/{id}`)
- Üniversitedeki salon kapılarına asılan tabletler ve ekranlar için özel tam ekran görünüm.
- Büyük dijital saat, salon doluluk durumuna göre kırmızı/yeşil canlı arka plan parlaması.
- "Şu Anki Toplantı", toplantı ilerleme çubuğu, "Sıradaki Toplantı" bilgisi.
- Kapıdan geçerken doğrudan mobil telefondan o salonun güncel programına erişmek için **Dinamik QR Kod**.

### 🎓 5. Akademik & İdari Yönetim Fonksiyonları
- **Toplantı Türleri (Kategorilendirme):** 
  - 🎓 *Senato Toplantısı*
  - 🏛️ *Fakülte / Bölüm Akademik Kurulu*
  - 🔬 *Yüksek Lisans / Doktora Tez Savunması*
  - 📑 *BAP Proje Toplantısı*
  - 👥 *Öğrenci Kulübü Etkinliği*
  - 🎤 *Seminer / Konferans*
  - Takvimde tür bazlı renk kodlaması ve filtreleme.
- **🏛️ Fakülte ve Birim Filtrelemesi:** Rektörlük, Mühendislik-Mimarlık Fakültesi, İİBF vb. birimlere göre anlık filtreleme.
- **🛠️ Salon Donanım Künyesi:** 📽️ 4K Projeksiyon, 📹 Video Konferans / Zoom, 💻 Akıllı Tahta, 🎙️ Ses Sistemi, ☕ İkram Alanı, ♿ Engelli Erişimi.
- **📅 Ajanda Entegrasyonu:** Apple Calendar / Google Calendar / Outlook için `.ics` indirme ve tek tıkla doğrudan Google Calendar'a ekleme linkleri.
- **📄 Resmi Çağrı & Tutanak Çıktısı:** Toplantı detayında resmi üniversite toplantı katılım tutanağı formatı ve tek tıkla yazdırma (`window.print()`).

---

## 🛠️ Teknoloji Yığını

| Katman | Teknoloji | Açıklama |
| :--- | :--- | :--- |
| **Framework** | .NET 8.0 / C# 12 | Yüksek performanslı LTS kurumsal altyapı |
| **Mimari** | ASP.NET Core MVC | Model-View-Controller + Areas (Admin) |
| **Veritabanı / ORM** | Entity Framework Core 8/9 + MS SQL Server | Code-First, Connection Resiliency & Otomatik Kolon Senkronizasyonu |
| **Gerçek Zamanlı (Real-Time)** | ASP.NET Core SignalR | Canlı ajanda ve takvim güncelleme |
| **Kimlik & Yetkilendirme** | ASP.NET Core Identity | Cookie Güvenliği, Rol Yönetimi (Admin / User) |
| **Arayüz (Frontend)** | Bootstrap 5.3 + Custom CSS3 | Üniversite renkleri, CSS Değişkenleri, Dark Mode |
| **Takvim Kütüphanesi** | FullCalendar v6 (DayGrid, TimeGrid, Interaction) | Sürükle-bırak, slot seçimi, Türkçe dil desteği |
| **Yardımcı Kütüphaneler** | SweetAlert2, FontAwesome 6, QRious.js, iCal.NET | Bildirimler, ikonlar, dinamik QR ve takvim dışa aktarma |

---

## 📁 Proje Dizin Yapısı

```bash
KastamonuUni-ToplantiAjanda/
│
├── Areas/
│   └── Admin/                     # Yönetici Paneli (Dashboard, Salon & Kullanıcı Yönetimi)
│       ├── Controllers/
│       └── Views/
│
├── Controllers/
│   ├── HomeController.cs          # Canlı durumlar, bugün ajandası ve istatistikler
│   ├── ReservationController.cs   # Rezervasyon, FullCalendar API, SignalR & ICS
│   └── RoomController.cs          # Salon listesi, filtreler, donanımlar ve Kiosk Modu
│
├── Data/
│   └── ApplicationDbContext.cs    # EF Core veri tabanı bağlamı ve tablolar
│
├── Hubs/
│   └── CalendarHub.cs             # Gerçek zamanlı SignalR istemci dağıtım merkezi
│
├── Models/
│   ├── ApplicationUser.cs         # Identity genişletilmiş kullanıcı modeli
│   ├── Reservation.cs             # Toplantı türü, katılımcı ve tarih modeli
│   ├── Room.cs                    # Salon, birim, donanım ve canlı durum modeli
│   └── HomeDashboardViewModel.cs  # Anasayfa özet paneli görünüm modeli
│
├── Services/
│   ├── CalendarIntegrationService.cs  # RFC 5545 iCalendar (.ics) ve Google Calendar servisi
│   └── SmtpEmailSender.cs             # E-posta bildirim servisi
│
├── Views/
│   ├── Home/                      # Anasayfa & Canlı Göstergeler
│   ├── Reservation/               # Takvim, Hızlı Rezervasyon, Tutanak & Detay
│   ├── Room/                      # Salon Kataloğu, Filtreleme ve DoorDisplay (Kiosk)
│   └── Shared/                    # _Layout, Navbar, Dark Mode, SignalR & Footer
│
├── wwwroot/
│   ├── css/site.css               # Kurumsal Kırmızı-Beyaz-Siyah & Dark Theme stilleri
│   └── js/site.js                 # SignalR dinleyicisi & Dark Mode yöneticisi
│
├── Program.cs                     # Bağımlılık enjeksiyonu, SignalR, Seed Data & Middleware
└── README.md                      # Proje dokümantasyonu
```

---

## 🚀 Hızlı Kurulum

### Gereksinimler
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Microsoft SQL Server](https://www.microsoft.com/sql-server/) (veya SQL Server Express / LocalDB)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) veya [Visual Studio Code](https://code.visualstudio.com/)

### 1. Repoyu Klonlayın
```bash
git clone https://github.com/YusufKoc05/KastamonuUni-ToplantiAjanda.git
cd KastamonuUni-ToplantiAjanda/KastamonuMeetingRoom/KastamonuMeetingRoom
```

### 2. Veritabanı Bağlantısını Yapılandırın
`appsettings.json` dosyasındaki SQL Server bağlantı dizesini (`DefaultConnection`) kendi bilgisayarınıza göre ayarlayın:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=MeetingRoomDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

### 3. Projeyi Çalıştırın
Uygulama ilk çalıştığında gerekli tabloları otomatik oluşturur, örnek salonları ve varsayılan yönetici hesabını sisteme yükler:

```bash
dotnet run
```
veya Visual Studio / VS Code üzerinden **F5** tuşuna basarak başlatın.

Tarayıcınızdan açın:
- **Web Arayüzü:** `https://localhost:7187` veya `http://localhost:5044`

---

## 🔑 Varsayılan Yönetici (Admin) Girişi

Sistem ayağa kalktığında otomatik tanımlanan yönetici hesabı:
- **E-Posta:** `admin@kastamonu.com`
- **Şifre:** `Admin123!`

---

## 📺 Salon Kapı Ekranı / Kiosk Modu

Her salon için kapı tablet arayüzüne şu URL rotasından doğrudan ulaşılabilir:
```text
https://localhost:7187/Room/DoorDisplay/{SalonID}
```
*Örnek:*
- **Senato Salonu Kiosk:** `/Room/DoorDisplay/1`
- **Mavi Salon Kiosk:** `/Room/DoorDisplay/2`

> **Özellik:** Kapı ekranındaki dinamik QR kod telefon ile tarandığında, doğrudan o salonun güncel program akışını mobil tarayıcıda açar.

---

## 📄 Lisans
Bu proje [MIT Lisansı](LICENSE) kapsamında lisanslanmıştır. Eğitim ve kurumsal kullanım için serbestçe özelleştirilebilir.

---

## 👨‍💻 Geliştirici & İletişim

**Yusuf Koç**  
- **GitHub:** [@YusufKoc05](https://github.com/YusufKoc05)  
- **Proje Reposu:** [KastamonuUni-ToplantiAjanda](https://github.com/YusufKoc05/KastamonuUni-ToplantiAjanda)

<div align="center">
  <sub>Kastamonu Üniversitesi Bilgi İşlem ve Akademik Birimler İçin Tasarlanmıştır.</sub>
</div>
