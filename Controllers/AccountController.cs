using SRM.Data;
using SRM.Models;
using SRM.Services;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Web.Mvc;

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
            // Fallback for secret key
            string secretKey = System.Configuration.ConfigurationManager.AppSettings["JwtSecretKey"] ?? "YourSuperSecretKeyThatShouldBeAt32Characters!";
            _tokenService = new JwtTokenService(secretKey, 24);
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

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
                // 1. Normalize Input for "P" prefix flexibility
                // This allows 'P60987', 'p60987', and '60987' to all work.
                string searchInput = pno.Trim().ToUpper();
                string pnoWithoutP = searchInput.StartsWith("P") ? searchInput.Substring(1) : searchInput;
                string pnoWithP = searchInput.StartsWith("P") ? searchInput : "P" + searchInput;

                // 2. Find agent: Match raw input, prefixed version, or unprefixed version
                var agent = _context.agent
                    .FirstOrDefault(a =>
                        (a.Pno.ToUpper() == searchInput ||
                         a.Pno.ToUpper() == pnoWithP ||
                         a.Pno.ToUpper() == pnoWithoutP ||
                         a.Email.ToUpper() == searchInput)
                        && a.Status == "A");

                if (agent == null || !VerifyPassword(password, agent.Password))
                {
                    ModelState.AddModelError("", "Invalid personnel number or password.");
                    return View();
                }

                // 3. Generate JWT Token
                bool isAdmin = agent.IsAdministrator == "Y";
                string jwtToken = _tokenService.GenerateToken(
                    agent.Sno,
                    agent.Pno,
                    agent.Email,
                    agent.Privilege ?? string.Empty,
                    isAdmin
                );

                if (string.IsNullOrEmpty(jwtToken))
                {
                    ModelState.AddModelError("", "Failed to generate authentication token.");
                    return View();
                }

                // 4. Set Authentication Cookie
                bool remember = rememberMe ?? false;
                var cookie = new HttpCookie(JWT_COOKIE_NAME, jwtToken)
                {
                    HttpOnly = true,
                    Secure = Request.IsSecureConnection, // Better security practice
                    Path = "/",
                    Expires = remember ? DateTime.Now.AddDays(30) : DateTime.Now.AddHours(24)
                };

                Response.Cookies.Add(cookie);
                System.Web.Security.FormsAuthentication.SetAuthCookie(agent.Pno, remember);

                // 5. Update Audit Fields
                agent.LastLoginDateTime = DateTime.UtcNow;
                agent.LastLoginIp = GetClientIpAddress();
                agent.LastUpdate = DateTime.UtcNow;
                _context.SaveChanges();

                // 6. Manage Session Data
                Session["AgentId"] = agent.Sno;
                Session["AgentName"] = agent.Name;
                Session["AgentPno"] = agent.Pno;
                Session["IsAdmin"] = isAdmin ? "Y" : "N";
                Session["AgentProgramId"] = agent.ProgramId;

                // Determine Privileges (Role overrides Agent table default)
                string userPrivileges = agent.Privilege ?? "View";
                if (agent.RoleId.HasValue)
                {
                    var role = _context.Role.FirstOrDefault(r => r.Role_Id == agent.RoleId.Value);
                    if (role != null)
                    {
                        userPrivileges = role.Privilege ?? userPrivileges;
                    }
                }
                Session["UserPrivileges"] = userPrivileges;

                // 7. Redirection
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("home", "Home");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
                ModelState.AddModelError("", "An error occurred during login. Please try again.");
                return View();
            }
        }

        
        [Authorize]
        [HttpGet]
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();

            // Clear Forms Auth Cookie
            if (System.Web.Security.FormsAuthentication.CookiesSupported)
            {
                var faCookie = new HttpCookie(System.Web.Security.FormsAuthentication.FormsCookieName, "")
                {
                    Expires = DateTime.Now.AddYears(-1),
                    HttpOnly = true
                };
                Response.Cookies.Add(faCookie);
            }

            // Clear JWT and all other application cookies
            foreach (string key in Request.Cookies.AllKeys)
            {
                var c = new HttpCookie(key) { Expires = DateTime.Now.AddDays(-1) };
                Response.Cookies.Add(c);
            }

            return RedirectToAction("Login", "Account");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword))
            {
                ModelState.AddModelError("", "Current and new passwords are required.");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "New passwords do not match.");
                return View();
            }

            try
            {
                var userPno = Session["AgentPno"] as string;
                if (string.IsNullOrEmpty(userPno)) return RedirectToAction("Login");

                var agent = _context.agent.FirstOrDefault(a => a.Pno == userPno);
                if (agent == null)
                {
                    ModelState.AddModelError("", "User not found.");
                    return View();
                }

                if (!VerifyPassword(currentPassword, agent.Password))
                {
                    ModelState.AddModelError("", "Current password is incorrect.");
                    return View();
                }

                agent.Password = HashPassword(newPassword);
                agent.LastUpdate = DateTime.UtcNow;
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Password changed successfully.";
                return RedirectToAction("home", "Home");
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An error occurred while changing password.");
                return View();
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = new SHA256Managed())
            {
                var salt = new byte[16];
                using (var rng = new RNGCryptoServiceProvider())
                {
                    rng.GetBytes(salt);
                }

                var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000);
                byte[] hash = pbkdf2.GetBytes(20);

                byte[] hashBytes = new byte[36];
                Array.Copy(salt, 0, hashBytes, 0, 16);
                Array.Copy(hash, 0, hashBytes, 16, 20);

                return Convert.ToBase64String(hashBytes);
            }
        }

        private bool VerifyPassword(string password, string hash)
        {
            try
            {
                byte[] hashBytes = Convert.FromBase64String(hash);
                byte[] salt = new byte[16];
                Array.Copy(hashBytes, 0, salt, 0, 16);

                var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000);
                byte[] computedHash = pbkdf2.GetBytes(20);

                for (int i = 0; i < 20; i++)
                {
                    if (hashBytes[i + 16] != computedHash[i]) return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

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
            if (disposing) _context?.Dispose();
            base.Dispose(disposing);
        }
    }
}