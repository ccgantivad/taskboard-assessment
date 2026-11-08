using Microsoft.EntityFrameworkCore;
using TaskBoard.Api.Models;

namespace TaskBoard.Api.Data;

public sealed class TaskBoardDbContext(DbContextOptions<TaskBoardDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

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

        var task = modelBuilder.Entity<TaskItem>();
        task.ToTable("Tasks", "dbo", table =>
        {
            table.HasCheckConstraint("CK_Tasks_Title_NotBlank", "LEN(LTRIM(RTRIM(Title))) > 0");
            table.HasCheckConstraint("CK_Tasks_Status", "Status COLLATE Latin1_General_100_BIN2 IN (N'Pending', N'InProgress', N'Done')");
            table.HasCheckConstraint("CK_Tasks_AdditionalData_Json", "ISJSON(AdditionalData, OBJECT) = 1");
        });
        task.HasKey(x => x.Id).HasName("PK_Tasks");
        task.Property(x => x.Id).UseIdentityColumn();
        task.Property(x => x.Title).HasMaxLength(200).IsRequired();
        task.Property(x => x.Status).HasMaxLength(20).IsRequired()
            .HasDefaultValue(TaskStatuses.Pending).IsConcurrencyToken();
        task.Property(x => x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()")
            .HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        task.Property(x => x.AdditionalData).HasColumnType("nvarchar(max)").IsRequired().HasDefaultValue("{}");
        task.HasOne(x => x.AssignedUser).WithMany().HasForeignKey(x => x.AssignedUserId)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Tasks_Users");
        task.HasIndex(x => new { x.AssignedUserId, x.Status, x.CreatedAt })
            .IsDescending(false, false, true).HasDatabaseName("IX_Tasks_AssignedUserId_Status_CreatedAt");
        modelBuilder.HasDbFunction(typeof(SqlJson).GetMethod(nameof(SqlJson.Value))!)
            .HasName("JSON_VALUE").IsBuiltIn();
    }
}
