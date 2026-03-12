using System.ComponentModel.DataAnnotations;

namespace PalliativeCare.Models
{
    // ── Palliative Performance Scale (PPS) Assessment ─────────────────────────
    // Based on the Victoria Hospice PPS — standard clinical tool in palliative care.
    // Score ranges: 100 (fully mobile, normal activity) → 0 (death)
    public class PpsAssessment
    {
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }
        public Patient? Patient { get; set; }

        [Required]
        public int DoctorId { get; set; }
        public Doctor? Doctor { get; set; }

        /// <summary>PPS score: 0, 10, 20 ... 100</summary>
        [Required, Range(0, 100)]
        public int Score { get; set; }

        public DateTime AssessedAt { get; set; } = DateTime.UtcNow;

        // PPS domains (clinician fills these in — system derives the score)
        public string? Ambulation       { get; set; }  // e.g. "Reduced"
        public string? Activity         { get; set; }  // e.g. "Unable to do normal work"
        public string? SelfCare         { get; set; }  // e.g. "Mainly sit/lie"
        public string? Intake           { get; set; }  // e.g. "Normal or reduced"
        public string? Consciousness    { get; set; }  // e.g. "Full or confusion"

        public string? ClinicalNotes    { get; set; }
        public DateTime CreatedAt       { get; set; } = DateTime.UtcNow;

        // Derived label
        public string ScoreLabel => Score switch
        {
            100 => "Full — Normal activity, no evidence of disease",
            90  => "Full — Normal activity, some evidence of disease",
            80  => "Full — Normal activity with effort",
            70  => "Reduced — Unable to do normal job/work",
            60  => "Reduced — Unable to do hobby/housework",
            50  => "Mainly sit/lie — Unable to do any work",
            40  => "Mainly in bed — Unable to do most activity",
            30  => "Totally bed bound — Unable to do any activity",
            20  => "Totally bed bound — Minimal activity",
            10  => "Totally bed bound — Drowsy or coma",
            0   => "Death",
            _   => $"PPS {Score}%"
        };

        public string RiskCategory => Score switch
        {
            >= 70 => "Stable",
            >= 50 => "Transitional",
            >= 30 => "Declining",
            >= 10 => "Terminal",
            _     => "Deceased"
        };

        public string RiskColour => Score switch
        {
            >= 70 => "success",
            >= 50 => "warning",
            >= 30 => "danger",
            _     => "dark"
        };
    }

    // ── Family Member (portal user linked to a patient) ───────────────────────
    public class FamilyMember
    {
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }
        public Patient? Patient { get; set; }

        [Required, StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        public string FullName => $"{FirstName} {LastName}";

        [Required]
        public string Relationship { get; set; } = string.Empty;

        [Required, Phone]
        public string Phone { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        // Linked to ASP.NET Identity account (created when portal invite sent)
        public string? UserId { get; set; }

        public bool IsApproved      { get; set; } = false;
        public bool CanViewRecords  { get; set; } = true;
        public bool CanMessage      { get; set; } = true;

        public DateTime AddedAt     { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin  { get; set; }

        public ICollection<FamilyMessage> Messages { get; set; } = new List<FamilyMessage>();
    }

    // ── Family Portal Messages ────────────────────────────────────────────────
    public class FamilyMessage
    {
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }
        public Patient? Patient { get; set; }

        // Sender — either a family member or a staff member
        public int? FamilyMemberId  { get; set; }
        public FamilyMember? FamilyMember { get; set; }

        public int? DoctorId        { get; set; }
        public Doctor? Doctor       { get; set; }

        [Required]
        public string Body          { get; set; } = string.Empty;

        public MessageDirection Direction { get; set; }
        public bool IsRead          { get; set; } = false;
        public DateTime SentAt      { get; set; } = DateTime.UtcNow;
    }

    public enum MessageDirection
    {
        FamilyToTeam,
        TeamToFamily
    }

    // ── Portal Update (care team posts a public update for family) ────────────
    public class CareUpdate
    {
        public int Id { get; set; }

        [Required]
        public int PatientId    { get; set; }
        public Patient? Patient { get; set; }

        public int? DoctorId    { get; set; }
        public Doctor? Doctor   { get; set; }

        [Required, StringLength(200)]
        public string Title     { get; set; } = string.Empty;

        [Required]
        public string Body      { get; set; } = string.Empty;

        public UpdateVisibility Visibility { get; set; } = UpdateVisibility.Family;
        public DateTime PostedAt { get; set; } = DateTime.UtcNow;
    }

    public enum UpdateVisibility
    {
        Family,      // visible to approved family members
        StaffOnly    // internal only
    }
}
