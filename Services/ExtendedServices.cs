using Microsoft.EntityFrameworkCore;
using PalliativeCare.Data;
using PalliativeCare.Models;

namespace PalliativeCare.Services
{
    // ── PPS Service ───────────────────────────────────────────────────────────
    public interface IPpsService
    {
        Task<IEnumerable<PpsAssessment>> GetForPatientAsync(int patientId);
        Task<PpsAssessment?> GetLatestAsync(int patientId);
        Task<PpsAssessment> CreateAsync(PpsAssessment assessment);
        Task<PpsDeclineAlert?> CheckDeclineAsync(int patientId);
    }

    public class PpsDeclineAlert
    {
        public int PatientId       { get; set; }
        public string PatientName  { get; set; } = string.Empty;
        public int PreviousScore   { get; set; }
        public int CurrentScore    { get; set; }
        public int Drop            => PreviousScore - CurrentScore;
        public DateTime Since      { get; set; }
        public bool IsCritical     => Drop >= 20;
    }

    public class PpsService : IPpsService
    {
        private readonly ApplicationDbContext _db;
        public PpsService(ApplicationDbContext db) => _db = db;

        public async Task<IEnumerable<PpsAssessment>> GetForPatientAsync(int patientId) =>
            await _db.Set<PpsAssessment>()
                .Include(p => p.Doctor)
                .Where(p => p.PatientId == patientId)
                .OrderByDescending(p => p.AssessedAt)
                .ToListAsync();

        public async Task<PpsAssessment?> GetLatestAsync(int patientId) =>
            await _db.Set<PpsAssessment>()
                .Where(p => p.PatientId == patientId)
                .OrderByDescending(p => p.AssessedAt)
                .FirstOrDefaultAsync();

        public async Task<PpsAssessment> CreateAsync(PpsAssessment assessment)
        {
            assessment.CreatedAt  = DateTime.UtcNow;
            assessment.AssessedAt = DateTime.UtcNow;
            _db.Set<PpsAssessment>().Add(assessment);
            await _db.SaveChangesAsync();
            return assessment;
        }

        public async Task<PpsDeclineAlert?> CheckDeclineAsync(int patientId)
        {
            var recent = await _db.Set<PpsAssessment>()
                .Include(p => p.Patient)
                .Where(p => p.PatientId == patientId)
                .OrderByDescending(p => p.AssessedAt)
                .Take(2)
                .ToListAsync();

            if (recent.Count < 2) return null;

            var latest   = recent[0];
            var previous = recent[1];
            var drop     = previous.Score - latest.Score;

            if (drop <= 0) return null;

            return new PpsDeclineAlert
            {
                PatientId    = patientId,
                PatientName  = latest.Patient?.FullName ?? string.Empty,
                PreviousScore = previous.Score,
                CurrentScore  = latest.Score,
                Since         = previous.AssessedAt,
            };
        }
    }

    // ── Family Portal Service ─────────────────────────────────────────────────
    public interface IFamilyPortalService
    {
        // Family member management
        Task<IEnumerable<FamilyMember>> GetFamilyForPatientAsync(int patientId);
        Task<FamilyMember?> GetFamilyMemberAsync(int id);
        Task<FamilyMember> AddFamilyMemberAsync(FamilyMember member);
        Task ApproveFamilyMemberAsync(int id);
        Task RemoveFamilyMemberAsync(int id);

        // Care updates (care team → family)
        Task<IEnumerable<CareUpdate>> GetUpdatesForPatientAsync(int patientId, bool includeStaffOnly = false);
        Task<CareUpdate> PostUpdateAsync(CareUpdate update);

        // Messaging
        Task<IEnumerable<FamilyMessage>> GetMessagesAsync(int patientId);
        Task<FamilyMessage> SendMessageAsync(FamilyMessage message);
        Task MarkReadAsync(int messageId);
        Task<int> GetUnreadCountAsync(int patientId);
    }

    public class FamilyPortalService : IFamilyPortalService
    {
        private readonly ApplicationDbContext _db;
        public FamilyPortalService(ApplicationDbContext db) => _db = db;

        // ── Family Members ─────────────────────────────────────────────────
        public async Task<IEnumerable<FamilyMember>> GetFamilyForPatientAsync(int patientId) =>
            await _db.Set<FamilyMember>()
                .Where(f => f.PatientId == patientId)
                .OrderBy(f => f.FirstName).ToListAsync();

        public async Task<FamilyMember?> GetFamilyMemberAsync(int id) =>
            await _db.Set<FamilyMember>()
                .Include(f => f.Patient)
                .Include(f => f.Messages)
                .FirstOrDefaultAsync(f => f.Id == id);

        public async Task<FamilyMember> AddFamilyMemberAsync(FamilyMember member)
        {
            member.AddedAt = DateTime.UtcNow;
            _db.Set<FamilyMember>().Add(member);
            await _db.SaveChangesAsync();
            return member;
        }

        public async Task ApproveFamilyMemberAsync(int id)
        {
            var m = await _db.Set<FamilyMember>().FindAsync(id);
            if (m != null) { m.IsApproved = true; await _db.SaveChangesAsync(); }
        }

        public async Task RemoveFamilyMemberAsync(int id)
        {
            var m = await _db.Set<FamilyMember>().FindAsync(id);
            if (m != null) { _db.Set<FamilyMember>().Remove(m); await _db.SaveChangesAsync(); }
        }

        // ── Care Updates ───────────────────────────────────────────────────
        public async Task<IEnumerable<CareUpdate>> GetUpdatesForPatientAsync(int patientId, bool includeStaffOnly = false)
        {
            var query = _db.Set<CareUpdate>()
                .Include(u => u.Doctor)
                .Where(u => u.PatientId == patientId);

            if (!includeStaffOnly)
                query = query.Where(u => u.Visibility == UpdateVisibility.Family);

            return await query.OrderByDescending(u => u.PostedAt).ToListAsync();
        }

        public async Task<CareUpdate> PostUpdateAsync(CareUpdate update)
        {
            update.PostedAt = DateTime.UtcNow;
            _db.Set<CareUpdate>().Add(update);
            await _db.SaveChangesAsync();
            return update;
        }

        // ── Messages ───────────────────────────────────────────────────────
        public async Task<IEnumerable<FamilyMessage>> GetMessagesAsync(int patientId) =>
            await _db.Set<FamilyMessage>()
                .Include(m => m.FamilyMember)
                .Include(m => m.Doctor)
                .Where(m => m.PatientId == patientId)
                .OrderBy(m => m.SentAt).ToListAsync();

        public async Task<FamilyMessage> SendMessageAsync(FamilyMessage message)
        {
            message.SentAt = DateTime.UtcNow;
            _db.Set<FamilyMessage>().Add(message);
            await _db.SaveChangesAsync();
            return message;
        }

        public async Task MarkReadAsync(int messageId)
        {
            var m = await _db.Set<FamilyMessage>().FindAsync(messageId);
            if (m != null) { m.IsRead = true; await _db.SaveChangesAsync(); }
        }

        public async Task<int> GetUnreadCountAsync(int patientId) =>
            await _db.Set<FamilyMessage>().CountAsync(m =>
                m.PatientId == patientId && !m.IsRead &&
                m.Direction == MessageDirection.FamilyToTeam);
    }
}
