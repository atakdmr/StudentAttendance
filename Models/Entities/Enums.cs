using System;

namespace Yoklama.Models.Entities
{
    // Kullanıcı rolleri
    public enum UserRole
    {
        Admin = 1,
        Teacher = 2
    }

    // Yoklama durumu
    public enum AttendanceStatus
    {
        Present = 1,
        Absent = 2,
        Late = 3,
        Excused = 4
    }

    // Oturum durumu
    public enum SessionStatus
    {
        Open = 1,
        Closed = 2,
        Finalized = 3
    }
}
