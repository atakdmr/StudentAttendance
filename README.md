# 📚 Yoklama Sistemi

Modern ve kullanıcı dostu ASP.NET Core MVC tabanlı yoklama yönetim sistemi.

## 🚀 Özellikler

### 👥 Kullanıcı Yönetimi
- **Admin/Teacher** rolleri ile yetkilendirme
- Güvenli cookie-based authentication
- Kullanıcı CRUD işlemleri
- Şifre hash'leme sistemi

### 📚 Ders Yönetimi
- Ders oluşturma, düzenleme ve silme
- Öğretmen atama sistemi
- Haftalık ders programı görünümü
- Gelişmiş filtreleme (Grup, Öğretmen, Ders adı)
- Case-insensitive arama

### 👨‍🎓 Öğrenci Yönetimi
- Öğrenci CRUD işlemleri
- Grup atama sistemi
- Benzersiz öğrenci numarası

### 📊 Yoklama Sistemi
- Oturum açma/kapama
- Devam durumu kaydetme
- Otomatik tarih hesaplama
- Durum yönetimi (Open/Closed/Finalized)

### 📈 Raporlama
- Öğrenci devam raporları
- Grup bazında raporlar
- CSV export özelliği
- Tarih filtreleme
- Ders detay modal'ı

### 📅 Ders Programı
- Modern grid layout
- 7 günlük görünüm
- Responsive tasarım
- Filtreleme sistemi
- "Kendi dersleriniz" vurgulama

## 🛠️ Teknoloji Stack

- **Backend**: ASP.NET Core 9.0 MVC
- **Veritabanı**: SQLite (Entity Framework Core)
- **Frontend**: Bootstrap 5, jQuery, Font Awesome
- **Kimlik Doğrulama**: Cookie Authentication
- **SMS Entegrasyonu**: NetGSM API
- **Güvenlik**: HTTPS, XSS Protection, CSRF Protection

## 📦 Kurulum

### Gereksinimler
- .NET 9.0 SDK
- Visual Studio 2022 veya VS Code

### Adımlar

1. **Repository'yi klonlayın**
```bash
git clone https://github.com/kullaniciadi/yoklama-projesi.git
cd yoklama-projesi
```

2. **Paketleri yükleyin**
```bash
dotnet restore
```

3. **Veritabanını oluşturun**
```bash
dotnet ef database update
```

4. **Uygulamayı çalıştırın**
```bash
dotnet run
```

5. **Tarayıcıda açın**
```
https://localhost:5001
```

## 🔧 Yapılandırma

### SMS Ayarları
`appsettings.json` dosyasında NetGSM API bilgilerini doldurun:

```json
{
  "SmsSettings": {
    "Username": "your_username",
    "Password": "your_password",
    "ApiUrl": "https://api.netgsm.com.tr/sms/send/get"
  }
}
```

### Production Ayarları
Production ortamı için `appsettings.Production.json` dosyasını düzenleyin.

## 📱 Kullanım

### Admin Kullanıcısı
- Tüm kullanıcıları yönetebilir
- Grup ve öğrenci oluşturabilir
- Tüm dersleri görebilir ve yönetebilir
- Raporları görüntüleyebilir

### Teacher Kullanıcısı
- Sadece kendi derslerini görebilir
- Yoklama alabilir
- Öğrenci devam durumlarını kaydedebilir

## 🔒 Güvenlik

- HTTPS zorunluluğu
- Cookie güvenlik ayarları
- XSS ve CSRF koruması
- Role-based authorization
- Audit logging sistemi

## 📊 Veritabanı Yapısı

- **Users**: Kullanıcı bilgileri
- **Groups**: Grup bilgileri
- **Students**: Öğrenci bilgileri
- **Lessons**: Ders bilgileri
- **AttendanceSessions**: Yoklama oturumları
- **AttendanceRecords**: Devam kayıtları
- **AuditLogs**: Sistem logları

## 🚀 Deployment

### Self-contained Publish
```bash
dotnet publish --configuration Release --output ./publish --self-contained true --runtime win-x64
```

### Docker (Opsiyonel)
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
COPY . /app
WORKDIR /app
EXPOSE 80
ENTRYPOINT ["dotnet", "Yoklama.dll"]
```

## 🤝 Katkıda Bulunma

1. Fork yapın
2. Feature branch oluşturun (`git checkout -b feature/AmazingFeature`)
3. Commit yapın (`git commit -m 'Add some AmazingFeature'`)
4. Push yapın (`git push origin feature/AmazingFeature`)
5. Pull Request oluşturun

## 📄 Lisans

Bu proje MIT lisansı altında lisanslanmıştır.

## 👨‍💻 Geliştirici

Bu proje modern web teknolojileri kullanılarak geliştirilmiştir.

## 📞 İletişim

Sorularınız için issue açabilirsiniz.

---

**Not**: Bu sistem eğitim kurumları için tasarlanmıştır ve production ortamında kullanıma hazırdır.
