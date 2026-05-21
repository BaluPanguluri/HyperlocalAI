using HyperlocalAI.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace HyperlocalAI.Infrastructure.Persistence;

public sealed class LocalMindDbContext : DbContext
{
    public LocalMindDbContext(DbContextOptions<LocalMindDbContext> options)
        : base(options)
    {
    }

    public DbSet<LeadRecord> Leads => Set<LeadRecord>();
    public DbSet<AppointmentRecord> Appointments => Set<AppointmentRecord>();
    public DbSet<DailyMetricsRecord> DailyMetrics => Set<DailyMetricsRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var lead = modelBuilder.Entity<LeadRecord>();
        lead.ToTable("leads");
        lead.HasKey(e => e.Id);
        lead.Property(e => e.Channel).HasMaxLength(32);
        lead.Property(e => e.ContactKey).HasMaxLength(256);
        lead.Property(e => e.DisplayName).HasMaxLength(256);
        lead.Property(e => e.Email).HasMaxLength(256);
        lead.Property(e => e.PhoneE164).HasMaxLength(32);
        lead.Property(e => e.Source).HasMaxLength(64);
        lead.Property(e => e.Notes).HasMaxLength(4000);
        lead.Property(e => e.LostReason).HasMaxLength(500);
        lead.HasIndex(e => new { e.Vertical, e.ContactKey }).IsUnique();

        var appt = modelBuilder.Entity<AppointmentRecord>();
        appt.ToTable("appointments");
        appt.HasKey(e => e.Id);
        appt.Property(e => e.Channel).HasMaxLength(32);
        appt.Property(e => e.CustomerName).HasMaxLength(256);
        appt.Property(e => e.Phone).HasMaxLength(64);
        appt.Property(e => e.Notes).HasMaxLength(4000);
        appt.HasIndex(e => new { e.Vertical, e.StartUtc });
        appt.HasOne(e => e.Lead)
            .WithMany(l => l.Appointments)
            .HasForeignKey(e => e.LeadId)
            .OnDelete(DeleteBehavior.SetNull);

        var daily = modelBuilder.Entity<DailyMetricsRecord>();
        daily.ToTable("daily_metrics");
        daily.HasKey(e => new { e.Vertical, e.DayUtc });
    }
}
