using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using pms.Data;
using Microsoft.AspNetCore.SignalR;
using pms.Models;

namespace pms.Controllers
{
    /// <summary>
    /// Controller for managing administrator activity logs.
    /// Provides CRUD operations for AdminLog records.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminLogsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<pms.Hubs.RefreshHub> _hubContext;

        public AdminLogsController(ApplicationDbContext context, Microsoft.AspNetCore.SignalR.IHubContext<pms.Hubs.RefreshHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Displays a list of all administrator logs.
        /// </summary>
        /// <returns>A view displaying the list of logs.</returns>
        public async Task<IActionResult> Index()
        {
            return View(await _context.AdminLogs.ToListAsync());
        }

        /// <summary>
        /// Displays the details of a specific admin log.
        /// </summary>
        /// <param name="id">The ID of the log to view.</param>
        /// <returns>A view for the specific log or NotFound.</returns>
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var log = await _context.AdminLogs.FindAsync(id);
            if (log == null) return NotFound();
            return View(log);
        }

        /// <summary>
        /// Renders the view to create a new admin log entry.
        /// </summary>
        /// <returns>The creation view.</returns>
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Processes the creation of a new admin log entry.
        /// </summary>
        /// <param name="log">The log object to create.</param>
        /// <returns>Redirect to Index on success, or the creation view on failure.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminLog log)
        {
            if (ModelState.IsValid)
            {
                _context.Add(log);
                await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveRefresh");
                return RedirectToAction(nameof(Index));
            }
            return View(log);
        }

        /// <summary>
        /// Renders the edit view for an existing admin log.
        /// </summary>
        /// <param name="id">The ID of the log to edit.</param>
        /// <returns>The edit view or NotFound.</returns>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var log = await _context.AdminLogs.FindAsync(id);
            if (log == null) return NotFound();
            return View(log);
        }

        /// <summary>
        /// Processes the update of an existing admin log entry.
        /// </summary>
        /// <param name="id">The ID of the log to update.</param>
        /// <param name="log">The updated log object.</param>
        /// <returns>Redirect to Index on success, or the edit view on failure.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AdminLog log)
        {
            if (id != log.LogID) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(log);
                await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveRefresh");
                return RedirectToAction(nameof(Index));
            }
            return View(log);
        }

        /// <summary>
        /// Displays a confirmation view for deleting an admin log entry.
        /// </summary>
        /// <param name="id">The ID of the log to delete.</param>
        /// <returns>The deletion confirmation view or NotFound.</returns>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var log = await _context.AdminLogs.FindAsync(id);
            if (log == null) return NotFound();
            return View(log);
        }

        /// <summary>
        /// Confirms the deletion of an admin log entry.
        /// </summary>
        /// <param name="id">The ID of the log to remove.</param>
        /// <returns>Redirect to Index after deletion.</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var log = await _context.AdminLogs.FindAsync(id);
            _context.AdminLogs.Remove(log);
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveRefresh");
            return RedirectToAction(nameof(Index));
        }
    }
}
