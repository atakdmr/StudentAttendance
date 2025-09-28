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


## 📄 License

This project is licensed under the MIT License.

## 👨‍💻 Developer

This project was developed using modern web technologies.

## 📞 Contact

You can open an issue for questions.

---

**Note**: This system is designed for educational institutions and is ready for production use.
