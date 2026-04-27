using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using pms.Data;
using pms.Models;

namespace pms.Controllers
{
    /// <summary>
    /// Controller for managing job postings.
    /// Used by administrators to create, update, and delete job openings.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class JobsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<pms.Hubs.RefreshHub> _hubContext;

        public JobsController(ApplicationDbContext context, Microsoft.AspNetCore.SignalR.IHubContext<pms.Hubs.RefreshHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Displays a list of all job postings.
        /// </summary>
        /// <returns>A view with the list of jobs.</returns>
        public async Task<IActionResult> Index()
        {
            return View(await _context.Jobs.ToListAsync());
        }

        /// <summary>
        /// Shows detailed information for a specific job posting.
        /// </summary>
        /// <param name="id">The job ID.</param>
        /// <returns>A view with job details or NotFound.</returns>
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var job = await _context.Jobs.FindAsync(id);
            if (job == null) return NotFound();
            return View(job);
        }

        /// <summary>
        /// Renders the view to create a new job posting.
        /// </summary>
        /// <returns>The creation view.</returns>
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Processes the creation of a new job posting.
        /// </summary>
        /// <param name="job">The job data.</param>
        /// <returns>Redirect to Index on success, or the creation view on failure.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Job job)
        {
            if (ModelState.IsValid)
            {
                _context.Add(job);
                await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveRefresh");
                return RedirectToAction(nameof(Index));
            }
            return View(job);
        }

        /// <summary>
        /// Renders the edit view for an existing job posting.
        /// </summary>
        /// <param name="id">The job ID.</param>
        /// <returns>The edit view or NotFound.</returns>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var job = await _context.Jobs.FindAsync(id);
            if (job == null) return NotFound();
            return View(job);
        }

        /// <summary>
        /// Processes the update of a job posting's information.
        /// </summary>
        /// <param name="id">The job ID.</param>
        /// <param name="job">The updated job data.</param>
        /// <returns>Redirect to Index on success, or the edit view on failure.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Job job)
        {
            if (id != job.JobID) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(job);
                await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveRefresh");
                return RedirectToAction(nameof(Index));
            }
            return View(job);
        }

        /// <summary>
        /// Displays a confirmation view for deleting a job posting.
        /// </summary>
        /// <param name="id">The job ID.</param>
        /// <returns>The deletion confirmation view or NotFound.</returns>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var job = await _context.Jobs.FindAsync(id);
            if (job == null) return NotFound();
            return View(job);
        }

        /// <summary>
        /// Confirms the deletion of a job posting record.
        /// </summary>
        /// <param name="id">The job ID to remove.</param>
        /// <returns>Redirect to Index after deletion.</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            _context.Jobs.Remove(job);
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveRefresh");
            return RedirectToAction(nameof(Index));
        }
    }
}
