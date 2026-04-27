using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using pms.Data;
using pms.Models;
using pms.Services;
using System.Security.Cryptography;
using System.Text;

namespace pms.Controllers
{
    /// <summary>
    /// Controller handling user accounts, authentication (Login/Logout), 
    /// and multi-step registration (SignUp) for both Students and Admins.
    /// </summary>
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<pms.Hubs.RefreshHub> _hubContext;

        public UsersController(ApplicationDbContext context, IJwtService jwtService, Microsoft.AspNetCore.SignalR.IHubContext<pms.Hubs.RefreshHub> hubContext)
        {
            _context = context;
            _jwtService = jwtService;
            _hubContext = hubContext;
        }


        // ─────────────────────────────────────────
        //  LOGIN
        // ─────────────────────────────────────────
        /// <summary>
        /// Renders the login page.
        /// </summary>
        [HttpGet]
        public IActionResult Login()
        {
            return View("~/Views/Home/Login.cshtml");
        }

        /// <summary>
        /// Processes login requests, validates credentials, sets session, and issues JWT cookies.
        /// </summary>
        /// <param name="username">Username or Email.</param>
        /// <param name="password">Plain text password (hashed comparison recommended in future).</param>
        /// <param name="role">Selected login role.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string role)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(role))
            {
                ViewBag.Error = "All fields are required.";
                return View("~/Views/Home/Login.cshtml");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u =>
                (u.Username == username || u.Email == username) &&
                u.Role.ToLower() == role.ToLower() &&
                u.IsActive);

            if (user == null)
            {
                ViewBag.Error = "Oops! Our security hamsters 🐹 couldn't find a match for those credentials. Please try again!";
                return View("~/Views/Home/Login.cshtml");
            }

            if (user.PasswordHash != password)
            {
                ViewBag.Error = "Oops! Our security hamsters 🐹 couldn't find a match for those credentials. Please try again!";
                return View("~/Views/Home/Login.cshtml");
            }

            TempData["UserID"] = user.UserID;
            TempData["Role"] = user.Role;

            HttpContext.Session.SetInt32("UserID", user.UserID);
            HttpContext.Session.SetString("Role", user.Role);

            // Generate JWT Token
            var token = _jwtService.GenerateToken(user.UserID, user.Username, user.Role);

            // Append JWT to Cookie
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps, // Dynamically set based on request
                SameSite = SameSiteMode.Lax, // More compatible for local network
                Expires = DateTime.Now.AddHours(1)
            };
            Response.Cookies.Append("JWT", token, cookieOptions);

            if (user.Role.ToLower() == "admin")
                return RedirectToAction("AdminDashboard", "Home");
            else if (user.Role.ToLower() == "student")
                return RedirectToAction("StudentDashboard", "Home");
            else
                return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Clears the user session and deletes the authentication JWT cookie.
        /// </summary>
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete("JWT");
            return RedirectToAction("Login", "Users");
        }

        // ─────────────────────────────────────────
        //  SIGN-UP – STEP 1 (Role + Credentials)
        // ─────────────────────────────────────────
        /// <summary>
        /// Renders the initial sign-up page (Step 1: Basic Credentials).
        /// </summary>
        [HttpGet]
        public IActionResult SignUp()
        {
            return View("~/Views/Home/SignUp.cshtml", new SignUpViewModel());
        }

        /// <summary>
        /// Processes the first step of sign-up and stores data in TempData.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SignUp(SignUpViewModel model)
        {
            if (!ModelState.IsValid)
                return View("~/Views/Home/SignUp.cshtml", model);

            // Check duplicate username / email
            bool exists = _context.Users.Any(u =>
                u.Username == model.Username || u.Email == model.Email);

            if (exists)
            {
                ModelState.AddModelError("", "A user with this username or email already exists.");
                return View("~/Views/Home/SignUp.cshtml", model);
            }

            // Store Step-1 data in TempData and route to Step 2
            TempData["SignUp_Username"] = model.Username;
            TempData["SignUp_Email"]    = model.Email;
            TempData["SignUp_Password"] = model.Password;
            TempData["SignUp_Role"]     = model.Role;

            if (model.Role.ToLower() == "student")
                return RedirectToAction("SignUpStudent");
            else
                return RedirectToAction("SignUpAdmin");
        }

        // ─────────────────────────────────────────
        //  SIGN-UP STUDENT – STEP 2
        // ─────────────────────────────────────────
        /// <summary>
        /// Renders the student-specific detail page (Step 2: Profile Info).
        /// </summary>
        [HttpGet]
        public IActionResult SignUpStudent()
        {
            // Validate TempData is present
            if (TempData.Peek("SignUp_Username") == null)
                return RedirectToAction("SignUp");

            TempData.Keep(); // keep so POST can read it
            return View("~/Views/Home/SignUpStudent.cshtml", new StudentSignUpViewModel
            {
                Username = TempData.Peek("SignUp_Username")?.ToString(),
                Email    = TempData.Peek("SignUp_Email")?.ToString(),
                Password = TempData.Peek("SignUp_Password")?.ToString()
            });
        }

        /// <summary>
        /// Finalizes student registration, creating both User and Student records.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignUpStudent(StudentSignUpViewModel model)
        {
            if (!ModelState.IsValid)
                return View("~/Views/Home/SignUpStudent.cshtml", model);

            // Check duplicate enrollment no
            bool enrollmentExists = _context.Students.Any(s => s.EnrollmentNo == model.EnrollmentNo);
            if (enrollmentExists)
            {
                ModelState.AddModelError("EnrollmentNo", "A student with this enrollment number already exists.");
                return View("~/Views/Home/SignUpStudent.cshtml", model);
            }

            // Create User
            var user = new User
            {
                Username     = model.Username,
                Email        = model.Email,
                PasswordHash = model.Password,
                Role         = "Student",
                IsActive     = true,
                CreatedAt    = DateTime.Now
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveRefresh");

            // Create linked Student record
            var student = new Student
            {
                UserID       = user.UserID,
                FullName     = model.FullName,
                EnrollmentNo = model.EnrollmentNo,
                Branch       = model.Branch,
                CGPA         = model.CGPA,
                PassingYear  = model.PassingYear,
                Phone        = model.Phone,
                ResumeURL    = model.ResumeURL
            };
            _context.Students.Add(student);
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveRefresh");

            // Log in the new user automatically
            HttpContext.Session.SetInt32("UserID", user.UserID);
            HttpContext.Session.SetString("Role", user.Role);

            // Generate JWT Token
            var token = _jwtService.GenerateToken(user.UserID, user.Username, user.Role);

            // Append JWT to Cookie
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.Now.AddHours(1)
            };
            Response.Cookies.Append("JWT", token, cookieOptions);

            TempData["Success"] = "Account created successfully! Welcome to the Placement Portal.";
            return RedirectToAction("StudentDashboard", "Home");
        }

        // ─────────────────────────────────────────
        //  SIGN-UP ADMIN – Secret Key Verification
        // ─────────────────────────────────────────
        /// <summary>
        /// Renders the admin registration page with secret key field.
        /// </summary>
        [HttpGet]
        public IActionResult SignUpAdmin()
        {
            if (TempData.Peek("SignUp_Username") == null)
                return RedirectToAction("SignUp");

            TempData.Keep();
            return View("~/Views/Home/SignUpAdmin.cshtml");
        }

        /// <summary>
        /// Validates the secret key and creates a new Admin account.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignUpAdmin(string secretKey, string username, string email, string password)
        {
            // Check if any admin already exists (case-insensitive)
            bool adminsExist = await _context.Users.AnyAsync(u => u.Role.ToLower() == "admin");

            if (adminsExist)
            {
                if (string.IsNullOrWhiteSpace(secretKey))
                {
                    ViewBag.Error = "An existing administrator is already registered. Please enter a valid secret key to Join the team.";
                    return View("~/Views/Home/SignUpAdmin.cshtml");
                }

                // Find the active secret key
                var activeKey = await _context.AdminSecretKeys
                    .Where(k => k.IsActive)
                    .OrderByDescending(k => k.CreatedAt)
                    .FirstOrDefaultAsync();

                if (activeKey == null)
                {
                    ViewBag.Error = "No active secret key exists. Please contact an existing admin to generate one.";
                    return View("~/Views/Home/SignUpAdmin.cshtml");
                }

                string inputHash = HashKey(secretKey);
                if (inputHash != activeKey.KeyHash)
                {
                    ViewBag.Error = "Invalid secret key. Please verify the key with your admin.";
                    return View("~/Views/Home/SignUpAdmin.cshtml");
                }
            }
            
            // Check if temp data or hidden fields failed
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Session session expired or invalid request. Please start sign-up again.";
                return RedirectToAction("SignUp");
            }

            // Create Admin User
            var user = new User
            {
                Username     = username,
                Email        = email,
                PasswordHash = password,
                Role         = "Admin",
                IsActive     = true,
                CreatedAt    = DateTime.Now
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveRefresh");

            // Log in automatically
            HttpContext.Session.SetInt32("UserID", user.UserID);
            HttpContext.Session.SetString("Role", user.Role);

            // Generate JWT Token
            var token = _jwtService.GenerateToken(user.UserID, user.Username, user.Role);

            // Append JWT to Cookie
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.Now.AddHours(1)
            };
            Response.Cookies.Append("JWT", token, cookieOptions);

            TempData["Success"] = "Admin account created successfully!";
            return RedirectToAction("AdminDashboard", "Home");
        }

        // ─────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────
        /// <summary>
        /// Generates a SHA256 hex string for verifying the admin secret key.
        /// </summary>
        public static string HashKey(string key)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}
