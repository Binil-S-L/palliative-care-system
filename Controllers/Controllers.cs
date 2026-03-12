using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PalliativeCare.Models;
using PalliativeCare.Services;

namespace PalliativeCare.Controllers
{
    // ── Home / Dashboard ──────────────────────────────────────────────────────
    public class HomeController : Controller
    {
        private readonly IDashboardService _dashboard;
        private readonly IReminderService _reminders;

        public HomeController(IDashboardService dashboard, IReminderService reminders)
        {
            _dashboard = dashboard;
            _reminders = reminders;
        }

        public async Task<IActionResult> Index()
        {
            await _reminders.AutoCreateAppointmentRemindersAsync();
            var stats = await _dashboard.GetStatsAsync();
            return View(stats);
        }
    }

    // ── Patient Controller ────────────────────────────────────────────────────
    public class PatientController : Controller
    {
        private readonly IPatientService _patients;
        private readonly IDoctorService _doctors;

        public PatientController(IPatientService patients, IDoctorService doctors)
        {
            _patients = patients;
            _doctors  = doctors;
        }

        public async Task<IActionResult> Index(string? search, PatientStatus? status)
        {
            ViewBag.Search = search;
            ViewBag.Status = status;
            var patients = await _patients.GetAllAsync(search, status);
            return View(patients);
        }

        public async Task<IActionResult> Details(int id)
        {
            var patient = await _patients.GetByIdAsync(id);
            if (patient == null) return NotFound();
            return View(patient);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateDoctors();
            return View(new Patient());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Patient patient)
        {
            if (!ModelState.IsValid) { await PopulateDoctors(); return View(patient); }
            await _patients.CreateAsync(patient);
            TempData["Success"] = $"Patient {patient.FullName} registered successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var patient = await _patients.GetByIdAsync(id);
            if (patient == null) return NotFound();
            await PopulateDoctors(patient.DoctorId);
            return View(patient);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Patient patient)
        {
            if (id != patient.Id) return BadRequest();
            if (!ModelState.IsValid) { await PopulateDoctors(patient.DoctorId); return View(patient); }
            await _patients.UpdateAsync(patient);
            TempData["Success"] = "Patient updated successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _patients.DeleteAsync(id);
            TempData["Success"] = "Patient record deleted.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDoctors(int? selectedId = null)
        {
            var doctors = await _doctors.GetAllAsync();
            ViewBag.Doctors = new SelectList(doctors, "Id", "FullName", selectedId);
        }
    }

    // ── Doctor Controller ─────────────────────────────────────────────────────
    public class DoctorController : Controller
    {
        private readonly IDoctorService _doctors;

        public DoctorController(IDoctorService doctors) => _doctors = doctors;

        public async Task<IActionResult> Index() => View(await _doctors.GetAllAsync());

        public async Task<IActionResult> Details(int id)
        {
            var doctor = await _doctors.GetByIdAsync(id);
            if (doctor == null) return NotFound();
            return View(doctor);
        }

        public IActionResult Create() => View(new Doctor());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Doctor doctor)
        {
            if (!ModelState.IsValid) return View(doctor);
            await _doctors.CreateAsync(doctor);
            TempData["Success"] = $"{doctor.FullName} added to the team.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var doctor = await _doctors.GetByIdAsync(id);
            if (doctor == null) return NotFound();
            return View(doctor);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Doctor doctor)
        {
            if (id != doctor.Id) return BadRequest();
            if (!ModelState.IsValid) return View(doctor);
            await _doctors.UpdateAsync(doctor);
            TempData["Success"] = "Doctor/Staff updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _doctors.DeleteAsync(id);
            TempData["Success"] = "Staff member removed.";
            return RedirectToAction(nameof(Index));
        }
    }

    // ── Appointment Controller ────────────────────────────────────────────────
    public class AppointmentController : Controller
    {
        private readonly IAppointmentService _appointments;
        private readonly IPatientService _patients;
        private readonly IDoctorService _doctors;
        private readonly IReminderService _reminders;

        public AppointmentController(IAppointmentService appointments, IPatientService patients,
            IDoctorService doctors, IReminderService reminders)
        {
            _appointments = appointments;
            _patients = patients;
            _doctors  = doctors;
            _reminders = reminders;
        }

        public async Task<IActionResult> Index(DateTime? from, DateTime? to)
        {
            from ??= DateTime.Today;
            to   ??= DateTime.Today.AddDays(30);
            ViewBag.From = from.Value.ToString("yyyy-MM-dd");
            ViewBag.To   = to.Value.ToString("yyyy-MM-dd");
            return View(await _appointments.GetAllAsync(from, to));
        }

        public async Task<IActionResult> Today() =>
            View("Index", await _appointments.GetTodayAsync());

        public async Task<IActionResult> Details(int id)
        {
            var appt = await _appointments.GetByIdAsync(id);
            if (appt == null) return NotFound();
            return View(appt);
        }

        public async Task<IActionResult> Create(int? patientId)
        {
            await PopulateLists(patientId);
            return View(new Appointment { ScheduledAt = DateTime.Now.AddHours(1), DurationMinutes = 30, PatientId = patientId ?? 0 });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Appointment appt)
        {
            if (!ModelState.IsValid) { await PopulateLists(); return View(appt); }

            if (await _appointments.HasConflictAsync(appt.DoctorId, appt.ScheduledAt, appt.DurationMinutes))
            {
                ModelState.AddModelError("", "Doctor has a scheduling conflict at this time.");
                await PopulateLists(); return View(appt);
            }

            await _appointments.CreateAsync(appt);

            // Auto-create reminder
            await _reminders.CreateAsync(new Reminder
            {
                PatientId = appt.PatientId,
                DoctorId  = appt.DoctorId,
                Title     = "Upcoming Appointment",
                Message   = $"Appointment scheduled for {appt.ScheduledAt:dd MMM yyyy HH:mm}. Purpose: {appt.Purpose}",
                DueAt     = appt.ScheduledAt.AddHours(-2),
                Type      = ReminderType.Appointment,
                Status    = ReminderStatus.Pending,
            });

            TempData["Success"] = "Appointment scheduled and reminder created.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var appt = await _appointments.GetByIdAsync(id);
            if (appt == null) return NotFound();
            await PopulateLists(appt.PatientId, appt.DoctorId);
            return View(appt);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Appointment appt)
        {
            if (id != appt.Id) return BadRequest();
            if (!ModelState.IsValid) { await PopulateLists(); return View(appt); }

            if (await _appointments.HasConflictAsync(appt.DoctorId, appt.ScheduledAt, appt.DurationMinutes, id))
            {
                ModelState.AddModelError("", "Doctor has a scheduling conflict at this time.");
                await PopulateLists(); return View(appt);
            }

            await _appointments.UpdateAsync(appt);
            TempData["Success"] = "Appointment updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _appointments.DeleteAsync(id);
            TempData["Success"] = "Appointment cancelled.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateLists(int? patientId = null, int? doctorId = null)
        {
            var patients = await _patients.GetAllAsync(status: PatientStatus.Active);
            var doctors  = await _doctors.GetAllAsync();
            ViewBag.Patients = new SelectList(patients, "Id", "FullName", patientId);
            ViewBag.Doctors  = new SelectList(doctors,  "Id", "FullName", doctorId);
        }
    }

    // ── Medical Record Controller ─────────────────────────────────────────────
    public class MedicalRecordController : Controller
    {
        private readonly Data.ApplicationDbContext _db;
        private readonly IPatientService _patients;
        private readonly IDoctorService _doctors;

        public MedicalRecordController(Data.ApplicationDbContext db, IPatientService patients, IDoctorService doctors)
        {
            _db = db; _patients = patients; _doctors = doctors;
        }

        public async Task<IActionResult> Index(int? patientId)
        {
            var query = _db.MedicalRecords
                .Include(m => m.Patient).Include(m => m.Doctor)
                .AsQueryable();

            if (patientId.HasValue)
                query = query.Where(m => m.PatientId == patientId.Value);

            var records = await query.OrderByDescending(m => m.RecordDate).ToListAsync();
            ViewBag.PatientId = patientId;
            return View(records);
        }

        public async Task<IActionResult> Create(int? patientId)
        {
            await PopulateLists(patientId);
            return View(new MedicalRecord { RecordDate = DateTime.Now, PatientId = patientId ?? 0 });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MedicalRecord record)
        {
            if (!ModelState.IsValid) { await PopulateLists(); return View(record); }
            record.CreatedAt = DateTime.UtcNow;
            _db.MedicalRecords.Add(record);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Medical record saved.";
            return RedirectToAction("Details", "Patient", new { id = record.PatientId });
        }

        public async Task<IActionResult> Details(int id)
        {
            var record = await _db.MedicalRecords
                .Include(m => m.Patient).Include(m => m.Doctor)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (record == null) return NotFound();
            return View(record);
        }

        private async Task PopulateLists(int? patientId = null, int? doctorId = null)
        {
            var patients = await _patients.GetAllAsync(status: PatientStatus.Active);
            var doctors  = await _doctors.GetAllAsync();
            ViewBag.Patients = new SelectList(patients, "Id", "FullName", patientId);
            ViewBag.Doctors  = new SelectList(doctors,  "Id", "FullName", doctorId);
        }
    }

    // ── Reminder Controller ───────────────────────────────────────────────────
    public class ReminderController : Controller
    {
        private readonly IReminderService _reminders;
        private readonly IPatientService _patients;
        private readonly IDoctorService _doctors;

        public ReminderController(IReminderService reminders, IPatientService patients, IDoctorService doctors)
        {
            _reminders = reminders; _patients = patients; _doctors = doctors;
        }

        public async Task<IActionResult> Index() => View(await _reminders.GetAllAsync());

        public async Task<IActionResult> Create(int? patientId)
        {
            await PopulateLists(patientId);
            return View(new Reminder { DueAt = DateTime.Now.AddHours(1), PatientId = patientId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Reminder reminder)
        {
            if (!ModelState.IsValid) { await PopulateLists(); return View(reminder); }
            await _reminders.CreateAsync(reminder);
            TempData["Success"] = "Reminder created.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> MarkSent(int id)
        {
            await _reminders.MarkSentAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Dismiss(int id)
        {
            await _reminders.DismissAsync(id);
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateLists(int? patientId = null, int? doctorId = null)
        {
            var patients = await _patients.GetAllAsync();
            var doctors  = await _doctors.GetAllAsync();
            ViewBag.Patients = new SelectList(patients, "Id", "FullName", patientId);
            ViewBag.Doctors  = new SelectList(doctors,  "Id", "FullName", doctorId);
        }
    }
}
