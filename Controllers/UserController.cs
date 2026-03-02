using System;
using System.Linq;
using System.Web.Mvc;
using SRM.Data;
using SRM.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;

namespace SRM.Controllers
{
    [Authorize]
    public class UserController : BaseController
    {
        private readonly AppDbContext _db = new AppDbContext();

        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2)
        };

        // GET: ManageUser
        public ActionResult ManageUser()
        {
            var pno = Session["AgentPno"] as string;
            if (string.IsNullOrEmpty(pno))
                return RedirectToAction("Login", "Account");

            int? agentProgramId = Session["AgentProgramId"] as int?;

            // 1. Fetch filtered users
            var filteredUsers = _db.agent
                .Where(a => !agentProgramId.HasValue || a.ProgramId == agentProgramId)
                .ToList();

            // 2. Fetch and sort roles
            var rolesInProgram = agentProgramId.HasValue
                ? _db.Role.Where(r => r.program_Id == agentProgramId).ToList()
                : _db.Role.ToList();

            var sortedRoles = rolesInProgram
                .OrderByDescending(r => r.Role_Name.Contains("Admin"))
                .ThenBy(r => r.Role_Name)
                .ToList();

            // 3. Sort users by role hierarchy
            var allUsers = filteredUsers
                .OrderBy(a => sortedRoles.FindIndex(r => r.Role_Id == a.RoleId))
                .ThenBy(a => a.Name)
                .ToList();

            // 4. Populate ViewBags
            ViewBag.RoleListSource = sortedRoles;
            ViewBag.RoleList = new SelectList(sortedRoles, "Role_Id", "Role_Name");
            ViewBag.GroupList = _db.groups.Select(g => new SelectListItem { Value = g.gid.ToString(), Text = g.gname }).ToList();

            // Filtered programs (for table/display context)
            var filteredPrograms = agentProgramId.HasValue
                ? _db.Programs.Where(p => p.Program_Id == agentProgramId).ToList()
                : _db.Programs.ToList();

            ViewBag.Programs = filteredPrograms
                .Select(p => new SelectListItem { Value = p.Program_Id.ToString(), Text = p.Program_Name })
                .ToList();

            // All programs (for the form dropdown — always unfiltered)
            ViewBag.AllPrograms = _db.Programs
                .Select(p => new SelectListItem { Value = p.Program_Id.ToString(), Text = p.Program_Name })
                .ToList();

            ViewBag.WorkAreas = _db.Locations.Select(l => l.Location_Description).Distinct().OrderBy(x => x).ToList();
            ViewBag.Operators = _db.agent.Where(a => !string.IsNullOrEmpty(a.MobileOperator)).Select(a => a.MobileOperator).Distinct().ToList();

            ViewBag.ProgramName = agentProgramId.HasValue
                ? _db.Programs.FirstOrDefault(p => p.Program_Id == agentProgramId)?.Program_Name ?? "Program"
                : "All Programs";

            return View("~/Views/SystemSetup/ManageUser.cshtml", allUsers);
        }

        [OutputCache(Duration = 3600, VaryByParam = "pno")]
        public async Task<ActionResult> GetUserImage(string pno)
        {
            if (!string.IsNullOrEmpty(pno))
            {
                try
                {
                    string url = $"https://systemsupport.piac.com.pk/admin/AgentImages/{pno}.jpg";
                    var response = await _httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var bytes = await response.Content.ReadAsByteArrayAsync();
                        return File(bytes, "image/jpeg");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error: " + ex.Message);
                }
            }

            var grey = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAACMAAAAjCAIAAAC0Xo7tAAAAGklEQVR42mNk+M9Qz0BFAAIAAf//AzAEAADkAgQBHON6AAAAAElFTkSuQmCC"
            );
            return File(grey, "image/png");
        }

        // POST: ManageUser (Save/Update)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ManageUser(Agent model)
        {
            try
            {
                bool isCreateMode = Request.Form["IsNewUser"] == "true";
                var agent = _db.agent.FirstOrDefault(a => a.Pno == model.Pno);

                if (isCreateMode && agent != null)
                    return Json(new { success = false, message = "Duplicate Entry: PNo " + model.Pno + " already exists." });

                bool isNewRecord = (agent == null);
                if (isNewRecord) agent = new Agent { Pno = model.Pno };

                agent.Name = model.Name;
                agent.Email = model.Email;
                agent.Mobile = model.Mobile;
                agent.RoleId = model.RoleId;
                agent.ProgramId = model.ProgramId;
                agent.MobileOperator = model.MobileOperator;
                agent.WorkArea = model.WorkArea;
                agent.UserType = Request.Form["UserType"];
                agent.Status = Request.Form["Status"];
                agent.LastUpdate = DateTime.Now;
                agent.IsAdministrator = Request.Form["IsAdministrator"] ?? "N";

                if (model.RoleId != null)
                {
                    var selectedRole = _db.Role.FirstOrDefault(r => r.Role_Id == model.RoleId);
                    if (selectedRole != null)
                        agent.Privilege = selectedRole.Privilege.ToString();
                }

                if (agent.UserType == "G")
                {
                    string selectedGid = Request.Form["GId"];
                    if (!string.IsNullOrEmpty(selectedGid)) agent.Gid = int.Parse(selectedGid);
                }
                else { agent.Gid = null; }

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
            var currentUserPno = Session["AgentPno"] as string;
            if (string.IsNullOrEmpty(currentUserPno))
                return Json(new { success = false, message = "Session expired." });

            var currentUser = _db.agent.FirstOrDefault(a => a.Pno == currentUserPno);
            bool isAuthorized = (currentUser != null && currentUser.IsAdministrator == "Y");

            if (!isAuthorized)
                return Json(new { success = false, message = "Unauthorized. Admin access required." });

            if (newPassword != confirmPassword)
                return Json(new { success = false, message = "Passwords do not match." });

            var agent = _db.agent.FirstOrDefault(a => a.Pno == Pno);
            if (agent == null) return Json(new { success = false, message = "User not found." });

            agent.Password = HashPassword(newPassword);
            agent.LastUpdate = DateTime.Now;
            _db.SaveChanges();

            return Json(new { success = true, message = "Password updated successfully." });
        }

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
    }
}