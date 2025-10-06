using System;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Yoklama.Data
{
    // Design-time factory so EF tools can create the context without running Program.cs startup code
    public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));
            // Dummy connection string; EF doesn't connect during migration scaffolding
            var connectionString = "server=localhost;port=3306;database=design_time_db;user=root;password=root123;TreatTinyAsBoolean=true;Allow User Variables=True;";
            optionsBuilder.UseMySql(connectionString, serverVersion);

            return new AppDbContext(optionsBuilder.Options, new HttpContextAccessor());
        }
    }
}


