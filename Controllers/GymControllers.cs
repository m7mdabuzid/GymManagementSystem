using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GymManagementSystem.Data;
using GymManagementSystem.Models;

namespace GymManagementSystem.Controllers
{
    // ------------------------------
    // MembersController (DB version)
    // ------------------------------
    public class MembersController : Controller
    {
        private readonly ApplicationDbContext _db;

        public MembersController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Members
        public async Task<IActionResult> Index()
        {
            var members = await _db.Members
                .OrderBy(m => m.MemberId)
                .ToListAsync();

            return View(members);
        }

        // GET: /Members/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var member = await _db.Members
                .Include(m => m.Bookings)
                    .ThenInclude(b => b.Session)
                        .ThenInclude(s => s.Trainer)
                .Include(m => m.ProgressLog)
                .FirstOrDefaultAsync(m => m.MemberId == id);

            if (member == null)
                return NotFound();

            // Optional: provide list of sessions to book from in the Details view
            ViewBag.AllSessions = await _db.ClassSessions
                .Include(s => s.Trainer)
                .OrderBy(s => s.ScheduleTime)
                .ToListAsync();

            return View(member);
        }

        // GET: /Members/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Members/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Member member)
        {
            if (!ModelState.IsValid)
                return View(member);

            // Keep same behavior as before:
            // if end date not provided => active + 1 month
            if (member.MembershipEndDate == null)
            {
                member.IsActive = true;
                member.MembershipEndDate = DateTime.Today.AddMonths(1);
            }

            _db.Members.Add(member);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /Members/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var member = await _db.Members.FirstOrDefaultAsync(m => m.MemberId == id);
            if (member == null)
                return NotFound();

            return View(member);
        }

        // POST: /Members/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Member updated)
        {
            if (!ModelState.IsValid)
                return View(updated);

            var member = await _db.Members.FirstOrDefaultAsync(m => m.MemberId == updated.MemberId);
            if (member == null)
                return NotFound();

            member.Name = updated.Name;
            member.MembershipType = updated.MembershipType;
            member.IsActive = updated.IsActive;
            member.MembershipEndDate = updated.MembershipEndDate;

            await _db.SaveChangesAsync();

            TempData["Message"] = "Member updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Members/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var member = await _db.Members.FirstOrDefaultAsync(m => m.MemberId == id);
            if (member == null)
                return NotFound();

            return View(member);
        }

        // POST: /Members/DeleteConfirmed/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var member = await _db.Members.FirstOrDefaultAsync(m => m.MemberId == id);
            if (member == null)
                return NotFound();

            _db.Members.Remove(member);
            await _db.SaveChangesAsync();

            TempData["Message"] = "Member removed.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Members/ToggleActive/5
        public async Task<IActionResult> ToggleActive(int id)
        {
            var member = await _db.Members.FirstOrDefaultAsync(m => m.MemberId == id);
            if (member == null)
                return NotFound();

            member.IsActive = !member.IsActive;
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: /Members/Book?memberId=1&sessionId=2
        public async Task<IActionResult> Book(int memberId, int sessionId)
        {
            var member = await _db.Members.FirstOrDefaultAsync(m => m.MemberId == memberId);
            var session = await _db.ClassSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (member == null || session == null)
                return NotFound();

            // Rule 1: membership validity (same logic as before)
            if (!member.CanBookClass())
            {
                TempData["Error"] = "Member has inactive or expired membership.";
                return RedirectToAction(nameof(Details), new { id = memberId });
            }

            // Rule 2: capacity
            var bookedCount = await _db.Bookings.CountAsync(b => b.SessionId == sessionId);
            if (bookedCount >= session.MaxSlots)
            {
                TempData["Error"] = "No available slots for this class.";
                return RedirectToAction(nameof(Details), new { id = memberId });
            }

            // Rule 3: prevent duplicate booking
            var alreadyBooked = await _db.Bookings.AnyAsync(b => b.MemberId == memberId && b.SessionId == sessionId);
            if (alreadyBooked)
            {
                TempData["Error"] = "Member is already booked in this class.";
                return RedirectToAction(nameof(Details), new { id = memberId });
            }

            _db.Bookings.Add(new Booking
            {
                MemberId = memberId,
                SessionId = sessionId,
                BookedAt = DateTime.Now
            });

            try
            {
                await _db.SaveChangesAsync();
                TempData["Message"] = $"Booked {member.Name} into {session.ClassName}.";
            }
            catch (DbUpdateException)
            {
                // In case unique index triggers (race condition)
                TempData["Error"] = "Could not complete booking (it may already exist).";
            }

            return RedirectToAction(nameof(Details), new { id = memberId });
        }

        // GET: /Members/CancelBooking?memberId=1&sessionId=2
        public async Task<IActionResult> CancelBooking(int memberId, int sessionId)
        {
            var booking = await _db.Bookings
                .FirstOrDefaultAsync(b => b.MemberId == memberId && b.SessionId == sessionId);

            if (booking == null)
            {
                TempData["Error"] = "Booking not found.";
                return RedirectToAction(nameof(Details), new { id = memberId });
            }

            _db.Bookings.Remove(booking);
            await _db.SaveChangesAsync();

            TempData["Message"] = "Booking canceled.";
            return RedirectToAction(nameof(Details), new { id = memberId });
        }
    }

    // ------------------------------
    // TrainersController (DB version)
    // ------------------------------
    public class TrainersController : Controller
    {
        private readonly ApplicationDbContext _db;

        public TrainersController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Trainers
        public async Task<IActionResult> Index()
        {
            var trainers = await _db.Trainers
                .Include(t => t.AssignedClasses) // ✅ load classes so count isn't 0
                .OrderBy(t => t.TrainerId)
                .ToListAsync();

            return View(trainers);
        }

        // GET: /Trainers/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var trainer = await _db.Trainers
                .Include(t => t.AssignedClasses)
                .FirstOrDefaultAsync(t => t.TrainerId == id);

            if (trainer == null)
                return NotFound();

            return View(trainer);
        }
    }

    // ------------------------------
    // ClassSessionsController (DB version)
    // ------------------------------
    public class ClassSessionsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ClassSessionsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /ClassSessions
        public async Task<IActionResult> Index()
        {
            var sessions = await _db.ClassSessions
                .Include(s => s.Trainer)
                .Include(s => s.Bookings) // ✅ load bookings so Booked shows correctly
                .OrderBy(s => s.ScheduleTime)
                .ToListAsync();

            return View(sessions);
        }

        // GET: /ClassSessions/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var session = await _db.ClassSessions
                .Include(s => s.Trainer)
                .Include(s => s.Bookings)
                    .ThenInclude(b => b.Member)
                .FirstOrDefaultAsync(s => s.SessionId == id);

            if (session == null)
                return NotFound();

            // Optional: provide list of members to book from in the Details view
            ViewBag.AllMembers = await _db.Members
                .OrderBy(m => m.Name)
                .ToListAsync();

            return View(session);
        }

        // GET: /ClassSessions/Book?sessionId=1&memberId=2
        public async Task<IActionResult> Book(int sessionId, int memberId)
        {
            var member = await _db.Members.FirstOrDefaultAsync(m => m.MemberId == memberId);
            var session = await _db.ClassSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (member == null || session == null)
                return NotFound();

            if (!member.CanBookClass())
            {
                TempData["Error"] = "Member has inactive or expired membership.";
                return RedirectToAction(nameof(Details), new { id = sessionId });
            }

            var bookedCount = await _db.Bookings.CountAsync(b => b.SessionId == sessionId);
            if (bookedCount >= session.MaxSlots)
            {
                TempData["Error"] = "No available slots for this class.";
                return RedirectToAction(nameof(Details), new { id = sessionId });
            }

            var alreadyBooked = await _db.Bookings
                .AnyAsync(b => b.MemberId == memberId && b.SessionId == sessionId);

            if (alreadyBooked)
            {
                TempData["Error"] = "Member is already booked in this class.";
                return RedirectToAction(nameof(Details), new { id = sessionId });
            }

            _db.Bookings.Add(new Booking
            {
                MemberId = memberId,
                SessionId = sessionId,
                BookedAt = DateTime.Now
            });

            try
            {
                await _db.SaveChangesAsync();
                TempData["Message"] = $"Booked {member.Name} into {session.ClassName}.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Could not complete booking (it may already exist).";
            }

            return RedirectToAction(nameof(Details), new { id = sessionId });
        }
    }
}

