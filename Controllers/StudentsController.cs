using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using pms.Data;
using pms.Models;

namespace pms.Controllers
{
    /// <summary>
    /// Controller for managing student profiles and records.
    /// Provides administrative access to student data.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class StudentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<pms.Hubs.RefreshHub> _hubContext;

        public StudentsController(ApplicationDbContext context, Microsoft.AspNetCore.SignalR.IHubContext<pms.Hubs.RefreshHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Displays a list of all registered students.
        /// </summary>
        /// <returns>A view with the students list.</returns>
        public async Task<IActionResult> Index()
        {
            return View(await _context.Students.ToListAsync());
        }

        /// <summary>
        /// Shows the details of a specific student profile.
        /// </summary>
        /// <param name="id">The student ID.</param>
        /// <returns>A view with student details or NotFound.</returns>
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var student = await _context.Students.FindAsync(id);
            if (student == null) return NotFound();
            return View(student);
        }

        /// <summary>
        /// Renders the view to create a new student record manually.
        /// </summary>
        /// <returns>The creation view.</returns>
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Processes the manual creation of a new student record.
        /// </summary>
        /// <param name="student">The student data.</param>
        /// <returns>Redirect to Index on success, or the creation view on failure.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Student student)
        {
            if (ModelState.IsValid)
            {
                _context.Add(student);
                await _context.SaveChangesAsync();
                await _hubContext.Clients.All.SendAsync("ReceiveRefresh");
                return RedirectToAction(nameof(Index));
            }
            return View(student);
        }

        /// <summary>
        /// Renders the edit view for an existing student record.
        /// </summary>
        /// <param name="id">The student ID.</param>
        /// <returns>The edit view or NotFound.</returns>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var student = await _context.Students.FindAsync(id);
            if (student == null) return NotFound();
            return View(student);
        }

        /// <summary>
        /// Processes the update of a student's profile information.
        /// </summary>
        /// <param name="id">The ID of the student.</param>
        /// <param name="student">The updated student data.</param>
        /// <returns>Redirect to Index on success, or the edit view on failure.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Student student)
        {
            if (id != student.StudentID) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(student);
                await _context.SaveChangesAsync();
                await _hubContext.Clients.All.SendAsync("ReceiveRefresh");
                return RedirectToAction(nameof(Index));
            }
            return View(student);
        }

        /// <summary>
        /// Displays a confirmation view for deleting a student record.
        /// </summary>
        /// <param name="id">The student ID.</param>
        /// <returns>The deletion confirmation view or NotFound.</returns>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var student = await _context.Students.FindAsync(id);
            if (student == null) return NotFound();
            return View(student);
        }

        /// <summary>
        /// Confirms the deletion of a student record.
        /// </summary>
        /// <param name="id">The student ID to remove.</param>
        /// <returns>Redirect to Index after deletion.</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students.FindAsync(id);
            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveRefresh");
            return RedirectToAction(nameof(Index));
        }
    }
}
