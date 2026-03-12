using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PalliativeCare.Models;
using PalliativeCare.Services;

namespace PalliativeCare.Controllers
{
    // ── PPS Controller ────────────────────────────────────────────────────────
    public class PpsController : Controller
    {
        private readonly IPpsService _pps;
        private readonly IPatientService _patients;
        private readonly IDoctorService _doctors;

        public PpsController(IPpsService pps, IPatientService patients, IDoctorService doctors)
        {
            _pps = pps; _patients = patients; _doctors = doctors;
        }

        /// <summary>Full PPS history + trend chart for one patient.</summary>
        public async Task<IActionResult> PatientHistory(int patientId)
        {
            var patient = await _patients.GetByIdAsync(patientId);
            if (patient == null) return NotFound();

            var assessments = await _pps.GetForPatientAsync(patientId);
            var alert       = await _pps.CheckDeclineAsync(patientId);

            ViewBag.Patient = patient;
            ViewBag.Alert   = alert;
            ViewData["Title"] = $"PPS History — {patient.FullName}";
            return View(assessments);
        }

        /// <summary>New PPS assessment form.</summary>
        public async Task<IActionResult> Create(int patientId)
        {
            var patient = await _patients.GetByIdAsync(patientId);
            if (patient == null) return NotFound();

            await PopulateDoctors();
            ViewBag.Patient = patient;
            ViewData["Title"] = "Record PPS Assessment";
            return View(new PpsAssessment { PatientId = patientId, Score = 70 });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PpsAssessment assessment)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Patient = await _patients.GetByIdAsync(assessment.PatientId);
                await PopulateDoctors();
                return View(assessment);
            }

            await _pps.CreateAsync(assessment);

            // Check for significant decline and set alert
            var alert = await _pps.CheckDeclineAsync(assessment.PatientId);
            if (alert?.IsCritical == true)
                TempData["Warning"] = $"⚠️ PPS declined {alert.Drop} points — from {alert.PreviousScore}% to {alert.CurrentScore}%. Clinical review recommended.";
            else
                TempData["Success"] = $"PPS assessment recorded: {assessment.Score}% — {assessment.ScoreLabel}";

            return RedirectToAction("PatientHistory", new { patientId = assessment.PatientId });
        }

        private async Task PopulateDoctors(int? selected = null)
        {
            var doctors = await _doctors.GetAllAsync();
            ViewBag.Doctors = new SelectList(doctors, "Id", "FullName", selected);
        }
    }

    // ── Family Portal Controller (staff side) ─────────────────────────────────
    public class FamilyPortalController : Controller
    {
        private readonly IFamilyPortalService _family;
        private readonly IPatientService _patients;
        private readonly IDoctorService _doctors;

        public FamilyPortalController(IFamilyPortalService family, IPatientService patients, IDoctorService doctors)
        {
            _family = family; _patients = patients; _doctors = doctors;
        }

        /// <summary>Staff view — manage family members and messages for a patient.</summary>
        public async Task<IActionResult> Manage(int patientId)
        {
            var patient = await _patients.GetByIdAsync(patientId);
            if (patient == null) return NotFound();

            var family   = await _family.GetFamilyForPatientAsync(patientId);
            var updates  = await _family.GetUpdatesForPatientAsync(patientId, includeStaffOnly: true);
            var messages = await _family.GetMessagesAsync(patientId);
            var unread   = await _family.GetUnreadCountAsync(patientId);

            ViewBag.Patient  = patient;
            ViewBag.Family   = family;
            ViewBag.Updates  = updates;
            ViewBag.Messages = messages;
            ViewBag.Unread   = unread;
            ViewData["Title"] = $"Family Portal — {patient.FullName}";
            return View();
        }

        // ── Add Family Member ─────────────────────────────────────────────
        public async Task<IActionResult> AddMember(int patientId)
        {
            var patient = await _patients.GetByIdAsync(patientId);
            if (patient == null) return NotFound();
            ViewBag.Patient = patient;
            ViewData["Title"] = "Add Family / Carer";
            return View(new FamilyMember { PatientId = patientId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(FamilyMember member)
        {
            if (!ModelState.IsValid) { ViewBag.Patient = await _patients.GetByIdAsync(member.PatientId); return View(member); }
            await _family.AddFamilyMemberAsync(member);
            TempData["Success"] = $"{member.FullName} added. Approve their access below.";
            return RedirectToAction("Manage", new { patientId = member.PatientId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, int patientId)
        {
            await _family.ApproveFamilyMemberAsync(id);
            TempData["Success"] = "Access approved — family member can now log in to the portal.";
            return RedirectToAction("Manage", new { patientId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id, int patientId)
        {
            await _family.RemoveFamilyMemberAsync(id);
            TempData["Success"] = "Family member removed.";
            return RedirectToAction("Manage", new { patientId });
        }

        // ── Post Care Update ──────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> PostUpdate(int patientId, string title, string body,
            UpdateVisibility visibility, int? doctorId)
        {
            await _family.PostUpdateAsync(new CareUpdate
            {
                PatientId  = patientId,
                DoctorId   = doctorId,
                Title      = title,
                Body       = body,
                Visibility = visibility,
            });
            TempData["Success"] = "Update posted to family portal.";
            return RedirectToAction("Manage", new { patientId });
        }

        // ── Reply to Message ──────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int patientId, string body, int? doctorId)
        {
            await _family.SendMessageAsync(new FamilyMessage
            {
                PatientId = patientId,
                DoctorId  = doctorId,
                Body      = body,
                Direction = MessageDirection.TeamToFamily,
                IsRead    = false,
            });
            TempData["Success"] = "Reply sent to family.";
            return RedirectToAction("Manage", new { patientId });
        }

        // ── Mark message read ─────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> MarkRead(int messageId, int patientId)
        {
            await _family.MarkReadAsync(messageId);
            return RedirectToAction("Manage", new { patientId });
        }

        // ── Family Portal (read-only view for family members) ─────────────
        /// <summary>
        /// This is the portal view a family member sees after logging in.
        /// In a full build, this would be behind family-role auth.
        /// For this project it's accessible by token/patientId.
        /// </summary>
        public async Task<IActionResult> Portal(int patientId, int familyMemberId)
        {
            var patient = await _patients.GetByIdAsync(patientId);
            if (patient == null) return NotFound();

            var member = await _family.GetFamilyMemberAsync(familyMemberId);
            if (member == null || !member.IsApproved || member.PatientId != patientId)
                return Forbid();

            var updates  = await _family.GetUpdatesForPatientAsync(patientId, includeStaffOnly: false);
            var messages = await _family.GetMessagesAsync(patientId);

            ViewBag.Patient  = patient;
            ViewBag.Member   = member;
            ViewBag.Updates  = updates;
            ViewBag.Messages = messages;
            ViewData["Title"] = $"Care Updates — {patient.FullName}";
            return View();
        }

        // ── Family member sends message ────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int patientId, int familyMemberId, string body)
        {
            await _family.SendMessageAsync(new FamilyMessage
            {
                PatientId      = patientId,
                FamilyMemberId = familyMemberId,
                Body           = body,
                Direction      = MessageDirection.FamilyToTeam,
                IsRead         = false,
            });
            TempData["Success"] = "Message sent to the care team.";
            return RedirectToAction("Portal", new { patientId, familyMemberId });
        }
    }
}
