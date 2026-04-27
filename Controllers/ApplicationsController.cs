using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using pms.Data;
using pms.Models;

namespace pms.Controllers
{
    /// <summary>
    /// Controller for managing student job applications.
    /// Primarily used by administrators to review and update application records.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class ApplicationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<pms.Hubs.RefreshHub> _hubContext;

        public ApplicationsController(ApplicationDbContext context, Microsoft.AspNetCore.SignalR.IHubContext<pms.Hubs.RefreshHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Displays a list of all job applications in the system.
        /// </summary>
        /// <returns>A view displaying the applications list.</returns>
        public async Task<IActionResult> Index()
        {
            return View(await _context.Applications.ToListAsync());
        }

        /// <summary>
        /// Shows the details of a specific job application.
        /// </summary>
        /// <param name="id">The application ID.</param>
        /// <returns>A view for the specific application or NotFound.</returns>
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var application = await _context.Applications.FindAsync(id);
            if (application == null) return NotFound();
            return View(application);
        }

        /// <summary>
        /// Renders the view to record a new application manually.
        /// </summary>
        /// <returns>The creation view.</returns>
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Processes the manual creation of a new application.
        /// </summary>
        /// <param name="application">The application data to save.</param>
        /// <returns>Redirect to Index on success, or the creation view on failure.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Application application)
        {
            if (ModelState.IsValid)
            {
                _context.Add(application);
                await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveRefresh");
                return RedirectToAction(nameof(Index));
            }
            return View(application);
        }

        /// <summary>
        /// Renders the edit view for an existing job application.
        /// </summary>
        /// <param name="id">The application ID to edit.</param>
        /// <returns>The edit view or NotFound.</returns>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var application = await _context.Applications.FindAsync(id);
            if (application == null) return NotFound();
            return View(application);
        }

        /// <summary>
        /// Processes the update of a job application's details.
        /// </summary>
        /// <param name="id">The ID of the application.</param>
        /// <param name="application">The updated application data.</param>
        /// <returns>Redirect to Index on success, or the edit view on failure.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Application application)
        {
            if (id != application.ApplicationID) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(application);
                await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveRefresh");
                return RedirectToAction(nameof(Index));
            }
            return View(application);
        }

        /// <summary>
        /// Displays a confirmation view for deleting a job application.
        /// </summary>
        /// <param name="id">The application ID to delete.</param>
        /// <returns>The deletion confirmation view or NotFound.</returns>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var application = await _context.Applications.FindAsync(id);
            if (application == null) return NotFound();
            return View(application);
        }

        /// <summary>
        /// Confirms the deletion of a job application record.
        /// </summary>
        /// <param name="id">The application ID to remove.</param>
        /// <returns>Redirect to Index after deletion.</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var application = await _context.Applications.FindAsync(id);
            _context.Applications.Remove(application);
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveRefresh");
            return RedirectToAction(nameof(Index));
        }
    }
}
