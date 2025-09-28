using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Yoklama.Models.Entities;

namespace Yoklama.Data
{
    public static class SeedData
    {
        public static async Task EnsureSeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var sp = scope.ServiceProvider;
            var db = sp.GetRequiredService<AppDbContext>();

            // Apply pending migrations
            await db.Database.MigrateAsync();

            // Users
            if (!await db.Users.AnyAsync())
            {
                var hasher = new PasswordHasher<User>();

                var admin = new User
                {
                    UserName = "admin",
                    FullName = "Sistem Yöneticisi",
                    Role = UserRole.Admin,
                    IsActive = true
                };
                admin.PasswordHash = hasher.HashPassword(admin, "Admin@12345");
                admin.RowVersion = new byte[8];

                var teacher1 = new User
                {
                    UserName = "teacher1",
                    FullName = "Ahmet Öğretmen",
                    Role = UserRole.Teacher,
                    IsActive = true
                };
                teacher1.PasswordHash = hasher.HashPassword(teacher1, "Teacher@123");
                teacher1.RowVersion = new byte[8];

                var teacher2 = new User
                {
                    UserName = "teacher2",
                    FullName = "Ayşe Öğretmen",
                    Role = UserRole.Teacher,
                    IsActive = true
                };
                teacher2.PasswordHash = hasher.HashPassword(teacher2, "Teacher@123");
                teacher2.RowVersion = new byte[8];

                await db.Users.AddRangeAsync(admin, teacher1, teacher2);
                await db.SaveChangesAsync();

                // Groups - Sadece 3 grup
                var group8A = new Group { Name = "8/A", Code = "8A", Description = "8. Sınıf A Şubesi" };
                var group8B = new Group { Name = "8/B", Code = "8B", Description = "8. Sınıf B Şubesi" };
                var group8C = new Group { Name = "8/C", Code = "8C", Description = "8. Sınıf C Şubesi" };
                await db.Groups.AddRangeAsync(group8A, group8B, group8C);
                await db.SaveChangesAsync();

                // Students - Toplam 10 öğrenci
                var students = new[]
                {
                    // 8/A - 4 öğrenci
                    new Student { FirstName = "Ali", LastName = "Yılmaz", StudentNumber = "8A01", GroupId = group8A.Id, IsActive = true },
                    new Student { FirstName = "Ayşe", LastName = "Demir", StudentNumber = "8A02", GroupId = group8A.Id, IsActive = true },
                    new Student { FirstName = "Mehmet", LastName = "Kaya", StudentNumber = "8A03", GroupId = group8A.Id, IsActive = true },
                    new Student { FirstName = "Fatma", LastName = "Şahin", StudentNumber = "8A04", GroupId = group8A.Id, IsActive = true },
                    
                    // 8/B - 3 öğrenci
                    new Student { FirstName = "Ahmet", LastName = "Çelik", StudentNumber = "8B01", GroupId = group8B.Id, IsActive = true },
                    new Student { FirstName = "Zeynep", LastName = "Özkan", StudentNumber = "8B02", GroupId = group8B.Id, IsActive = true },
                    new Student { FirstName = "Mustafa", LastName = "Aydın", StudentNumber = "8B03", GroupId = group8B.Id, IsActive = true },
                    
                    // 8/C - 3 öğrenci
                    new Student { FirstName = "Elif", LastName = "Arslan", StudentNumber = "8C01", GroupId = group8C.Id, IsActive = true },
                    new Student { FirstName = "Can", LastName = "Yıldız", StudentNumber = "8C02", GroupId = group8C.Id, IsActive = true },
                    new Student { FirstName = "Selin", LastName = "Kara", StudentNumber = "8C03", GroupId = group8C.Id, IsActive = true },
                };

                await db.Students.AddRangeAsync(students);
                await db.SaveChangesAsync();

                // Lessons - Çakışmasız ders programı
                var lessons = new[]
                {
                    // Teacher1 - Matematik dersleri
                    new Lesson { GroupId = group8A.Id, Title = "Matematik", DayOfWeek = 1, StartTime = TimeSpan.Parse("08:00"), EndTime = TimeSpan.Parse("08:45"), TeacherId = teacher1.Id, IsActive = true },
                    new Lesson { GroupId = group8B.Id, Title = "Matematik", DayOfWeek = 1, StartTime = TimeSpan.Parse("10:00"), EndTime = TimeSpan.Parse("10:45"), TeacherId = teacher1.Id, IsActive = true },
                    new Lesson { GroupId = group8C.Id, Title = "Matematik", DayOfWeek = 1, StartTime = TimeSpan.Parse("13:00"), EndTime = TimeSpan.Parse("13:45"), TeacherId = teacher1.Id, IsActive = true },

                    // Teacher2 - Türkçe dersleri
                    new Lesson { GroupId = group8A.Id, Title = "Türkçe", DayOfWeek = 2, StartTime = TimeSpan.Parse("08:00"), EndTime = TimeSpan.Parse("08:45"), TeacherId = teacher2.Id, IsActive = true },
                    new Lesson { GroupId = group8B.Id, Title = "Türkçe", DayOfWeek = 2, StartTime = TimeSpan.Parse("10:00"), EndTime = TimeSpan.Parse("10:45"), TeacherId = teacher2.Id, IsActive = true },
                    new Lesson { GroupId = group8C.Id, Title = "Türkçe", DayOfWeek = 2, StartTime = TimeSpan.Parse("13:00"), EndTime = TimeSpan.Parse("13:45"), TeacherId = teacher2.Id, IsActive = true },

                    // Teacher1 - Fen Bilimleri dersleri
                    new Lesson { GroupId = group8A.Id, Title = "Fen Bilimleri", DayOfWeek = 3, StartTime = TimeSpan.Parse("08:00"), EndTime = TimeSpan.Parse("08:45"), TeacherId = teacher1.Id, IsActive = true },
                    new Lesson { GroupId = group8B.Id, Title = "Fen Bilimleri", DayOfWeek = 3, StartTime = TimeSpan.Parse("10:00"), EndTime = TimeSpan.Parse("10:45"), TeacherId = teacher1.Id, IsActive = true },
                    new Lesson { GroupId = group8C.Id, Title = "Fen Bilimleri", DayOfWeek = 3, StartTime = TimeSpan.Parse("13:00"), EndTime = TimeSpan.Parse("13:45"), TeacherId = teacher1.Id, IsActive = true },

                    // Teacher2 - Sosyal Bilgiler dersleri
                    new Lesson { GroupId = group8A.Id, Title = "Sosyal Bilgiler", DayOfWeek = 4, StartTime = TimeSpan.Parse("08:00"), EndTime = TimeSpan.Parse("08:45"), TeacherId = teacher2.Id, IsActive = true },
                    new Lesson { GroupId = group8B.Id, Title = "Sosyal Bilgiler", DayOfWeek = 4, StartTime = TimeSpan.Parse("10:00"), EndTime = TimeSpan.Parse("10:45"), TeacherId = teacher2.Id, IsActive = true },
                    new Lesson { GroupId = group8C.Id, Title = "Sosyal Bilgiler", DayOfWeek = 4, StartTime = TimeSpan.Parse("13:00"), EndTime = TimeSpan.Parse("13:45"), TeacherId = teacher2.Id, IsActive = true },

                    // Teacher1 - İngilizce dersleri
                    new Lesson { GroupId = group8A.Id, Title = "İngilizce", DayOfWeek = 5, StartTime = TimeSpan.Parse("08:00"), EndTime = TimeSpan.Parse("08:45"), TeacherId = teacher1.Id, IsActive = true },
                    new Lesson { GroupId = group8B.Id, Title = "İngilizce", DayOfWeek = 5, StartTime = TimeSpan.Parse("10:00"), EndTime = TimeSpan.Parse("10:45"), TeacherId = teacher1.Id, IsActive = true },
                    new Lesson { GroupId = group8C.Id, Title = "İngilizce", DayOfWeek = 5, StartTime = TimeSpan.Parse("13:00"), EndTime = TimeSpan.Parse("13:45"), TeacherId = teacher1.Id, IsActive = true },
                };

                await db.Lessons.AddRangeAsync(lessons);
                await db.SaveChangesAsync();
            }
        }
    }
}
