using Microsoft.EntityFrameworkCore;
using PalliativeCare.Models;

namespace PalliativeCare.Data
{
    /// <summary>
    /// Partial extension — adds PPS, FamilyMember, FamilyMessage, CareUpdate to the DbContext.
    /// Merge these DbSets and OnModelCreating rules into ApplicationDbContext.
    /// </summary>
    public static class DbContextExtensions
    {
        /// <summary>Call this inside ApplicationDbContext.OnModelCreating after base.OnModelCreating.</summary>
        public static void ConfigureExtendedModels(ModelBuilder builder)
        {
            // ── PPS Assessment ──────────────────────────────────────────────
            builder.Entity<PpsAssessment>()
                .HasOne(p => p.Patient)
                .WithMany(pa => pa.PpsAssessments)
                .HasForeignKey(p => p.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PpsAssessment>()
                .HasOne(p => p.Doctor)
                .WithMany()
                .HasForeignKey(p => p.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PpsAssessment>()
                .HasIndex(p => new { p.PatientId, p.AssessedAt });

            // ── Family Member ───────────────────────────────────────────────
            builder.Entity<FamilyMember>()
                .HasOne(f => f.Patient)
                .WithMany(p => p.FamilyMembers)
                .HasForeignKey(f => f.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<FamilyMember>()
                .HasIndex(f => f.Email);

            // ── Family Message ──────────────────────────────────────────────
            builder.Entity<FamilyMessage>()
                .HasOne(m => m.Patient)
                .WithMany()
                .HasForeignKey(m => m.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<FamilyMessage>()
                .HasOne(m => m.FamilyMember)
                .WithMany(f => f.Messages)
                .HasForeignKey(m => m.FamilyMemberId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<FamilyMessage>()
                .HasOne(m => m.Doctor)
                .WithMany()
                .HasForeignKey(m => m.DoctorId)
                .OnDelete(DeleteBehavior.SetNull);

            // ── Care Update ─────────────────────────────────────────────────
            builder.Entity<CareUpdate>()
                .HasOne(u => u.Patient)
                .WithMany(p => p.CareUpdates)
                .HasForeignKey(u => u.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CareUpdate>()
                .HasOne(u => u.Doctor)
                .WithMany()
                .HasForeignKey(u => u.DoctorId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
