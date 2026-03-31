using SRM.Data;
using SRM.Models;
using SRM.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace SRM.Controllers
{
    public class RoleController : BaseController
    {
        private AppDbContext _db = new AppDbContext();

        // 1. Initial Page Load (Supports Super User "All Programs")
        public ActionResult ManageRole()
        {
            var vm = new ManageRoleVM();

            // Get program filter from session (Switcher value)
            int? agentProgramId = Session["AgentProgramId"] as int?;

            // Use AsQueryable to build the query dynamically
            var rolesQuery = _db.Role.AsQueryable();

            // If a specific program is selected (not "All"), apply the filter
            if (agentProgramId.HasValue && agentProgramId.Value > 0)
            {
                rolesQuery = rolesQuery.Where(r => r.program_Id == agentProgramId.Value);
            }

            vm.ExistingRoles = rolesQuery.ToList().Select(r => new ManageRoleVM
            {
                RoleId = r.Role_Id,
                RoleName = r.Role_Name,
                program_Id = r.program_Id,
                // Fetch the program name; fallback to "Global" if ID is 0 or null
                ProgramName = _db.Programs.FirstOrDefault(p => p.Program_Id == r.program_Id)?.Program_Name ?? "Shared/Global",
                // Count users: filter by program ONLY if a specific program is selected
                UserCount = _db.agent.Count(a => a.RoleId == r.Role_Id && (!agentProgramId.HasValue || agentProgramId == 0 || a.ProgramId == agentProgramId))
            }).ToList();

            // Populate Dropdown: Show all programs for Super User, or just the one restricted program
            var programs = (agentProgramId.HasValue && agentProgramId.Value > 0)
                ? _db.Programs.Where(p => p.Program_Id == agentProgramId).ToList()
                : _db.Programs.ToList();

            ViewBag.Programs = new SelectList(programs, "Program_Id", "Program_Name");
            ViewBag.ProgramId = agentProgramId;
            ViewBag.ProgramName = agentProgramId.HasValue && agentProgramId.Value > 0
                ? (programs.FirstOrDefault(p => p.Program_Id == agentProgramId)?.Program_Name)
                : "All Programs";

            return View("~/Views/SystemSetup/ManageRole.cshtml", vm);
        }

        // 2. AJAX: Get Specific Details
        [HttpGet]
        public JsonResult GetRoleDetails(int id)
        {
            int? agentProgramId = Session["AgentProgramId"] as int?;

            var role = _db.Role.FirstOrDefault(r => r.Role_Id == id);
            if (role == null)
                return Json(new { success = false, message = "Role not found" }, JsonRequestBehavior.AllowGet);

            // Access Check: If restricted to a program, ensure the role belongs to it
            if (agentProgramId.HasValue && agentProgramId.Value > 0 && role.program_Id != agentProgramId)
                return Json(new { success = false, message = "Access denied to this program's role" }, JsonRequestBehavior.AllowGet);

            string programName = _db.Programs.FirstOrDefault(p => p.Program_Id == role.program_Id)?.Program_Name ?? "Shared/Global";

            var privilegeList = (role.Privilege ?? "")
                .Split(',')
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList();

            var data = new
            {
                RoleId = role.Role_Id,
                RoleName = role.Role_Name,
                program_Id = role.program_Id,
                ProgramName = programName,
                // Privilege Mapping
                CanAddEditEngineer = privilegeList.Contains("AFE"),
                CanLogNewRequest = privilegeList.Contains("LNR"),
                CanViewForwardAny = privilegeList.Contains("FQR"),
                CanOnlyViewAny = privilegeList.Contains("VR"),
                CanViewForwardOwn = privilegeList.Contains("FOR"),
                CanAddEditGroups = privilegeList.Contains("MG"),
                CanAddNewIncident = privilegeList.Contains("IMA"),
                CanViewIncident = privilegeList.Contains("IMV"),
                CanLogNOC = privilegeList.Contains("LR"),
                CanViewOwnGroup = privilegeList.Contains("VOR"),
                CanReopenAny = privilegeList.Contains("URO"),
                CanViewEditMessage = privilegeList.Contains("MSG"),
                CanSendSMS = privilegeList.Contains("SMS"),
                CanHardware = privilegeList.Contains("RHR"),
                CanSendPassword = privilegeList.Contains("SPWD"),
                CanViewReports = privilegeList.Contains("VRPT"),
                CanAddEditAsset = privilegeList.Contains("ASS")
            };

            return Json(new { success = true, data = data }, JsonRequestBehavior.AllowGet);
        }

        // 3. AJAX: Save Role (Unified Create/Edit)
        [HttpPost]
        public JsonResult SaveRole(ManageRoleVM vm)
        {
            if (vm == null || string.IsNullOrWhiteSpace(vm.RoleName))
                return Json(new { success = false, message = "Invalid data: Role Name is required" });

            int? agentProgramId = Session["AgentProgramId"] as int?;

            // Priority: Session ID (if locked). Fallback: Dropdown ID (if Super User).
            int targetProgramId = (agentProgramId.HasValue && agentProgramId.Value > 0)
                                  ? agentProgramId.Value
                                  : (vm.program_Id ?? 0);

            if (targetProgramId == 0)
                return Json(new { success = false, message = "Please select a specific program for this role." });

            try
            {
                // Logic for Privilege CSV string creation
                var privileges = new List<string>();
                if (vm.CanAddEditEngineer) privileges.Add("AFE");
                if (vm.CanLogNewRequest) privileges.Add("LNR");
                if (vm.CanViewForwardAny) privileges.Add("FQR");
                if (vm.CanOnlyViewAny) privileges.Add("VR");
                if (vm.CanViewForwardOwn) privileges.Add("FOR");
                if (vm.CanAddEditGroups) privileges.Add("MG");
                if (vm.CanAddNewIncident) privileges.Add("IMA");
                if (vm.CanViewIncident) privileges.Add("IMV");
                if (vm.CanLogNOC) privileges.Add("LR");
                if (vm.CanViewOwnGroup) privileges.Add("VOR");
                if (vm.CanReopenAny) privileges.Add("URO");
                if (vm.CanViewEditMessage) privileges.Add("MSG");
                if (vm.CanSendSMS) privileges.Add("SMS");
                if (vm.CanHardware) privileges.Add("RHR");
                if (vm.CanSendPassword) privileges.Add("SPWD");
                if (vm.CanViewReports) privileges.Add("VRPT");
                if (vm.CanAddEditAsset) privileges.Add("ASS");

                string privilegeCsv = string.Join(",", privileges);

                Role role;
                if (vm.RoleId == 0) // CREATE
                {
                    // Check for duplicates in the target program
                    if (_db.Role.Any(r => r.Role_Name.ToLower() == vm.RoleName.ToLower() && r.program_Id == targetProgramId))
                        return Json(new { success = false, message = "This role name already exists in the selected program." });

                    role = new Role
                    {
                        Role_Name = vm.RoleName.Trim(),
                        Privilege = privilegeCsv,
                        program_Id = targetProgramId
                    };
                    _db.Role.Add(role);
                }
                else // UPDATE
                {
                    role = _db.Role.Find(vm.RoleId);
                    if (role == null) return Json(new { success = false, message = "Role not found" });

                    // Ensure Super User isn't editing a restricted role they shouldn't see
                    if (agentProgramId.HasValue && agentProgramId.Value > 0 && role.program_Id != agentProgramId)
                        return Json(new { success = false, message = "Access denied" });

                    role.Role_Name = vm.RoleName.Trim();
                    role.Privilege = privilegeCsv;
                    role.program_Id = targetProgramId;
                }

                _db.SaveChanges();
                return Json(new { success = true, role = new { RoleId = role.Role_Id, RoleName = role.Role_Name } });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Database Error: " + ex.Message });
            }
        }

        // 4. AJAX: Delete Role
        [HttpPost]
        public JsonResult DeleteRole(int id)
        {
            int? agentProgramId = Session["AgentProgramId"] as int?;
            var role = _db.Role.Find(id);

            if (role == null)
                return Json(new { success = false, message = "Role not found." });

            if (agentProgramId.HasValue && agentProgramId.Value > 0 && role.program_Id != agentProgramId)
                return Json(new { success = false, message = "Access denied" });

            // Ensure no users are currently using this role before deletion
            if (_db.agent.Any(a => a.RoleId == id && (!agentProgramId.HasValue || a.ProgramId == agentProgramId)))
                return Json(new { success = false, message = "Cannot delete: Users are currently assigned to this role." });

            _db.Role.Remove(role);
            _db.SaveChanges();

            return Json(new { success = true });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}