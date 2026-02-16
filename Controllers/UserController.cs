using System;
using System.Linq;
using System.Web.Mvc;
using SRM.Data;
using SRM.Models;
using System.Security.Claims;
using System.Collections.Generic;

namespace SRM.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly AppDbContext _db = new AppDbContext();

        public ActionResult ManageUser()
        {
            var pno = Session["AgentPno"] as string;
            if (string.IsNullOrEmpty(pno))
                return RedirectToAction("Login", "Account");

            // 1. Fetch ALL users for the server-side table
            var allUsers = _db.agent.ToList();

            // 2. Populate ViewBags for Dropdowns (Instantly available on load)
            ViewBag.RoleList = new SelectList(_db.Roles.ToList(), "Role_Id", "Role_Name");
            ViewBag.GroupList = _db.groups
          .Select(g => new SelectListItem { Value = g.gid.ToString(), Text = g.gname })
          .ToList();

            ViewBag.Programs = _db.Programs
                .Select(p => new SelectListItem { Value = p.Program_Id.ToString(), Text = p.Program_Name })
                .ToList();
            ViewBag.WorkAreas = _db.Locations.Select(l => l.Location_Description).Distinct().OrderBy(x => x).ToList();

            // Logic from your GetMobileOperators function
            ViewBag.Operators = _db.agent
                                .Where(a => a.MobileOperator != null && a.MobileOperator != "")
                                .Select(a => a.MobileOperator)
                                .Distinct()
                                .ToList();

            return View("~/Views/SystemSetup/ManageUser.cshtml", allUsers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ManageUser(Agent model)
        {
            try
            {
                bool isCreateMode = Request.Form["IsNewUser"] == "true";
                var agent = _db.agent.FirstOrDefault(a => a.Pno == model.Pno);

                if (isCreateMode && agent != null)
                {
                    return Json(new { success = false, message = "Duplicate Entry: PNo " + model.Pno + " already exists." });
                }

                bool isNewRecord = false;
                if (agent == null)
                {
                    agent = new Agent { Pno = model.Pno };
                    isNewRecord = true;
                }

                // 1. Basic Mapping
                agent.Name = model.Name;
                agent.Email = model.Email;
                agent.Mobile = model.Mobile;
                agent.RoleId = model.RoleId; // Saves the Role ID
                agent.ProgramId = model.ProgramId;
                agent.MobileOperator = model.MobileOperator;
                agent.WorkArea = model.WorkArea;
                agent.UserType = Request.Form["UserType"];
                agent.Status = Request.Form["Status"];
                agent.LastUpdate = DateTime.Now;

                // 2. PRIVILEGE FIX: Lookup privilege_id from Roles table
                if (model.RoleId != null)
                {
                    // We find the role to get its associated privilege_id
                    var selectedRole = _db.Roles.FirstOrDefault(r => r.Role_Id == model.RoleId);
                    if (selectedRole != null)
                    {
                        // Save the role's privilege_id into the Agent's Privilege column
                        // Convert the integer ID to a string to match the Agent model property type
                        agent.Privilege = selectedRole.Privilege_Id.ToString();
                    }
                }

                // 3. Set IsAdministrator based on form value
                string isAdminValue = Request.Form["IsAdministrator"];
                agent.IsAdministrator = isAdminValue ?? "N";

                // 4. Group ID Logic
                if (agent.UserType == "U")
                {
                    agent.Gid = null;
                }
                else
                {
                    string selectedGid = Request.Form["GId"];
                    if (!string.IsNullOrEmpty(selectedGid)) agent.Gid = int.Parse(selectedGid);
                }

                if (isNewRecord) _db.agent.Add(agent);

                _db.SaveChanges();

                return Json(new { success = true, isNew = isNewRecord });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetUserByPno(string pno)
        {
            var agent = _db.agent.FirstOrDefault(x => x.Pno == pno);
            if (agent != null)
            {
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        agent.Name,
                        agent.Email,
                        agent.Mobile,
                        agent.MobileOperator,
                        agent.WorkArea,
                        agent.Privilege,
                        agent.RoleId,
                        agent.ProgramId,
                        agent.Status,
                        agent.UserType,
                        agent.IsAdministrator,
                        Gid = agent.Gid,
                        LastUpdate = agent.LastUpdate?.ToString("g")
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { success = false, message = "No record found." }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult DeleteUser(string pno)
        {
            try
            {
                var user = _db.agent.FirstOrDefault(u => u.Pno == pno);
                if (user == null) return Json(new { success = false, message = "User not found" });

                _db.agent.Remove(user);
                _db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ChangePassword(string Pno, string newPassword, string confirmPassword)
        {
            // ===== AUTHORIZATION CHECK: Verify current user is admin in database =====
            bool isAuthorized = false;

            // Get the current logged-in user's Pno from session
            var currentUserPno = Session["AgentPno"] as string;

            if (string.IsNullOrEmpty(currentUserPno))
            {
                return Json(new { success = false, message = "Session expired. Please log in again." });
            }

            // Check if current user is an admin in the database (IsAdministrator column)
            var currentUser = _db.agent.FirstOrDefault(a => a.Pno == currentUserPno);

            if (currentUser != null && currentUser.IsAdministrator == "Y")
            {
                isAuthorized = true;
            }

            // Fallback to Session if database check fails
            if (!isAuthorized && Session["IsAdmin"] != null)
            {
                var sessionValue = Session["IsAdmin"].ToString();
                isAuthorized = (sessionValue == "Y");
            }

            if (!isAuthorized)
                return Json(new { success = false, message = "Unauthorized. Admin access required." });

            if (newPassword != confirmPassword)
                return Json(new { success = false, message = "Passwords do not match." });

            var agent = _db.agent.FirstOrDefault(a => a.Pno == Pno);
            if (agent == null)
                return Json(new { success = false, message = "User not found." });

            agent.Password = HashPassword(newPassword);
            agent.LastUpdate = DateTime.Now;
            _db.SaveChanges();

            return Json(new { success = true, message = "Password updated successfully." });
        }

        // Password hashing using PBKDF2
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
    }
}