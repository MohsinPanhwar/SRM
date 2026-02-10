using System;
using System.Linq;
using System.Web.Mvc;
using System.Web;
using System.Security.Cryptography;
using SRM.Models;
using SRM.Data;
using SRM.Services;

namespace SRM.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly JwtTokenService _tokenService;
        private const string JWT_COOKIE_NAME = "SRM_JWT_TOKEN";

        public AccountController()
        {
            _context = new AppDbContext();
            // Initialize JWT Service with a secret key
            string secretKey = System.Configuration.ConfigurationManager.AppSettings["JwtSecretKey"] ?? "YourSuperSecretKeyThatShouldBeAt32Characters!";
            _tokenService = new JwtTokenService(secretKey, 24); // 24 hours
        }

        // GET: Account/Login
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string pno, string password, bool? rememberMe, string returnUrl)
        {

            if (string.IsNullOrEmpty(pno) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Personnel number and password are required.");
                return View();
            }

            try
            {
                // Find agent by Pno or Email
                var agent = _context.agent.FirstOrDefault(a =>
                    (a.Pno == pno || a.Email == pno) && a.Status == "A");

                if (agent == null || !VerifyPassword(password, agent.Password))
                {
                    ModelState.AddModelError("", "Invalid personnel number or password.");
                    return View();
                }

                // Generate JWT Token
                bool isAdmin = agent.IsAdministrator == "Y";
                string jwtToken = _tokenService.GenerateToken(
                    agent.Sno,
                    agent.Pno,
                    agent.Email,
                    agent.Privilege,
                    isAdmin
                );

                if (string.IsNullOrEmpty(jwtToken))
                {
                    ModelState.AddModelError("", "Failed to generate authentication token.");
                    return View();
                }
                bool remember = rememberMe ?? false;

                // Store JWT in secure cookie
                var cookie = new HttpCookie(JWT_COOKIE_NAME, jwtToken)
                {
                    HttpOnly = true,
                    Secure = false, // Set to true in production with HTTPS
                    Path = "/",
                    Expires = remember ? DateTime.Now.AddDays(30) : DateTime.Now.AddHours(24)
                };

                Response.Cookies.Add(cookie);
                System.Web.Security.FormsAuthentication.SetAuthCookie(agent.Pno, remember);

                // Update last login
                agent.LastLoginDateTime = DateTime.UtcNow;
                agent.LastLoginIp = GetClientIpAddress();
                agent.LastUpdate = DateTime.UtcNow;
                _context.SaveChanges();

                // Store user info in session for quick access
                Session["AgentId"] = agent.Sno;
                Session["AgentName"] = agent.Name;
                Session["AgentPno"] = agent.Pno;
                Session["IsAdmin"] = isAdmin;
                Session["Privilege"] = agent.Privilege;

                // Redirect to return URL or dashboard
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("home", "Home");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                ModelState.AddModelError("", "An error occurred during login. Please try again.");
                return View();
            }
        }

        // GET: Account/Register
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(string pno, string name, string email, string mobile, string password, string confirmPassword)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(pno))
                {
                    ModelState.AddModelError("pno", "Personnel number is required.");
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    ModelState.AddModelError("name", "Full name is required.");
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    ModelState.AddModelError("password", "Password is required.");
                }

                if (password != confirmPassword)
                {
                    ModelState.AddModelError("confirmPassword", "Passwords do not match.");
                }

                if (password != null && password.Length < 6)
                {
                    ModelState.AddModelError("password", "Password must be at least 6 characters.");
                }

                // Check if Pno already exists
                if (!string.IsNullOrWhiteSpace(pno) && _context.agent.Any(a => a.Pno == pno))
                {
                    ModelState.AddModelError("pno", "Personnel number already exists.");
                }

                // Check if email already registered
                if (!string.IsNullOrEmpty(email) && _context.agent.Any(a => a.Email == email))
                {
                    ModelState.AddModelError("email", "Email already registered.");
                }

                // If there are validation errors, return to view
                if (!ModelState.IsValid)
                {
                    return View();
                }

                // Create new agent
                var agent = new Agent
                {
                    Pno = pno.Trim(),
                    Name = name.Trim(),
                    Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                    Mobile = string.IsNullOrWhiteSpace(mobile) ? null : mobile.Trim(),
                    Password = HashPassword(password),
                    Status = "A",
                    IsAdministrator = "N",
                    UserType = "U",
                    CreateDateTime = DateTime.UtcNow,
                    LastUpdate = DateTime.UtcNow,
                    Privilege = "View"
                };

                _context.agent.Add(agent);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Registration Complete! Log in Now";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Register error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }

                ModelState.AddModelError("", $"An error occurred during registration: {ex.Message}");
                return View();
            }
        }

 
        [Authorize]
        [HttpGet]
        public ActionResult Logout()
        {
            // Clear session
            Session.Clear();
            Session.Abandon();

            // Remove authentication cookie if using FormsAuthentication
            if (System.Web.Security.FormsAuthentication.CookiesSupported)
            {
                var cookie = new HttpCookie(System.Web.Security.FormsAuthentication.FormsCookieName, "")
                {
                    Expires = DateTime.Now.AddYears(-1),
                    HttpOnly = true
                };
                Response.Cookies.Add(cookie);
            }

            // Clear all cookies
            foreach (var key in Request.Cookies.AllKeys)
            {
                var c = new HttpCookie(key) { Expires = DateTime.Now.AddDays(-1) };
                Response.Cookies.Add(c);
            }

            return RedirectToAction("Login", "Account");
        }

        // POST: Account/ChangePassword
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword))
            {
                ModelState.AddModelError("", "Current password and new password are required.");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "New passwords do not match.");
                return View();
            }

            if (newPassword.Length < 6)
            {
                ModelState.AddModelError("", "New password must be at least 6 characters.");
                return View();
            }

            try
            {
                // Get logged-in user's Pno from Session
                var userPno = Session["AgentPno"] as string;

                if (string.IsNullOrEmpty(userPno))
                {
                    ModelState.AddModelError("", "Session expired. Please login again.");
                    return RedirectToAction("Login");
                }

                // Find agent
                var agent = _context.agent.FirstOrDefault(a => a.Pno == userPno);

                if (agent == null)
                {
                    ModelState.AddModelError("", "User not found.");
                    return View();
                }

                // Verify current password
                if (!VerifyPassword(currentPassword, agent.Password))
                {
                    ModelState.AddModelError("", "Current password is incorrect.");
                    return View();
                }

                // Update password
                agent.Password = HashPassword(newPassword);
                agent.LastUpdate = DateTime.UtcNow;
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Password changed successfully.";
                return RedirectToAction("home", "Home");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ChangePassword error: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while changing password.");
                return View();
            }
        }

        // Helper: Check if user is authenticated via JWT
        private bool IsJwtAuthenticated()
        {
            if (Request.Cookies[JWT_COOKIE_NAME] == null)
                return false;

            string token = Request.Cookies[JWT_COOKIE_NAME].Value;
            return _tokenService.IsTokenValid(token);
        }

        // Password hashing using PBKDF2
        private string HashPassword(string password)
        {
            using (var sha256 = new SHA256Managed())
            {
                var salt = new byte[16];
                using (var rng = new RNGCryptoServiceProvider())
                {
                    rng.GetBytes(salt);
                }

                var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(password, salt, 10000);
                byte[] hash = pbkdf2.GetBytes(20);

                byte[] hashBytes = new byte[36];
                Array.Copy(salt, 0, hashBytes, 0, 16);
                Array.Copy(hash, 0, hashBytes, 16, 20);

                return Convert.ToBase64String(hashBytes);
            }
        }

        // Password verification
        private bool VerifyPassword(string password, string hash)
        {
            try
            {
                byte[] hashBytes = Convert.FromBase64String(hash);
                byte[] salt = new byte[16];
                Array.Copy(hashBytes, 0, salt, 0, 16);

                var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(password, salt, 10000);
                byte[] computedHash = pbkdf2.GetBytes(20);

                for (int i = 0; i < 20; i++)
                {
                    if (hashBytes[i + 16] != computedHash[i])
                    {
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        // Get client IP address
        private string GetClientIpAddress()
        {
            var ipAddress = HttpContext.Request.UserHostAddress;
            if (HttpContext.Request.ServerVariables["HTTP_X_FORWARDED_FOR"] != null)
            {
                ipAddress = HttpContext.Request.ServerVariables["HTTP_X_FORWARDED_FOR"].Split(',')[0];
            }
            return ipAddress ?? "Unknown";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context?.Dispose();
            }
            base.Dispose(disposing);
        }
    
    }
}