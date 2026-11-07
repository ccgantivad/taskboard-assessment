using Microsoft.EntityFrameworkCore;
using TaskBoard.Api.Models;

namespace TaskBoard.Api.Data;

public sealed class TaskBoardDbContext(DbContextOptions<TaskBoardDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<User>();
        user.ToTable("Users", "dbo", table =>
        {
            table.HasCheckConstraint("CK_Users_Name_NotBlank", "LEN(LTRIM(RTRIM(Name))) > 0");
            table.HasCheckConstraint("CK_Users_Email_NotBlank", "LEN(LTRIM(RTRIM(Email))) > 0");
        });
        user.HasKey(x => x.Id).HasName("PK_Users");
        user.Property(x => x.Id).UseIdentityColumn();
        user.Property(x => x.Name).HasMaxLength(120).IsRequired();
        user.Property(x => x.Email).HasMaxLength(254).IsRequired();
    }
}
