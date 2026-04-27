using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using pms.Data;
using pms.Models;

namespace pms.Controllers
{
    /// <summary>
    /// Controller for managing recruiting companies.
    /// Used by administrators to maintain company records.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class CompaniesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<pms.Hubs.RefreshHub> _hubContext;

        public CompaniesController(ApplicationDbContext context, Microsoft.AspNetCore.SignalR.IHubContext<pms.Hubs.RefreshHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Displays a list of all registered companies.
        /// </summary>
        /// <returns>A view with the list of companies.</returns>
        public async Task<IActionResult> Index()
        {
            return View(await _context.Companies.ToListAsync());
        }

        /// <summary>
        /// Shows detailed information for a specific company.
        /// </summary>
        /// <param name="id">The company ID.</param>
        /// <returns>A view with company details or NotFound.</returns>
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var company = await _context.Companies.FindAsync(id);
            if (company == null) return NotFound();
            return View(company);
        }

        /// <summary>
        /// Renders the view to create a new company record.
        /// </summary>
        /// <returns>The creation view.</returns>
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Processes the creation of a new company entry.
        /// </summary>
        /// <param name="company">The company data.</param>
        /// <returns>Redirect to Index on success, or the creation view on failure.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Company company)
        {
            if (ModelState.IsValid)
            {
                _context.Add(company);
                await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveRefresh");
                return RedirectToAction(nameof(Index));
            }
            return View(company);
        }

        /// <summary>
        /// Renders the edit view for an existing company record.
        /// </summary>
        /// <param name="id">The company ID.</param>
        /// <returns>The edit view or NotFound.</returns>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var company = await _context.Companies.FindAsync(id);
            if (company == null) return NotFound();
            return View(company);
        }

        /// <summary>
        /// Processes the update of a company's information.
        /// </summary>
        /// <param name="id">The company ID.</param>
        /// <param name="company">The updated company data.</param>
        /// <returns>Redirect to Index on success, or the edit view on failure.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Company company)
        {
            if (id != company.CompanyID) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(company);
                await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveRefresh");
                return RedirectToAction(nameof(Index));
            }
            return View(company);
        }

        /// <summary>
        /// Displays a confirmation view for deleting a company record.
        /// </summary>
        /// <param name="id">The company ID.</param>
        /// <returns>The deletion confirmation view or NotFound.</returns>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var company = await _context.Companies.FindAsync(id);
            if (company == null) return NotFound();
            return View(company);
        }

        /// <summary>
        /// Confirms the deletion of a company record.
        /// </summary>
        /// <param name="id">The company ID to remove.</param>
        /// <returns>Redirect to Index after deletion.</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var company = await _context.Companies.FindAsync(id);
            _context.Companies.Remove(company);
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveRefresh");
            return RedirectToAction(nameof(Index));
        }
    }
}
