using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using pms.Data;
using pms.Models;

namespace pms.Controllers
{
    /// <summary>
    /// Controller for managing student placement records.
    /// Tracks successful placements including company, student, and package details.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class PlacementsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<pms.Hubs.RefreshHub> _hubContext;

        public PlacementsController(ApplicationDbContext context, Microsoft.AspNetCore.SignalR.IHubContext<pms.Hubs.RefreshHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Displays a list of all successful placements.
        /// </summary>
        /// <returns>A view with the placements list.</returns>
        public async Task<IActionResult> Index()
        {
            return View(await _context.Placements.ToListAsync());
        }

        /// <summary>
        /// Shows detailed information for a specific placement.
        /// </summary>
        /// <param name="id">The placement ID.</param>
        /// <returns>A view with placement details or NotFound.</returns>
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var placement = await _context.Placements.FindAsync(id);
            if (placement == null) return NotFound();
            return View(placement);
        }

        /// <summary>
        /// Renders the view to manually create a placement record.
        /// </summary>
        /// <returns>The creation view.</returns>
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Processes the manual creation of a new placement record.
        /// </summary>
        /// <param name="placement">The placement data.</param>
        /// <returns>Redirect to Index on success, or the creation view on failure.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Placement placement)
        {
            if (ModelState.IsValid)
            {
                _context.Add(placement);
                await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveRefresh");
                return RedirectToAction(nameof(Index));
            }
            return View(placement);
        }

        /// <summary>
        /// Renders the edit view for an existing placement record.
        /// </summary>
        /// <param name="id">The placement ID.</param>
        /// <returns>The edit view or NotFound.</returns>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var placement = await _context.Placements.FindAsync(id);
            if (placement == null) return NotFound();
            return View(placement);
        }

        /// <summary>
        /// Processes the update of a placement's details.
        /// </summary>
        /// <param name="id">The placement ID.</param>
        /// <param name="placement">The updated placement record.</param>
        /// <returns>Redirect to Index on success, or the edit view on failure.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Placement placement)
        {
            if (id != placement.PlacementID) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(placement);
                await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveRefresh");
                return RedirectToAction(nameof(Index));
            }
            return View(placement);
        }

        /// <summary>
        /// Displays a confirmation view for deleting a placement record.
        /// </summary>
        /// <param name="id">The placement ID.</param>
        /// <returns>The deletion confirmation view or NotFound.</returns>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var placement = await _context.Placements.FindAsync(id);
            if (placement == null) return NotFound();
            return View(placement);
        }

        /// <summary>
        /// Confirms the deletion of a placement record.
        /// </summary>
        /// <param name="id">The placement ID to remove.</param>
        /// <returns>Redirect to Index after deletion.</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var placement = await _context.Placements.FindAsync(id);
            _context.Placements.Remove(placement);
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveRefresh");
            return RedirectToAction(nameof(Index));
        }
    }
}
