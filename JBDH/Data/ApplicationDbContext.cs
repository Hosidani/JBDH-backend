using Microsoft.EntityFrameworkCore; // For DbContext, DbSet, etc.
using JBDH.Models; 
public class MyDbContext : DbContext
{
    public DbSet<MyEntity> MyEntities { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseMySQL("Server=127.0.0.1;Database=testing_purposes;User ID=root;Password=ArianAAA123;Port=3306;");
    }
}