# 📚 Student Attendance System

Modern and user-friendly ASP.NET Core MVC based student attendance management system.

## 🚀 Features

### 👥 User Management
- **Admin/Teacher** role-based authorization
- Secure cookie-based authentication
- User CRUD operations
- Password hashing system

### 📚 Lesson Management
- Create, edit and delete lessons
- Teacher assignment system
- Weekly lesson schedule view
- Advanced filtering (Group, Teacher, Lesson name)
- Case-insensitive search

### 👨‍🎓 Student Management
- Student CRUD operations
- Group assignment system
- Unique student number

### 📊 Attendance System
- Session open/close
- Attendance status recording
- Automatic date calculation
- Status management (Open/Closed/Finalized)

### 📈 Reporting
- Student attendance reports
- Group-based reports
- CSV export feature
- Date filtering
- Lesson detail modal

### 📅 Schedule
- Modern grid layout
- 7-day view
- Responsive design
- Filtering system
- "Your lessons" highlighting

## 🛠️ Technology Stack

- **Backend**: ASP.NET Core 9.0 MVC
- **Database**: SQLite (Entity Framework Core)
- **Frontend**: Bootstrap 5, jQuery, Font Awesome
- **Authentication**: Cookie Authentication
- **SMS Integration**: NetGSM API
- **Security**: HTTPS, XSS Protection, CSRF Protection

## 📦 Installation

### Requirements
- .NET 9.0 SDK
- Visual Studio 2022 or VS Code

### Steps

1. **Clone the repository**
```bash
git clone https://github.com/atakdmr/StudentAttendance.git
cd StudentAttendance
```

2. **Install packages**
```bash
dotnet restore
```

3. **Create database**
```bash
dotnet ef database update
```

4. **Run the application**
```bash
dotnet run
```

5. **Open in browser**
```
https://localhost:5001
```

## 🔧 Configuration

### SMS Settings
Fill in NetGSM API information in `appsettings.json`:

```json
{
  "SmsSettings": {
    "Username": "your_username",
    "Password": "your_password",
    "ApiUrl": "https://api.netgsm.com.tr/sms/send/get"
  }
}
```

### Production Settings
Edit `appsettings.Production.json` for production environment.

## 📱 Usage

### Admin User
- Can manage all users
- Can create groups and students
- Can view and manage all lessons
- Can view reports

### Teacher User
- Can only see their own lessons
- Can take attendance
- Can record student attendance status

## 🔒 Security

- HTTPS requirement
- Cookie security settings
- XSS and CSRF protection
- Role-based authorization
- Audit logging system

## 📊 Database Structure

- **Users**: User information
- **Groups**: Group information
- **Students**: Student information
- **Lessons**: Lesson information
- **AttendanceSessions**: Attendance sessions
- **AttendanceRecords**: Attendance records
- **AuditLogs**: System logs

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

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License.

## 👨‍💻 Developer

This project was developed using modern web technologies.

## 📞 Contact

You can open an issue for questions.

---

**Note**: This system is designed for educational institutions and is ready for production use.
