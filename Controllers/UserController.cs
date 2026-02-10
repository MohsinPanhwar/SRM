using System;
using System.Linq;
using System.Web.Mvc;
using SRM.Data;
using SRM.Models;

namespace SRM.Controllers
{
    [Authorize] // any logged-in user
    public class UserController : Controller
    {
        private readonly AppDbContext _db = new AppDbContext();

        // GET: Manage Profile
        public ActionResult ManageUser()
        {
            var pno = Session["AgentPno"] as string;
            if (string.IsNullOrEmpty(pno))
                return RedirectToAction("Login", "Account");

            var agent = _db.agent.FirstOrDefault(a => a.Pno == pno);
            if (agent == null)
                return HttpNotFound();

            return View("~/Views/SystemSetup/ManageUser.cshtml", agent);
        }

        // POST: Update profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ManageUser(Agent model)
        {
            if (!ModelState.IsValid)
                return View("~/Views/SystemSetup/ManageUser.cshtml", model);

            try
            {
                var agent = _db.agent.FirstOrDefault(a => a.Pno == model.Pno);
                if (agent == null)
                    return HttpNotFound();

                agent.Name = model.Name;
                agent.Email = model.Email;
                agent.Mobile = model.Mobile;
                agent.LastUpdate = DateTime.UtcNow;

                _db.SaveChanges();

                TempData["Success"] = "Profile updated successfully!";
                return RedirectToAction("ManageUser"); // must redirect
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error updating profile: " + ex.Message);
                return View("~/Views/SystemSetup/ManageUser.cshtml", model); // only on error
            }
        }

        // POST: Change password
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var pno = Session["AgentPno"] as string;
            if (string.IsNullOrEmpty(pno))
                return RedirectToAction("Login", "Account");

            var agent = _db.agent.FirstOrDefault(a => a.Pno == pno);
            if (agent == null)
                return HttpNotFound();

            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword))
            {
                ModelState.AddModelError("", "Please provide both current and new password.");
                return RedirectToAction("ManageUser");
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "New passwords do not match.");
                return RedirectToAction("ManageUser");
            }

            if (!VerifyPassword(currentPassword, agent.Password))
            {
                ModelState.AddModelError("", "Current password is incorrect.");
                return RedirectToAction("ManageUser");
            }

            agent.Password = HashPassword(newPassword);
            agent.LastUpdate = DateTime.UtcNow;
            _db.SaveChanges();

            TempData["Success"] = "Password changed successfully!";
            return RedirectToAction("ManageUser");
        }

        #region Helpers

        private string HashPassword(string password)
        {
            using (var sha256 = new System.Security.Cryptography.SHA256Managed())
            {
                var salt = new byte[16];
                using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
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
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
