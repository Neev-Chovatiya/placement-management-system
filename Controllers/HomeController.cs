using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using pms.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using ClosedXML.Excel;
using System.IO;

namespace pms.Controllers
{
    public class HomeController : Controller
    {
        private readonly pms.Data.ApplicationDbContext _context;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<pms.Hubs.RefreshHub> _hubContext;

        public HomeController(pms.Data.ApplicationDbContext context, Microsoft.AspNetCore.SignalR.IHubContext<pms.Hubs.RefreshHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }
    
        /// <summary>
        /// Renders the public homepage with real-time statistics.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var totalPlaced = await _context.Placements.CountAsync();
            var totalCompanies = await _context.Companies.CountAsync();
            var topPartners = await _context.Companies.OrderByDescending(c => c.VisitDate).Take(12).ToListAsync();

            var viewModel = new HomeViewModel
            {
                TotalStudentsPlaced = totalPlaced,
                TotalCompaniesVisited = totalCompanies,
                TopRecruitingPartners = topPartners
            };

            return View(viewModel);
        }



        /// <summary>
        /// Displays information about the placement cell and its activities.
        /// </summary>
        public IActionResult AboutPlacements()
        {
            return View();
        }

        /// <summary>
        /// Lists all recruiting partners for students to view.
        /// </summary>
        public async Task<IActionResult> Companies()
        {
            var companies = await _context.Companies.OrderBy(c => c.CompanyName).ToListAsync();
            return View(companies);
        }

        public IActionResult Privacy()
        {
            return View();
        }
        /// <summary>
        /// Entry point for administrative reports.
        /// </summary>
        [Authorize(Roles = "Admin")]
        public IActionResult Reports()
        {
            return View();
        }

        /// <summary>
        /// Generates visual and tabular reports based on placement data.
        /// </summary>
        /// <param name="type">Filtering type (placed, company, branch).</param>
        /// <param name="year">Placement year filter.</param>
        [Authorize(Roles = "Admin")]
        public IActionResult ReportCharts(string type = "placed", string year = "all")
        {
            ViewBag.ReportType = type;
            ViewBag.SelectedYear = year;

            var query = _context.Placements
                .Include(p => p.Student)
                .Include(p => p.Company)
                .Include(p => p.Job)
                .AsQueryable();

            if (year != "all" && int.TryParse(year, out int selectedYear))
            {
                query = query.Where(p => p.PlacementDate.Year == selectedYear);
            }

            return View(query.ToList());
        }

        /// <summary>
        /// Exports placement report data to a downloadable CSV file.
        /// </summary>
        [Authorize(Roles = "Admin")]
        public IActionResult DownloadCsvReport(string type = "placed", string year = "all")
        {
            var query = _context.Placements
                .Include(p => p.Student)
                .Include(p => p.Company)
                .Include(p => p.Job)
                .AsQueryable();

            if (year != "all" && int.TryParse(year, out int selectedYear))
            {
                query = query.Where(p => p.PlacementDate.Year == selectedYear);
            }

            var placements = query.ToList();
            var builder = new System.Text.StringBuilder();

            if (type == "placed")
            {
                builder.AppendLine("Student Name,Branch,Company,Package (LPA),Placement Year");
                foreach (var p in placements)
                    builder.AppendLine($"\"{p.Student?.FullName}\",\"{p.Student?.Branch}\",\"{p.Company?.CompanyName}\",{p.Package},{p.PlacementDate.Year}");
            }
            else if (type == "company")
            {
                builder.AppendLine("Company Name,Total Placed,Average Package (LPA)");
                var grouped = placements.GroupBy(p => p.Company?.CompanyName)
                    .Select(g => new { Company = g.Key, Count = g.Count(), AvgPackage = g.Average(p => p.Package) });
                foreach (var g in grouped)
                    builder.AppendLine($"\"{g.Company}\",{g.Count},{g.AvgPackage:F2}");
            }
            else if (type == "branch")
            {
                builder.AppendLine("Branch,Total Placed,Average Package (LPA)");
                var grouped = placements.GroupBy(p => p.Student?.Branch)
                    .Select(g => new { Branch = g.Key, Count = g.Count(), AvgPackage = g.Average(p => p.Package) });
                foreach (var g in grouped)
                    builder.AppendLine($"\"{g.Branch}\",{g.Count},{g.AvgPackage:F2}");
            }

            return File(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", $"{type}_report_{year}.csv");
        }

        /// <summary>
        /// Renders a printable version of placement reports.
        /// </summary>
        [Authorize(Roles = "Admin")]
        public IActionResult PrintReport(string type = "placed", string year = "all")
        {
            ViewBag.ReportType = type;
            ViewBag.SelectedYear = year;

            var query = _context.Placements
                .Include(p => p.Student)
                .Include(p => p.Company)
                .Include(p => p.Job)
                .AsQueryable();

            if (year != "all" && int.TryParse(year, out int selectedYear))
            {
                query = query.Where(p => p.PlacementDate.Year == selectedYear);
            }

            return View(query.ToList());
        }
        /// <summary>
        /// Displays a searchable and paginated register of all placements.
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PlacementsRegister(string year = "all", string branch = "all", string search = null, int page = 1, int pageSize = 10)
        {
            var query = _context.Placements
                .Include(p => p.Student)
                .Include(p => p.Company)
                .Include(p => p.Job)
                .AsQueryable();

            if (year != "all" && int.TryParse(year, out int selectedYear))
            {
                query = query.Where(p => p.PlacementDate.Year == selectedYear);
            }

            if (branch != "all")
            {
                query = query.Where(p => p.Student.Branch == branch);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => 
                    (p.Student != null && p.Student.FullName.Contains(search)) || 
                    (p.Company != null && p.Company.CompanyName.Contains(search)) || 
                    p.Package.ToString().Contains(search) ||
                    (p.Student != null && p.Student.Branch.Contains(search)));
            }

            var model = new PlacementsRegisterViewModel
            {
                Placements = await PaginatedList<Placement>.CreateAsync(query.OrderByDescending(p => p.PlacementDate), page, pageSize),
                SelectedYear = year,
                SelectedBranch = branch,
                SearchQuery = search
            };
            return View(model);
        }

        /// <summary>
        /// Renders the composed notification view for a specific student.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SendNotification(int studentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return NotFound();

            ViewBag.StudentName = student.FullName;
            ViewBag.EnrollmentNo = student.EnrollmentNo;
            return View(new Notification { StudentID = studentId });
        }

        /// <summary>
        /// Processes and sends the notification to the student.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendNotification(Notification notification)
        {
            ModelState.Remove("Student");

            if (ModelState.IsValid)
            {
                notification.CreatedAt = DateTime.Now;
                notification.IsRead = false;
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                await _hubContext.Clients.All.SendAsync("ReceiveRefresh");
                TempData["Success"] = "Notification sent successfully!";
                return RedirectToAction("ManageStudents");
            }
            return View(notification);
        }

        /// <summary>
        /// Shows detailed info for a single placement record.
        /// </summary>
        [Authorize(Roles = "Admin")]
        public IActionResult PlacementDetails(int id)
        {
            var placement = _context.Placements
                .Include(p => p.Student)
                .Include(p => p.Company)
                .Include(p => p.Job)
                .FirstOrDefault(p => p.PlacementID == id);

            if (placement == null)
            {
                return NotFound();
            }

            return View(placement);
        }

        /// <summary>
        /// Exports the full placement register to an Excel (.xlsx) file using ClosedXML.
        /// </summary>
        [Authorize(Roles = "Admin")]
        public IActionResult DownloadRegister(string year = "all", string branch = "all", string companyId = "all")
        {
            var query = _context.Placements
                .Include(p => p.Student)
                .Include(p => p.Company)
                .Include(p => p.Job)
                .AsQueryable();

            if (year != "all" && int.TryParse(year, out int selectedYear))
                query = query.Where(p => p.PlacementDate.Year == selectedYear);

            if (branch != "all")
                query = query.Where(p => p.Student.Branch == branch);

            if (companyId != "all" && int.TryParse(companyId, out int selectedCompanyId))
                query = query.Where(p => p.CompanyID == selectedCompanyId);

            var placements = query.ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Placements Register");
                var currentRow = 1;

                worksheet.Cell(currentRow, 1).Value = "Student Name";
                worksheet.Cell(currentRow, 2).Value = "Enrollment No";
                worksheet.Cell(currentRow, 3).Value = "Branch";
                worksheet.Cell(currentRow, 4).Value = "Passing Year";
                worksheet.Cell(currentRow, 5).Value = "CGPA";
                worksheet.Cell(currentRow, 6).Value = "Company Name";
                worksheet.Cell(currentRow, 7).Value = "Job Role";
                worksheet.Cell(currentRow, 8).Value = "Package (LPA)";
                worksheet.Cell(currentRow, 9).Value = "Placement Date";

                var rngTable = worksheet.Range("A1:I1");
                rngTable.Style.Font.Bold = true;
                rngTable.Style.Fill.BackgroundColor = XLColor.AirForceBlue;
                rngTable.Style.Font.FontColor = XLColor.White;

                foreach (var p in placements)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = p.Student?.FullName ?? "N/A";
                    worksheet.Cell(currentRow, 2).Value = p.Student?.EnrollmentNo ?? "N/A";
                    worksheet.Cell(currentRow, 3).Value = p.Student?.Branch ?? "N/A";
                    worksheet.Cell(currentRow, 4).Value = p.Student?.PassingYear.ToString() ?? "N/A";
                    worksheet.Cell(currentRow, 5).Value = p.Student?.CGPA.ToString() ?? "N/A";
                    worksheet.Cell(currentRow, 6).Value = p.Company?.CompanyName ?? "N/A";
                    worksheet.Cell(currentRow, 7).Value = p.Job?.JobRole ?? "N/A";
                    worksheet.Cell(currentRow, 8).Value = p.Package;
                    worksheet.Cell(currentRow, 9).Value = p.PlacementDate.ToString("yyyy-MM-dd");
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Placements_Register.xlsx");
                }
            }
        }
        /// <summary>
        /// Administrative interface for viewing and filtering all student applications.
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApplicationsManagement(int? jobId, string search = null, int page = 1, int pageSize = 10)
        {
            var query = _context.Applications
                .Include(a => a.Student)
                .Include(a => a.Job)
                .ThenInclude(j => j.Company)
                .AsQueryable();

            if (jobId.HasValue)
            {
                query = query.Where(a => a.JobID == jobId.Value);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(a => 
                    a.Student.FullName.Contains(search) || 
                    a.Student.Branch.Contains(search) || 
                    a.Student.EnrollmentNo.Contains(search) ||
                    (a.Job != null && a.Job.Company != null && a.Job.Company.CompanyName.Contains(search)));
            }

            ViewBag.Jobs = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                _context.Jobs.Include(j => j.Company).Select(j => new {
                    JobID = j.JobID,
                    DisplayText = $"[{j.Company.CompanyName}] {j.JobRole}"
                }).ToList(), "JobID", "DisplayText", jobId);

            var model = new ManageApplicationsViewModel
            {
                Applications = await PaginatedList<Application>.CreateAsync(query.OrderByDescending(a => a.AppliedDate), page, pageSize)
            };
            return View(model);
        }

        /// <summary>
        /// Updates an application's status (e.g., Shortlisted, Selected).
        /// If "Selected", it creates or updates a corresponding Placement record.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateApplicationStatus(int applicationId, string status)
        {
            var application = await _context.Applications
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.ApplicationID == applicationId);

            if (application != null)
            {
                application.Status = status;

                // Handle placement record creation/removal
                if (status == "Selected")
                {
                    var existingPlacement = await _context.Placements
                        .FirstOrDefaultAsync(p => p.StudentID == application.StudentID);

                    if (existingPlacement == null)
                    {
                        var placement = new Placement
                        {
                            StudentID = application.StudentID,
                            JobID = application.JobID,
                            CompanyID = application.Job.CompanyID,
                            Package = application.Job.Package,
                            PlacementDate = DateTime.Now
                        };
                        _context.Placements.Add(placement);
                    }
                    else
                    {
                        // Update existing placement record
                        existingPlacement.JobID = application.JobID;
                        existingPlacement.CompanyID = application.Job.CompanyID;
                        existingPlacement.Package = application.Job.Package;
                        existingPlacement.PlacementDate = DateTime.Now;
                        _context.Placements.Update(existingPlacement);
                    }
                }
                else
                {
                    // If status is changed away from "Selected", remove the placement record if it exists
                    var existingPlacement = await _context.Placements
                        .FirstOrDefaultAsync(p => p.StudentID == application.StudentID && p.JobID == application.JobID);
                    
                    if (existingPlacement != null)
                    {
                        _context.Placements.Remove(existingPlacement);
                    }
                }

                await _context.SaveChangesAsync();
                await _hubContext.Clients.All.SendAsync("ReceiveRefresh");
                TempData["Success"] = $"Application status updated to {status}.";
            }
            return RedirectToAction("ApplicationsManagement");
        }

        /// <summary>
        /// Renders the job creation portal for administrators.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult ManageJobs_Create()
        {
            ViewBag.Companies = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Companies.ToList(), "CompanyID", "CompanyName");
            return View(new Job { LastApplyDate = DateTime.Now.AddDays(7) }); // Default apply date
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult ManageJobs_Create(Job job, string[] EligibleBranchesArray)
        {
            // Convert the array of selected branches from checkboxes into a comma-separated string for database storage.
            if (EligibleBranchesArray != null && EligibleBranchesArray.Length > 0)
            {
                job.EligibleBranch = string.Join(",", EligibleBranchesArray);
                ModelState.Remove("EligibleBranch");
            }

            // Remove navigation properties from validation to avoid unwanted ModelState errors during creation.
            ModelState.Remove("Company");
            ModelState.Remove("Applications");
            ModelState.Remove("Placements");

            if (ModelState.IsValid)
            {
                _context.Jobs.Add(job);
                _context.SaveChanges();
                _hubContext.Clients.All.SendAsync("ReceiveRefresh").Wait();
                // Instead of RedirectToAction("JobOpenings"), we reload the same page so they can see the success message
                TempData["Success"] = "Job created successfully.";
                return RedirectToAction("ManageJobs");
            }
            
            ViewBag.Companies = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Companies.ToList(), "CompanyID", "CompanyName");
            return View(job);
        }
        /// <summary>
        /// Displays the student's own profile page.
        /// </summary>
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> StudentProfile()
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            var student = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.UserID == userId);
            if (student == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(student);
        }

        /// <summary>
        /// Renders the student profile edit view.
        /// </summary>
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> EditStudentProfile()
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            var student = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.UserID == userId);
            if (student == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(student);
        }

        /// <summary>
        /// Processes updates to the student's profile, including photo upload.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> EditStudentProfile(Student student, IFormFile profilePicture)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            
            if (profilePicture != null && profilePicture.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    await profilePicture.CopyToAsync(ms);
                    student.ProfilePicture = ms.ToArray();
                }
                ModelState.Remove("profilePicture");
            }
            else
            {
                // Preserve existing picture if no new one uploaded
                var existingStudent = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.StudentID == student.StudentID);
                if (existingStudent != null)
                {
                    student.ProfilePicture = existingStudent.ProfilePicture;
                }
            }

            // Remove navigation properties from validation
            ModelState.Remove("User");
            ModelState.Remove("Applications");
            ModelState.Remove("Notifications");
            ModelState.Remove("Placement");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(student);
                    await _context.SaveChangesAsync();
                    await _hubContext.Clients.All.SendAsync("ReceiveRefresh");
                    TempData["Success"] = "Profile updated successfully!";
                    return RedirectToAction("StudentProfile");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Students.Any(e => e.StudentID == student.StudentID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(student);
        }
        public IActionResult Login()
        {
            return View();
        }
        /// <summary>
        /// Lists companies with pagination for the student view.
        /// </summary>
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> ViewCompanies(string search = null, int page = 1, int pageSize = 5)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserID == userId);
            if (student != null)
            {
                ViewBag.StudentName = student.FullName;
            }

            var companiesQuery = _context.Companies.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                companiesQuery = companiesQuery.Where(c => c.CompanyName.Contains(search) || c.Location.Contains(search) || c.Description.Contains(search));
            }

            var model = new ViewCompaniesViewModel
            {
                Companies = await PaginatedList<Company>.CreateAsync(companiesQuery.OrderBy(c => c.CompanyName), page, pageSize)
            };
            return View(model);
        }

        /// <summary>
        /// Lists available job openings filtered by eligibility (Branch, CGPA).
        /// </summary>
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> JobOpenings(string search = null, int? companyId = null, int page = 1, int pageSize = 5)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserID == userId);
            if (student == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.StudentName = student.FullName;

            // Get jobs the student has already applied to
            var appliedJobIds = await _context.Applications
                .Where(a => a.StudentID == student.StudentID)
                .Select(a => a.JobID)
                .ToListAsync();

            // Check if the student is already placed
            var placement = await _context.Placements
                .FirstOrDefaultAsync(p => p.StudentID == student.StudentID);

            var jobsQuery = _context.Jobs
                .Include(j => j.Company)
                .AsQueryable();

            // Filter by student branch
            if (!string.IsNullOrEmpty(student.Branch))
            {
                jobsQuery = jobsQuery.Where(j => string.IsNullOrEmpty(j.EligibleBranch) || j.EligibleBranch.Contains(student.Branch));
            }

            // Filter by company if companyId is provided
            if (companyId.HasValue)
            {
                jobsQuery = jobsQuery.Where(j => j.CompanyID == companyId.Value);
                var company = await _context.Companies.FindAsync(companyId.Value);
                ViewBag.FilteredCompanyName = company?.CompanyName;
                ViewBag.FilteredCompanyId = companyId.Value;
            }

            if (!string.IsNullOrEmpty(search))
            {
                jobsQuery = jobsQuery.Where(j =>
                    j.JobRole.Contains(search) ||
                    (j.Company != null && j.Company.CompanyName.Contains(search)) ||
                    (j.EligibleBranch != null && j.EligibleBranch.Contains(search)));
            }

            var model = new JobOpeningsViewModel
            {
                Jobs = await PaginatedList<Job>.CreateAsync(jobsQuery.OrderByDescending(j => j.LastApplyDate), page, pageSize),
                AppliedJobIds = appliedJobIds,
                PlacedPackage = placement?.Package,
                StudentCGPA = student.CGPA
            };

            return View(model);
        }

        /// <summary>
        /// Processes a job application from a student, enforcing academic and placement rules.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> ApplyForJob(int jobId)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserID == userId);
            if (student == null) return RedirectToAction("Login", "Users");

            var targetJob = await _context.Jobs.Include(j => j.Company).FirstOrDefaultAsync(j => j.JobID == jobId);
            if (targetJob == null) return NotFound();

            // ── Deadline restriction: Prevents students from applying after the last date ──
            if (DateTime.Now.Date > targetJob.LastApplyDate.Date)
            {
                TempData["Error"] = $"The deadline for this job ({targetJob.LastApplyDate:dd MMM yyyy}) has passed.";
                return RedirectToAction("JobOpenings");
            }

            // ── CGPA restriction: Ensures student meets the minimum academic criteria ──
            if (student.CGPA < targetJob.MinCGPA)
            {
                TempData["Error"] = $"Your CGPA ({student.CGPA}) is lower than the minimum required ({targetJob.MinCGPA}) for this job.";
                return RedirectToAction("JobOpenings");
            }

            // ── Branch restriction: Filters by eligible engineering streams ──
            if (!string.IsNullOrEmpty(targetJob.EligibleBranch) && (string.IsNullOrEmpty(student.Branch) || !targetJob.EligibleBranch.Contains(student.Branch)))
            {
                TempData["Error"] = $"Your branch ({student.Branch ?? "N/A"}) is not eligible for this job. Eligible branches: {targetJob.EligibleBranch}";
                return RedirectToAction("JobOpenings");
            }

            // ── Placement restriction: 2x Salary Rule ──
            // Placed students can only apply to roles offering at least twice their current package.
            var existingPlacement = await _context.Placements
                .FirstOrDefaultAsync(p => p.StudentID == student.StudentID);

            if (existingPlacement != null)
            {
                decimal requiredPackage = existingPlacement.Package * 2;
                if (targetJob.Package < requiredPackage)
                {
                    TempData["Error"] = $"You are already placed with a package of {existingPlacement.Package} LPA. " +
                        $"You can only apply to companies offering {requiredPackage} LPA or more (2× your current package). " +
                        $"{targetJob.Company?.CompanyName} offers {targetJob.Package} LPA, which does not qualify.";
                    return RedirectToAction("JobOpenings");
                }
            }

            // Prevent duplicate applications
            bool alreadyApplied = await _context.Applications
                .AnyAsync(a => a.StudentID == student.StudentID && a.JobID == jobId);

            if (alreadyApplied)
            {
                TempData["Error"] = "You have already applied for this job.";
                return RedirectToAction("JobOpenings");
            }

            var application = new Application
            {
                StudentID = student.StudentID,
                JobID = jobId,
                AppliedDate = DateTime.Now,
                Status = "Applied"
            };

            _context.Applications.Add(application);
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveRefresh");

            TempData["Success"] = "Application submitted successfully!";
            return RedirectToAction("JobOpenings");
        }
        [Authorize(Roles = "Student")]
        /// <summary>
        /// Renders the student dashboard with application stats and recent notifications.
        /// </summary>
        public async Task<IActionResult> StudentDashboard(int page = 1, int pageSize = 5)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserID == userId);
            if (student == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var totalJobs = await _context.Jobs.CountAsync();
            var applicationsSubmitted = await _context.Applications.CountAsync(a => a.StudentID == student.StudentID);
            var interviewCalls = await _context.Applications.CountAsync(a => a.StudentID == student.StudentID && (a.Status == "Shortlisted" || a.Status == "Interview"));

            var notificationsQuery = _context.Notifications
                .Where(n => n.StudentID == student.StudentID)
                .OrderByDescending(n => n.CreatedAt);

            var viewModel = new StudentDashboardViewModel
            {
                Student = student,
                TotalJobsAvailable = totalJobs,
                ApplicationsSubmitted = applicationsSubmitted,
                InterviewCalls = interviewCalls,
                Notifications = await PaginatedList<Notification>.CreateAsync(notificationsQuery, page, pageSize)
            };

            return View(viewModel);
        }
        /// <summary>
        /// Lists all jobs applied to by the current student.
        /// </summary>
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MyApplications()
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserID == userId);
            if (student == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.StudentName = student.FullName;

            var applications = await _context.Applications
                .Where(a => a.StudentID == student.StudentID)
                .Include(a => a.Job)
                    .ThenInclude(j => j.Company)
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync();

            return View(applications);
        }
        /// <summary>
        /// Shows the current student's placement success or summary.
        /// </summary>
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> PlacementStatus()
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserID == userId);
            if (student == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.StudentName = student.FullName;

            // Check if the student has a placement record
            var placement = await _context.Placements
                .Include(p => p.Company)
                .Include(p => p.Job)
                .FirstOrDefaultAsync(p => p.StudentID == student.StudentID);

            if (placement == null)
            {
                // Not placed yet — pass application summary for context
                var applications = await _context.Applications
                    .Where(a => a.StudentID == student.StudentID)
                    .Include(a => a.Job).ThenInclude(j => j.Company)
                    .OrderByDescending(a => a.AppliedDate)
                    .ToListAsync();

                ViewBag.TotalApplications = applications.Count;
                ViewBag.ShortlistedCount = applications.Count(a => a.Status == "Shortlisted" || a.Status == "Interview");
                ViewBag.BestApplication = applications
                    .Where(a => a.Status == "Shortlisted" || a.Status == "Interview")
                    .OrderByDescending(a => a.AppliedDate)
                    .FirstOrDefault();
            }

            return View(placement);
        }

        /// <summary>
        /// Administrative interface to manage the company database.
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageCompanies(string search = null, int page = 1, int pageSize = 10)
        {
            var companiesQuery = _context.Companies.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                companiesQuery = companiesQuery.Where(c => c.CompanyName.Contains(search) || c.Location.Contains(search) || c.Description.Contains(search));
            }

            var model = new ManageCompaniesViewModel
            {
                Companies = await PaginatedList<Company>.CreateAsync(companiesQuery.OrderBy(c => c.CompanyName), page, pageSize)
            };
            return View(model);
        }

        /// <summary>
        /// Renders the form to add a new recruiting company.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult AddCompany()
        {
            return View(new Company());
        }

        /// <summary>
        /// Processes the addition of a new company with its logo.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddCompany(Company company, IFormFile companyLogo)
        {
            if (companyLogo != null && companyLogo.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    await companyLogo.CopyToAsync(ms);
                    company.CompanyLogo = ms.ToArray();
                }
            }
            ModelState.Remove("Jobs");

            if (ModelState.IsValid)
            {
                _context.Companies.Add(company);
                _context.SaveChanges();
                _hubContext.Clients.All.SendAsync("ReceiveRefresh").Wait();
                TempData["Success"] = "Company added successfully.";
                return RedirectToAction("ManageCompanies");
            }
            return View(company);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult EditCompany(int id)
        {
            var company = _context.Companies.FirstOrDefault(c => c.CompanyID == id);
            if (company == null) return NotFound();
            return View(company);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditCompany(Company updatedCompany, IFormFile companyLogo)
        {
            ModelState.Remove("Jobs");
            ModelState.Remove("companyLogo");

            if (ModelState.IsValid)
            {
                var company = _context.Companies.Find(updatedCompany.CompanyID);
                if (company == null) return NotFound();

                if (companyLogo != null && companyLogo.Length > 0)
                {
                    using (var ms = new MemoryStream())
                    {
                        await companyLogo.CopyToAsync(ms);
                        company.CompanyLogo = ms.ToArray();
                    }
                }

                company.CompanyName = updatedCompany.CompanyName;
                company.Location = updatedCompany.Location;
                company.VisitDate = updatedCompany.VisitDate;
                company.Description = updatedCompany.Description;
                company.ContactPerson = updatedCompany.ContactPerson;
                company.ContactEmail = updatedCompany.ContactEmail;
                _context.SaveChanges();
                _hubContext.Clients.All.SendAsync("ReceiveRefresh").Wait();

                // Generate Gmail link for updated company
                if (!string.IsNullOrEmpty(company.ContactEmail))
                {
                    string subject = Uri.EscapeDataString("Profile Update: Partnership with MSU");
                    string body = Uri.EscapeDataString($@"Dear {company.ContactPerson},

This is to inform you that your company profile ( {company.CompanyName} ) has been updated in the MSU Placement Management System.

Regards,
MSU Placement Office");
                    TempData["MailUrl"] = $"https://mail.google.com/mail/?view=cm&fs=1&to={company.ContactEmail}&su={subject}&body={body}";
                }

                TempData["Success"] = "Company updated successfully.";
                return RedirectToAction("ManageCompanies");
            }
            return View(updatedCompany);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteCompany(int id)
        {
            var company = _context.Companies.Include(c => c.Jobs).FirstOrDefault(c => c.CompanyID == id);
            if (company != null)
            {
                if (company.Jobs.Any())
                {
                    TempData["Error"] = "Cannot delete company with active jobs. Please delete all jobs first.";
                    return RedirectToAction("ManageCompanies");
                }
                _context.Companies.Remove(company);
                _context.SaveChanges();
                _hubContext.Clients.All.SendAsync("ReceiveRefresh").Wait();
            }
            return RedirectToAction("ManageCompanies");
        }

        [Authorize(Roles = "Admin")]
        public IActionResult AdminDashboard()
        {
            var stats = new DashboardStatsViewModel
            {
                TotalStudents = _context.Students.Count(),
                TotalCompanies = _context.Companies.Count(),
                TotalJobs = _context.Jobs.Count(),
                TotalPlacedStudents = _context.Placements.Select(p => p.StudentID).Distinct().Count()
            };
            return View(stats);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageJobs(string search, int page = 1, int pageSize = 10)
        {
            var jobsQuery = _context.Jobs.Include(j => j.Company).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                jobsQuery = jobsQuery.Where(j => j.JobRole.Contains(search) || j.Company.CompanyName.Contains(search));
            }

            var model = new ManageJobsViewModel
            {
                Jobs = await PaginatedList<Job>.CreateAsync(jobsQuery.OrderByDescending(j => j.LastApplyDate), page, pageSize)
            };
            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Email is required.");
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                ModelState.AddModelError("", "Email not found.");
                return View();
            }

            // For simplicity in this demo environment, we redirect to reset page directly
            // In a production app, you would send a tokenized link via email
            return RedirectToAction("ResetPassword", new { email = email });
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string email)
        {
            if (string.IsNullOrEmpty(email)) return RedirectToAction("ForgotPassword");
            
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return RedirectToAction("ForgotPassword");

            ViewBag.Email = email;
            ViewBag.Role = user.Role;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string email, string newPassword, string confirmPassword, string enrollmentNo)
        {
            var user = await _context.Users.Include(u => u.Student).FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return RedirectToAction("ForgotPassword");

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match.");
            }

            if (user.Role == "Student")
            {
                if (user.Student == null || user.Student.EnrollmentNo != enrollmentNo)
                {
                    ModelState.AddModelError("", "Security Check: Enrollment Number is incorrect.");
                }
            }

            if (ModelState.IsValid)
            {
                // In a production app, use a proper password hasher
                user.PasswordHash = newPassword; 
                _context.Update(user);
                await _context.SaveChangesAsync();
                await _hubContext.Clients.All.SendAsync("ReceiveRefresh");
                TempData["Success"] = "Password reset successfully! Please login with your new password.";
                return RedirectToAction("Login");
            }

            ViewBag.Email = email;
            ViewBag.Role = user.Role;
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult EditJob(int id)
        {
            var job = _context.Jobs.Find(id);
            if (job == null) return NotFound();

            ViewBag.Companies = new SelectList(_context.Companies.OrderBy(c => c.CompanyName), "CompanyID", "CompanyName", job.CompanyID);
            return View(job);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditJob(Job job, string[] EligibleBranchesArray)
        {
            ModelState.Remove("Company");
            ModelState.Remove("Applications");
            ModelState.Remove("Placements");

            if (ModelState.IsValid)
            {
                if (EligibleBranchesArray != null && EligibleBranchesArray.Length > 0)
                {
                    job.EligibleBranch = string.Join(",", EligibleBranchesArray);
                }

                _context.Update(job);
                await _context.SaveChangesAsync();
                await _hubContext.Clients.All.SendAsync("ReceiveRefresh");
                TempData["Success"] = "Job opening updated successfully!";
                return RedirectToAction("ManageJobs");
            }

            ViewBag.Companies = new SelectList(_context.Companies.OrderBy(c => c.CompanyName), "CompanyID", "CompanyName", job.CompanyID);
            return View(job);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteJob(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job != null)
            {
                _context.Jobs.Remove(job);
                await _context.SaveChangesAsync();
                await _hubContext.Clients.All.SendAsync("ReceiveRefresh");
                TempData["Success"] = "Job opening deleted successfully.";
            }
            return RedirectToAction("ManageJobs");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageStudents(string search = null, string branch = null, decimal? minCGPA = null, int? passingYear = null, bool? isPlaced = null, int page = 1, int pageSize = 10)
        {
            var studentsQuery = _context.Students.Include(s => s.Placement).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                studentsQuery = studentsQuery.Where(s =>
                    s.FullName.Contains(search) ||
                    s.EnrollmentNo.Contains(search) ||
                    s.Branch.Contains(search));
            }

            if (!string.IsNullOrEmpty(branch))
            {
                studentsQuery = studentsQuery.Where(s => s.Branch == branch);
            }

            if (minCGPA.HasValue)
            {
                studentsQuery = studentsQuery.Where(s => s.CGPA >= minCGPA.Value);
            }

            if (passingYear.HasValue)
            {
                studentsQuery = studentsQuery.Where(s => s.PassingYear == passingYear.Value);
            }

            if (isPlaced.HasValue)
            {
                if (isPlaced.Value)
                    studentsQuery = studentsQuery.Where(s => s.Placement != null);
                else
                    studentsQuery = studentsQuery.Where(s => s.Placement == null);
            }

            var model = new ManageStudentsViewModel
            {
                Students = await PaginatedList<Student>.CreateAsync(studentsQuery.OrderBy(s => s.EnrollmentNo), page, pageSize)
            };
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteStudent(int id)
        {
            var student = _context.Students.Find(id);
            if (student != null)
            {
                _context.Students.Remove(student);
                _context.SaveChanges();
                _hubContext.Clients.All.SendAsync("ReceiveRefresh").Wait();
            }
            return RedirectToAction("ManageStudents");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult AddStudent()
        {
            return View(new Student());
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddStudent(Student student, string loginUsername, string loginPassword, string loginEmail)
        {
            ModelState.Remove("User");
            ModelState.Remove("Applications");
            ModelState.Remove("Notifications");
            ModelState.Remove("Placement");

            if (ModelState.IsValid)
            {
                // Use admin-supplied credentials or fall back to EnrollmentNo as default
                var username = !string.IsNullOrWhiteSpace(loginUsername)
                    ? loginUsername.Trim()
                    : student.EnrollmentNo;
                var password = !string.IsNullOrWhiteSpace(loginPassword)
                    ? loginPassword.Trim()
                    : student.EnrollmentNo; // default password = enrollment no
                var email = !string.IsNullOrWhiteSpace(loginEmail)
                    ? loginEmail.Trim()
                    : $"{student.EnrollmentNo.ToLower()}@MSU.edu";

                // Check if username or email already taken
                bool userExists = _context.Users.Any(u => u.Username == username || u.Email == email);
                if (userExists)
                {
                    ModelState.AddModelError("loginUsername", "This username or email is already taken. Please choose another.");
                    return View(student);
                }

                // Create linked User account
                var user = new pms.Models.User
                {
                    Username     = username,
                    Email        = email,
                    PasswordHash = password,
                    Role         = "Student",
                    IsActive     = true,
                    CreatedAt    = DateTime.Now
                };
                _context.Users.Add(user);
                _context.SaveChanges(); // get user.UserID

                student.UserID = user.UserID;
                _context.Students.Add(student);
                _context.SaveChanges();
                _hubContext.Clients.All.SendAsync("ReceiveRefresh").Wait();

                // Generate Gmail Compose URL for manual sending
                string subject = Uri.EscapeDataString("Welcome to MSU - Your Placement Portal Account");
                string body = Uri.EscapeDataString($@"Welcome, {student.FullName}!

An account has been created for you on the MSU Placement Management System.

Your login credentials are:
Username: {username}
Initial Password: {password}

Please log in at Your Portal URL and change your password from your profile settings.

Regards,
MSU Placement Office");

                string gmailUrl = $"https://mail.google.com/mail/?view=cm&fs=1&to={email}&su={subject}&body={body}";
                TempData["MailUrl"] = gmailUrl;
                TempData["Success"] = $"Student added! Click 'Send Credentials' at the top to notify the student.";

                return RedirectToAction("ManageStudents");
            }
            return View(student);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult EditStudent(int id)
        {
            var student = _context.Students.FirstOrDefault(s => s.StudentID == id);
            if (student == null) return NotFound();
            return View(student);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult EditStudent(Student updatedStudent)
        {
            ModelState.Remove("User");
            ModelState.Remove("Applications");
            ModelState.Remove("Notifications");
            ModelState.Remove("Placement");

            if (ModelState.IsValid)
            {
                var student = _context.Students.Find(updatedStudent.StudentID);
                if (student == null) return NotFound();
                student.FullName = updatedStudent.FullName;
                student.Branch = updatedStudent.Branch;
                student.PassingYear = updatedStudent.PassingYear;
                student.CGPA = updatedStudent.CGPA;
                student.Phone = updatedStudent.Phone;
                student.ResumeURL = updatedStudent.ResumeURL;
                _context.SaveChanges();
                _hubContext.Clients.All.SendAsync("ReceiveRefresh").Wait();
                TempData["Success"] = "Student updated successfully.";
                return RedirectToAction("ManageStudents");
            }
            return View(updatedStudent);
        }










        // ─────────────────────────────────────────
        //  CHANGE PASSWORD (Student Profile)
        // ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return RedirectToAction("Login", "Users");

            if (user.PasswordHash != currentPassword)
            {
                TempData["PwdError"] = "Current password is incorrect.";
                return RedirectToAction("StudentProfile");
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                TempData["PwdError"] = "New password must be at least 6 characters.";
                return RedirectToAction("StudentProfile");
            }

            if (newPassword != confirmPassword)
            {
                TempData["PwdError"] = "New passwords do not match.";
                return RedirectToAction("StudentProfile");
            }

            user.PasswordHash = newPassword;
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveRefresh");
            TempData["PwdSuccess"] = "Password changed successfully!";
            return RedirectToAction("StudentProfile");
        }

        // ─────────────────────────────────────────
        //  ADMIN SETTINGS – Secret Key Management
        // ─────────────────────────────────────────
        [Authorize(Roles = "Admin")]
        public IActionResult AdminSettings()
        {
            var latestKey = _context.AdminSecretKeys
                .Where(k => k.IsActive)
                .OrderByDescending(k => k.CreatedAt)
                .FirstOrDefault();

            ViewBag.LastKeyDate = latestKey?.CreatedAt;
            ViewBag.HasActiveKey = latestKey != null;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult GenerateSecretKey()
        {
            // Generate a cryptographically random plain-text key
            var plainKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24))
                            .Replace("=", "").Replace("+", "-").Replace("/", "_");

            // Deactivate all previous keys
            var oldKeys = _context.AdminSecretKeys.Where(k => k.IsActive).ToList();
            oldKeys.ForEach(k => k.IsActive = false);

            // Save new hashed key
            var newKey = new pms.Models.AdminSecretKey
            {
                KeyHash   = pms.Controllers.UsersController.HashKey(plainKey),
                CreatedAt = DateTime.Now,
                IsActive  = true
            };
            _context.AdminSecretKeys.Add(newKey);
            _context.SaveChanges();
            _hubContext.Clients.All.SendAsync("ReceiveRefresh").Wait();

            // Pass plain key once via TempData for one-time display
            TempData["GeneratedKey"] = plainKey;
            TempData["Success"] = "New secret key generated. Share it with the new admin immediately – it will not be shown again.";
            return RedirectToAction("AdminSettings");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
