using JobTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<StatusChange> StatusChanges => Set<StatusChange>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(c => c.Name);
        });

        modelBuilder.Entity<JobApplication>(e =>
        {
            e.Property(a => a.RoleTitle).HasMaxLength(200).IsRequired();
            e.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Note>(e =>
        {
            e.Property(n => n.Text).HasMaxLength(2000).IsRequired();
        });

        modelBuilder.Entity<StatusChange>(e =>
        {
            e.Property(s => s.FromStatus).HasConversion<string>().HasMaxLength(20);
            e.Property(s => s.ToStatus).HasConversion<string>().HasMaxLength(20);
        });
    }
}