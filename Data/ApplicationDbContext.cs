using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PalliativeCare.Models;

namespace PalliativeCare.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Patient> Patients => Set<Patient>();
        public DbSet<Doctor> Doctors => Set<Doctor>();
        public DbSet<Appointment> Appointments => Set<Appointment>();
        public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
        public DbSet<Reminder> Reminders => Set<Reminder>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<PpsAssessment> PpsAssessments => Set<PpsAssessment>();
        public DbSet<FamilyMember> FamilyMembers => Set<FamilyMember>();
        public DbSet<FamilyMessage> FamilyMessages => Set<FamilyMessage>();
        public DbSet<CareUpdate> CareUpdates => Set<CareUpdate>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Patient → Doctor (many patients to one doctor)
            builder.Entity<Patient>()
                .HasOne(p => p.Doctor)
                .WithMany(d => d.Patients)
                .HasForeignKey(p => p.DoctorId)
                .OnDelete(DeleteBehavior.SetNull);

            // Appointment → Patient
            builder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Appointment → Doctor
            builder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // MedicalRecord → Patient
            builder.Entity<MedicalRecord>()
                .HasOne(m => m.Patient)
                .WithMany(p => p.MedicalRecords)
                .HasForeignKey(m => m.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // MedicalRecord → Doctor
            builder.Entity<MedicalRecord>()
                .HasOne(m => m.Doctor)
                .WithMany(d => d.MedicalRecords)
                .HasForeignKey(m => m.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            // Extended models (PPS, Family Portal)
            DbContextExtensions.ConfigureExtendedModels(builder);

            builder.Entity<Patient>().HasIndex(p => p.Phone);
            builder.Entity<Doctor>().HasIndex(d => d.Email).IsUnique();
            builder.Entity<Appointment>().HasIndex(a => a.ScheduledAt);
            builder.Entity<Reminder>().HasIndex(r => r.DueAt);

            var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // Seed Doctors
            builder.Entity<Doctor>().HasData(
                new Doctor { Id = 1, FirstName = "Sarah", LastName = "Williams", Specialisation = "Palliative Medicine", Phone = "07700900001", Email = "s.williams@palliativecare.nhs.uk", LicenceNumber = "GMC-001234", Role = DoctorRole.Doctor, IsAvailable = true, JoinedAt = seedDate },
                new Doctor { Id = 2, FirstName = "James", LastName = "Patel", Specialisation = "Pain Management", Phone = "07700900002", Email = "j.patel@palliativecare.nhs.uk", LicenceNumber = "GMC-005678", Role = DoctorRole.Doctor, IsAvailable = true, JoinedAt = seedDate },
                new Doctor { Id = 3, FirstName = "Emma", LastName = "Clarke", Specialisation = "Palliative Nursing", Phone = "07700900003", Email = "e.clarke@palliativecare.nhs.uk", Role = DoctorRole.Nurse, IsAvailable = true, JoinedAt = seedDate }
            );

            // Seed Patients
            builder.Entity<Patient>().HasData(
                new Patient { Id = 1, FirstName = "John", LastName = "Smith", DateOfBirth = new DateTime(1945, 3, 12), Gender = "Male", Phone = "07700100001", Email = "john.smith@email.com", Address = "12 Oak Street, London", Diagnosis = "Advanced COPD", Status = PatientStatus.Active, DoctorId = 1, RegisteredAt = seedDate, UpdatedAt = seedDate, EmergencyContactName = "Mary Smith", EmergencyContactPhone = "07700100002", EmergencyContactRelation = "Spouse" },
                new Patient { Id = 2, FirstName = "Mary", LastName = "Johnson", DateOfBirth = new DateTime(1952, 7, 22), Gender = "Female", Phone = "07700200001", Email = "mary.j@email.com", Address = "45 Elm Road, Manchester", Diagnosis = "Stage 4 Breast Cancer", Status = PatientStatus.Active, DoctorId = 2, RegisteredAt = seedDate, UpdatedAt = seedDate }
            );
        }
    }

    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? Role { get; set; }
    }
}
