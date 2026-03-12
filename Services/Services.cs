using Microsoft.EntityFrameworkCore;
using PalliativeCare.Data;
using PalliativeCare.Models;

namespace PalliativeCare.Services
{
    public interface IPatientService
    {
        Task<IEnumerable<Patient>> GetAllAsync(string? search = null, PatientStatus? status = null);
        Task<Patient?> GetByIdAsync(int id);
        Task<Patient> CreateAsync(Patient patient);
        Task<Patient> UpdateAsync(Patient patient);
        Task DeleteAsync(int id);
        Task<IEnumerable<Patient>> GetByDoctorAsync(int doctorId);
        Task<int> GetTotalCountAsync();
        Task<int> GetActiveCountAsync();
    }

    public class PatientService : IPatientService
    {
        private readonly ApplicationDbContext _db;

        public PatientService(ApplicationDbContext db) => _db = db;

        public async Task<IEnumerable<Patient>> GetAllAsync(string? search = null, PatientStatus? status = null)
        {
            var query = _db.Patients
                .Include(p => p.Doctor)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p =>
                    p.FirstName.Contains(search) ||
                    p.LastName.Contains(search) ||
                    p.Phone.Contains(search) ||
                    p.Diagnosis.Contains(search));

            if (status.HasValue)
                query = query.Where(p => p.Status == status.Value);

            return await query.OrderByDescending(p => p.RegisteredAt).ToListAsync();
        }

        public async Task<Patient?> GetByIdAsync(int id) =>
            await _db.Patients
                .Include(p => p.Doctor)
                .Include(p => p.Appointments).ThenInclude(a => a.Doctor)
                .Include(p => p.MedicalRecords).ThenInclude(m => m.Doctor)
                .Include(p => p.Reminders)
                .FirstOrDefaultAsync(p => p.Id == id);

        public async Task<Patient> CreateAsync(Patient patient)
        {
            patient.RegisteredAt = DateTime.UtcNow;
            patient.UpdatedAt    = DateTime.UtcNow;
            _db.Patients.Add(patient);
            await _db.SaveChangesAsync();
            return patient;
        }

        public async Task<Patient> UpdateAsync(Patient patient)
        {
            patient.UpdatedAt = DateTime.UtcNow;
            _db.Patients.Update(patient);
            await _db.SaveChangesAsync();
            return patient;
        }

        public async Task DeleteAsync(int id)
        {
            var p = await _db.Patients.FindAsync(id);
            if (p != null) { _db.Patients.Remove(p); await _db.SaveChangesAsync(); }
        }

        public async Task<IEnumerable<Patient>> GetByDoctorAsync(int doctorId) =>
            await _db.Patients.Where(p => p.DoctorId == doctorId)
                .Include(p => p.Doctor).ToListAsync();

        public async Task<int> GetTotalCountAsync() => await _db.Patients.CountAsync();

        public async Task<int> GetActiveCountAsync() =>
            await _db.Patients.CountAsync(p => p.Status == PatientStatus.Active);
    }

    // ── Doctor Service ────────────────────────────────────────────────────────
    public interface IDoctorService
    {
        Task<IEnumerable<Doctor>> GetAllAsync();
        Task<Doctor?> GetByIdAsync(int id);
        Task<Doctor> CreateAsync(Doctor doctor);
        Task<Doctor> UpdateAsync(Doctor doctor);
        Task DeleteAsync(int id);
        Task<int> GetTotalCountAsync();
    }

    public class DoctorService : IDoctorService
    {
        private readonly ApplicationDbContext _db;
        public DoctorService(ApplicationDbContext db) => _db = db;

        public async Task<IEnumerable<Doctor>> GetAllAsync() =>
            await _db.Doctors.OrderBy(d => d.LastName).ToListAsync();

        public async Task<Doctor?> GetByIdAsync(int id) =>
            await _db.Doctors
                .Include(d => d.Patients)
                .Include(d => d.Appointments)
                .FirstOrDefaultAsync(d => d.Id == id);

        public async Task<Doctor> CreateAsync(Doctor doctor)
        {
            doctor.JoinedAt = DateTime.UtcNow;
            _db.Doctors.Add(doctor);
            await _db.SaveChangesAsync();
            return doctor;
        }

        public async Task<Doctor> UpdateAsync(Doctor doctor)
        {
            _db.Doctors.Update(doctor);
            await _db.SaveChangesAsync();
            return doctor;
        }

        public async Task DeleteAsync(int id)
        {
            var d = await _db.Doctors.FindAsync(id);
            if (d != null) { _db.Doctors.Remove(d); await _db.SaveChangesAsync(); }
        }

        public async Task<int> GetTotalCountAsync() => await _db.Doctors.CountAsync();
    }

    // ── Appointment Service ───────────────────────────────────────────────────
    public interface IAppointmentService
    {
        Task<IEnumerable<Appointment>> GetAllAsync(DateTime? from = null, DateTime? to = null);
        Task<IEnumerable<Appointment>> GetTodayAsync();
        Task<IEnumerable<Appointment>> GetUpcomingAsync(int days = 7);
        Task<Appointment?> GetByIdAsync(int id);
        Task<Appointment> CreateAsync(Appointment appointment);
        Task<Appointment> UpdateAsync(Appointment appointment);
        Task DeleteAsync(int id);
        Task<bool> HasConflictAsync(int doctorId, DateTime scheduledAt, int durationMinutes, int? excludeId = null);
        Task<int> GetTodayCountAsync();
    }

    public class AppointmentService : IAppointmentService
    {
        private readonly ApplicationDbContext _db;
        public AppointmentService(ApplicationDbContext db) => _db = db;

        public async Task<IEnumerable<Appointment>> GetAllAsync(DateTime? from = null, DateTime? to = null)
        {
            var query = _db.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .AsQueryable();

            if (from.HasValue) query = query.Where(a => a.ScheduledAt >= from.Value);
            if (to.HasValue)   query = query.Where(a => a.ScheduledAt <= to.Value);

            return await query.OrderBy(a => a.ScheduledAt).ToListAsync();
        }

        public async Task<IEnumerable<Appointment>> GetTodayAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            return await _db.Appointments
                .Include(a => a.Patient).Include(a => a.Doctor)
                .Where(a => a.ScheduledAt >= today && a.ScheduledAt < tomorrow)
                .OrderBy(a => a.ScheduledAt).ToListAsync();
        }

        public async Task<IEnumerable<Appointment>> GetUpcomingAsync(int days = 7) =>
            await _db.Appointments
                .Include(a => a.Patient).Include(a => a.Doctor)
                .Where(a => a.ScheduledAt >= DateTime.Now && a.ScheduledAt <= DateTime.Now.AddDays(days)
                         && a.Status == AppointmentStatus.Scheduled)
                .OrderBy(a => a.ScheduledAt).ToListAsync();

        public async Task<Appointment?> GetByIdAsync(int id) =>
            await _db.Appointments
                .Include(a => a.Patient).Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == id);

        public async Task<Appointment> CreateAsync(Appointment appt)
        {
            appt.CreatedAt = DateTime.UtcNow;
            appt.UpdatedAt = DateTime.UtcNow;
            _db.Appointments.Add(appt);
            await _db.SaveChangesAsync();
            return appt;
        }

        public async Task<Appointment> UpdateAsync(Appointment appt)
        {
            appt.UpdatedAt = DateTime.UtcNow;
            _db.Appointments.Update(appt);
            await _db.SaveChangesAsync();
            return appt;
        }

        public async Task DeleteAsync(int id)
        {
            var a = await _db.Appointments.FindAsync(id);
            if (a != null) { _db.Appointments.Remove(a); await _db.SaveChangesAsync(); }
        }

        public async Task<bool> HasConflictAsync(int doctorId, DateTime scheduledAt, int durationMinutes, int? excludeId = null)
        {
            var end = scheduledAt.AddMinutes(durationMinutes);
            var query = _db.Appointments.Where(a =>
                a.DoctorId == doctorId &&
                a.Status != AppointmentStatus.Cancelled &&
                a.ScheduledAt < end &&
                a.ScheduledAt.AddMinutes(a.DurationMinutes) > scheduledAt);

            if (excludeId.HasValue)
                query = query.Where(a => a.Id != excludeId.Value);

            return await query.AnyAsync();
        }

        public async Task<int> GetTodayCountAsync()
        {
            var today = DateTime.Today;
            return await _db.Appointments.CountAsync(a =>
                a.ScheduledAt >= today && a.ScheduledAt < today.AddDays(1));
        }
    }

    // ── Reminder Service ──────────────────────────────────────────────────────
    public interface IReminderService
    {
        Task<IEnumerable<Reminder>> GetPendingAsync();
        Task<IEnumerable<Reminder>> GetAllAsync();
        Task<Reminder> CreateAsync(Reminder reminder);
        Task MarkSentAsync(int id);
        Task DismissAsync(int id);
        Task<int> GetPendingCountAsync();
        Task AutoCreateAppointmentRemindersAsync();
    }

    public class ReminderService : IReminderService
    {
        private readonly ApplicationDbContext _db;
        public ReminderService(ApplicationDbContext db) => _db = db;

        public async Task<IEnumerable<Reminder>> GetPendingAsync() =>
            await _db.Reminders
                .Include(r => r.Patient).Include(r => r.Doctor)
                .Where(r => r.Status == ReminderStatus.Pending && r.DueAt <= DateTime.Now.AddHours(24))
                .OrderBy(r => r.DueAt).ToListAsync();

        public async Task<IEnumerable<Reminder>> GetAllAsync() =>
            await _db.Reminders
                .Include(r => r.Patient).Include(r => r.Doctor)
                .OrderByDescending(r => r.DueAt).ToListAsync();

        public async Task<Reminder> CreateAsync(Reminder reminder)
        {
            reminder.CreatedAt = DateTime.UtcNow;
            _db.Reminders.Add(reminder);
            await _db.SaveChangesAsync();
            return reminder;
        }

        public async Task MarkSentAsync(int id)
        {
            var r = await _db.Reminders.FindAsync(id);
            if (r != null) { r.Status = ReminderStatus.Sent; r.SentAt = DateTime.UtcNow; await _db.SaveChangesAsync(); }
        }

        public async Task DismissAsync(int id)
        {
            var r = await _db.Reminders.FindAsync(id);
            if (r != null) { r.Status = ReminderStatus.Dismissed; await _db.SaveChangesAsync(); }
        }

        public async Task<int> GetPendingCountAsync() =>
            await _db.Reminders.CountAsync(r => r.Status == ReminderStatus.Pending);

        public async Task AutoCreateAppointmentRemindersAsync()
        {
            // Auto-create reminders for appointments happening in next 24 hours
            var tomorrow = DateTime.Now.AddHours(24);
            var upcoming = await _db.Appointments
                .Include(a => a.Patient)
                .Where(a => a.ScheduledAt <= tomorrow && a.ScheduledAt >= DateTime.Now
                         && !a.ReminderSent && a.Status == AppointmentStatus.Scheduled)
                .ToListAsync();

            foreach (var appt in upcoming)
            {
                var existing = await _db.Reminders.AnyAsync(r =>
                    r.PatientId == appt.PatientId &&
                    r.Type == ReminderType.Appointment &&
                    r.DueAt == appt.ScheduledAt);

                if (!existing)
                {
                    _db.Reminders.Add(new Reminder
                    {
                        PatientId = appt.PatientId,
                        DoctorId  = appt.DoctorId,
                        Title     = $"Appointment Reminder: {appt.Patient?.FullName}",
                        Message   = $"Appointment with {appt.Patient?.FullName} at {appt.ScheduledAt:HH:mm}. Purpose: {appt.Purpose}",
                        DueAt     = appt.ScheduledAt.AddHours(-1),
                        Type      = ReminderType.Appointment,
                        Status    = ReminderStatus.Pending,
                    });
                    appt.ReminderSent = true;
                }
            }
            await _db.SaveChangesAsync();
        }
    }

    // ── Dashboard / Stats Service ─────────────────────────────────────────────
    public interface IDashboardService
    {
        Task<DashboardStats> GetStatsAsync();
    }

    public class DashboardStats
    {
        public int TotalPatients { get; set; }
        public int ActivePatients { get; set; }
        public int TotalDoctors { get; set; }
        public int AppointmentsToday { get; set; }
        public int PendingReminders { get; set; }
        public int AppointmentsThisWeek { get; set; }
        public List<Appointment> TodayAppointments { get; set; } = new();
        public List<Reminder> UpcomingReminders { get; set; } = new();
        public Dictionary<string, int> PatientsByStatus { get; set; } = new();
        public Dictionary<string, int> AppointmentsByType { get; set; } = new();
    }

    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _db;
        public DashboardService(ApplicationDbContext db) => _db = db;

        public async Task<DashboardStats> GetStatsAsync()
        {
            var today    = DateTime.Today;
            var weekEnd  = today.AddDays(7);

            var stats = new DashboardStats
            {
                TotalPatients    = await _db.Patients.CountAsync(),
                ActivePatients   = await _db.Patients.CountAsync(p => p.Status == PatientStatus.Active),
                TotalDoctors     = await _db.Doctors.CountAsync(),
                PendingReminders = await _db.Reminders.CountAsync(r => r.Status == ReminderStatus.Pending),
                AppointmentsToday = await _db.Appointments.CountAsync(a =>
                    a.ScheduledAt >= today && a.ScheduledAt < today.AddDays(1)),
                AppointmentsThisWeek = await _db.Appointments.CountAsync(a =>
                    a.ScheduledAt >= today && a.ScheduledAt < weekEnd),

                TodayAppointments = await _db.Appointments
                    .Include(a => a.Patient).Include(a => a.Doctor)
                    .Where(a => a.ScheduledAt >= today && a.ScheduledAt < today.AddDays(1))
                    .OrderBy(a => a.ScheduledAt).Take(10).ToListAsync(),

                UpcomingReminders = await _db.Reminders
                    .Include(r => r.Patient)
                    .Where(r => r.Status == ReminderStatus.Pending)
                    .OrderBy(r => r.DueAt).Take(5).ToListAsync(),

                PatientsByStatus = await _db.Patients
                    .GroupBy(p => p.Status)
                    .ToDictionaryAsync(g => g.Key.ToString(), g => g.Count()),

                AppointmentsByType = await _db.Appointments
                    .GroupBy(a => a.Type)
                    .ToDictionaryAsync(g => g.Key.ToString(), g => g.Count()),
            };

            return stats;
        }
    }
}
