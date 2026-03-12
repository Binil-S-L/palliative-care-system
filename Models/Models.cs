using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PalliativeCare.Models
{
    // ── Patient ──────────────────────────────────────────────────────────────
    public class Patient
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        public string FullName => $"{FirstName} {LastName}";

        [Required, DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        public int Age => (int)((DateTime.Today - DateOfBirth).TotalDays / 365.25);

        [Required]
        public string Gender { get; set; } = string.Empty;

        [Required, Phone]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        public string Address { get; set; } = string.Empty;

        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? EmergencyContactRelation { get; set; }

        [Required]
        public string Diagnosis { get; set; } = string.Empty;

        public string? AllergiesAndMedications { get; set; }
        public string? Notes { get; set; }

        public PatientStatus Status { get; set; } = PatientStatus.Active;

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Assigned Doctor
        public int? DoctorId { get; set; }
        public Doctor? Doctor { get; set; }

        // Navigation
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
        public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
        public ICollection<PpsAssessment> PpsAssessments { get; set; } = new List<PpsAssessment>();
        public ICollection<FamilyMember> FamilyMembers { get; set; } = new List<FamilyMember>();
        public ICollection<CareUpdate> CareUpdates { get; set; } = new List<CareUpdate>();
    }

    public enum PatientStatus
    {
        Active,
        Inactive,
        Discharged,
        Deceased
    }

    // ── Doctor / Staff ────────────────────────────────────────────────────────
    public class Doctor
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        public string FullName => $"Dr. {FirstName} {LastName}";

        [Required]
        public string Specialisation { get; set; } = string.Empty;

        [Required, Phone]
        public string Phone { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? LicenceNumber { get; set; }
        public DoctorRole Role { get; set; } = DoctorRole.Doctor;
        public bool IsAvailable { get; set; } = true;

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Patient> Patients { get; set; } = new List<Patient>();
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
    }

    public enum DoctorRole
    {
        Doctor,
        Nurse,
        Caregiver,
        SocialWorker,
        Chaplain,
        Admin
    }

    // ── Appointment ───────────────────────────────────────────────────────────
    public class Appointment
    {
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }
        public Patient? Patient { get; set; }

        [Required]
        public int DoctorId { get; set; }
        public Doctor? Doctor { get; set; }

        [Required]
        public DateTime ScheduledAt { get; set; }

        public int DurationMinutes { get; set; } = 30;

        [Required]
        public string Purpose { get; set; } = string.Empty;

        public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
        public AppointmentType Type { get; set; } = AppointmentType.InPerson;

        public string? Notes { get; set; }
        public string? Location { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool ReminderSent { get; set; } = false;
    }

    public enum AppointmentStatus
    {
        Scheduled,
        Confirmed,
        Completed,
        Cancelled,
        NoShow
    }

    public enum AppointmentType
    {
        InPerson,
        HomeVisit,
        Teleconsult,
        Emergency
    }

    // ── Medical Record ────────────────────────────────────────────────────────
    public class MedicalRecord
    {
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }
        public Patient? Patient { get; set; }

        [Required]
        public int DoctorId { get; set; }
        public Doctor? Doctor { get; set; }

        [Required]
        public DateTime RecordDate { get; set; } = DateTime.UtcNow;

        [Required]
        public string ChiefComplaint { get; set; } = string.Empty;

        public string? VitalSigns { get; set; }       // JSON or plain text
        public string? PainScore { get; set; }         // 0-10 scale
        public string? Assessment { get; set; }
        public string? TreatmentPlan { get; set; }
        public string? Medications { get; set; }
        public string? FollowUpNotes { get; set; }

        public RecordType Type { get; set; } = RecordType.Consultation;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum RecordType
    {
        Consultation,
        HomeVisit,
        Emergency,
        FollowUp,
        Discharge
    }

    // ── Reminder ──────────────────────────────────────────────────────────────
    public class Reminder
    {
        public int Id { get; set; }

        public int? PatientId { get; set; }
        public Patient? Patient { get; set; }

        public int? DoctorId { get; set; }
        public Doctor? Doctor { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Message { get; set; }

        [Required]
        public DateTime DueAt { get; set; }

        public ReminderType Type { get; set; } = ReminderType.Appointment;
        public ReminderStatus Status { get; set; } = ReminderStatus.Pending;

        public bool IsRecurring { get; set; } = false;
        public string? RecurrencePattern { get; set; } // daily, weekly, monthly

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SentAt { get; set; }
    }

    public enum ReminderType
    {
        Appointment,
        Medication,
        FollowUp,
        HomeVisit,
        Custom
    }

    public enum ReminderStatus
    {
        Pending,
        Sent,
        Dismissed,
        Failed
    }

    // ── Audit Log ─────────────────────────────────────────────────────────────
    public class AuditLog
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Entity { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string? UserId { get; set; }
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? IpAddress { get; set; }
    }
}
