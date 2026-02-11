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

            var agent = _db.agent.FirstOrDefault(a => a.Pno == pno);
            if (agent == null)
                return HttpNotFound();

            // 🔥 INTEGRATION: Fetch Roles from DB for the dropdown
            // Mapping RoleId to ProgramId as requested
            var roles = _db.Roles.Select(r => new {
                r.Role_Id,
                r.Role_Name
            }).ToList();

            ViewBag.RoleList = new SelectList(roles, "RoleId", "RoleName");

            return View("~/Views/SystemSetup/ManageUser.cshtml", agent);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ManageUser(Agent model)
        {
            var agent = _db.agent.FirstOrDefault(a => a.Pno == model.Pno);

            if (agent != null)
            {
                // 🔥 INTEGRATION: ProgramId (The Role) update
                agent.ProgramId = model.ProgramId;

                agent.Name = model.Name;
                agent.Email = model.Email;
                agent.Mobile = model.Mobile;
                agent.MobileOperator = Request.Form["MobileOperator"];
                agent.Status = Request.Form["Status"];
                agent.UserType = Request.Form["UserType"];
                agent.LastUpdate = DateTime.Now;
                agent.WorkArea = Request.Form["WorkArea"];

                string selectedGid = Request.Form["GId"];
                if (!string.IsNullOrEmpty(selectedGid))
                {
                    agent.Gid = int.Parse(selectedGid);
                }

                _db.SaveChanges();

                TempData["Success"] = "User details updated successfully!";

                // 🔥 Re-populate dropdown before returning view
                var roles = _db.Roles.Select(r => new { r.Role_Id, r.Role_Name }).ToList();
                ViewBag.RoleList = new SelectList(roles, "RoleId", "RoleName", agent.ProgramId);

                return View("~/Views/SystemSetup/ManageUser.cshtml", agent);
            }
            else
            {
                TempData["Error"] = "User not found.";
                return View("~/Views/SystemSetup/ManageUser.cshtml", model);
            }
        }

        // ... Keep existing ChangePassword and GetUserByPno ...

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string Pno, string newPassword, string confirmPassword)
        {
            // 1. Extract the claim value from JWT
            var adminClaim = (User.Identity as ClaimsIdentity)?.FindFirst("IsAdmin")?.Value;

            // 2. Flexible Verification: 
            // Checks if JWT says "Y" or "True", OR checks the Session backup
            bool isAuthorized = (adminClaim == "Y" || adminClaim == "True") ||
                                (Session["IsAdmin"]?.ToString() == "True" || Session["IsAdmin"]?.ToString() == "Y");

            if (!isAuthorized)
            {
                // Debug hint: Includes the actual value found to help you see what's wrong
                TempData["Error"] = $"Unauthorized. Admin access required. (Found: {adminClaim ?? "None"})";
                return RedirectToAction("ManageUser");
            }

            // 3. Validation
            if (string.IsNullOrEmpty(newPassword) || newPassword != confirmPassword)
            {
                TempData["Error"] = "Passwords are empty or do not match.";
                return RedirectToAction("ManageUser");
            }

            // 4. Update Database
            var agent = _db.agent.FirstOrDefault(a => a.Pno == Pno);
            if (agent != null)
            {
                agent.Password = HashPassword(newPassword);
                agent.LastUpdate = DateTime.UtcNow;
                _db.SaveChanges();
                TempData["Success"] = "Password reset successfully for " + Pno;
            }
            else
            {
                TempData["Error"] = "User record not found.";
            }

            return RedirectToAction("ManageUser");
        }

        #region Helpers
        private string HashPassword(string password)
        {
            using (var sha256 = new System.Security.Cryptography.SHA256Managed())
            {
                var salt = new byte[16];
                using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider()) { rng.GetBytes(salt); }
                var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(password, salt, 10000);
                byte[] hash = pbkdf2.GetBytes(20);
                byte[] hashBytes = new byte[36];
                Array.Copy(salt, 0, hashBytes, 0, 16);
                Array.Copy(hash, 0, hashBytes, 16, 20);
                return Convert.ToBase64String(hashBytes);
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
                        Gid = agent.Gid,
                        LastUpdate = agent.LastUpdate?.ToString("g")
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { success = false, message = "No record found." }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetAllUsers()
        {
            var users = (from u in _db.agent
                         join r in _db.Roles on u.ProgramId equals r.Role_Id into roleJoin
                         from role in roleJoin.DefaultIfEmpty()
                         select new
                         {
                             u.Pno,
                             u.Name,
                             u.Email,
                             u.Mobile,
                             RoleName = role != null ? role.Role_Name : "No Role Assigned",
                             u.Status
                         })
                        .OrderByDescending(u => u.Pno)
                        .ToList();

            return Json(users, JsonRequestBehavior.AllowGet);
        }

[HttpGet]
        public JsonResult GetMobileOperators()
        {
            try
            {
                var operators = _db.agent
                    .Select(a => a.MobileOperator)
                    .Where(o => o != null && o != "")
                    .Distinct()
                    .ToList();

                return Json(operators, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new List<string> { "Error loading" }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetPrograms()
        {
            try
            {
                var programs = _db.Programs
                    .Select(p => new
                    {
                        Id = p.Program_Id,
                        Name = p.Program_Name
                    })
                    .ToList();

                return Json(programs, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult GetGroups()
        {
            try
            {
                var Groups = _db.groups
                    .Select(g=> new
                    {
                        GId =g.gid,
                        GName= g.gname
                    })
                    .ToList();

                return Json(Groups, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult GetWorkAreas()
        {
            try
            {
                var locations = _db.Locations
                    .Select(l => l.Location_Description) // Adjust 'LocationName' to your actual column name
                    .Where(l => !string.IsNullOrEmpty(l))
                    .Distinct()
                    .OrderBy(l => l)
                    .ToList();

                return Json(locations, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new List<string>(), JsonRequestBehavior.AllowGet);
            }
        }
        
        #endregion
    }
}