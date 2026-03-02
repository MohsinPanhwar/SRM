using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using SRM.Data;
using SRM.Models;
using SRM.Models.ViewModels;

namespace SRM.Controllers
{
    public class RoleController : BaseController
    {
        private AppDbContext _db = new AppDbContext();

        // 1. Initial Page Load (Shell)
        public ActionResult ManageRole()
        {
            var vm = new ManageRoleVM();

            // Get program filter from session
            int? agentProgramId = Session["AgentProgramId"] as int?;

            if (!agentProgramId.HasValue)
            {
                // No program assigned - show no roles
                vm.ExistingRoles = new List<ManageRoleVM>();
            }
            else
            {
                // Show ONLY roles for this program
                vm.ExistingRoles = _db.Role
                    .Where(r => r.program_Id == agentProgramId)  // ONLY this program
                    .ToList()
                    .Select(r => new ManageRoleVM
                    {
                        RoleId = r.Role_Id,
                        RoleName = r.Role_Name,
                        program_Id = r.program_Id,
                        ProgramName = _db.Programs.FirstOrDefault(p => p.Program_Id == r.program_Id)?.Program_Name ?? "Unknown",
                        UserCount = _db.agent.Count(a => a.RoleId == r.Role_Id && a.ProgramId == agentProgramId)
                    }).ToList();
            }

            // Show ONLY the user's program in dropdown
            var programs = agentProgramId.HasValue
                ? _db.Programs.Where(p => p.Program_Id == agentProgramId).ToList()
                : new List<Program_Setup>();

            ViewBag.Programs = new SelectList(programs, "Program_Id", "Program_Name");
            ViewBag.ProgramId = agentProgramId;
            ViewBag.ProgramName = programs.FirstOrDefault()?.Program_Name ?? "No Program";

            return View("~/Views/SystemSetup/ManageRole.cshtml", vm);
        }

        // 2. AJAX: Get Specific Details
        [HttpGet]
        public JsonResult GetRoleDetails(int id)
        {
            int? agentProgramId = Session["AgentProgramId"] as int?;

            var role = _db.Role.FirstOrDefault(r => r.Role_Id == id);
            if (role == null)
                return Json(new { success = false, message = "Not found" }, JsonRequestBehavior.AllowGet);

            // User can ONLY view roles from their program
            if (!agentProgramId.HasValue || role.program_Id != agentProgramId)
                return Json(new { success = false, message = "Access denied" }, JsonRequestBehavior.AllowGet);

            string programName = _db.Programs.FirstOrDefault(p => p.Program_Id == role.program_Id)?.Program_Name ?? "Unknown";

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
                return Json(new { success = false, message = "Invalid data" });

            int? agentProgramId = Session["AgentProgramId"] as int?;
            if (!agentProgramId.HasValue)
                return Json(new { success = false, message = "No program assigned" });

            try
            {
                // Force program to user's program
                vm.program_Id = agentProgramId.Value;

                bool exists = _db.Role.Any(r =>
                    r.Role_Name.Trim().ToLower() == vm.RoleName.Trim().ToLower()
                    && r.Role_Id != vm.RoleId
                    && r.program_Id == agentProgramId);

                if (exists)
                    return Json(new { success = false, message = "Role name already exists in this program" });

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

                if (vm.RoleId == 0)
                {
                    role = new Role
                    {
                        Role_Name = vm.RoleName.Trim(),
                        Privilege = privilegeCsv,
                        program_Id = vm.program_Id
                    };
                    _db.Role.Add(role);
                }
                else
                {
                    role = _db.Role.Find(vm.RoleId);
                    if (role == null || role.program_Id != agentProgramId)
                        return Json(new { success = false, message = "Access denied" });

                    role.Role_Name = vm.RoleName.Trim();
                    role.Privilege = privilegeCsv;
                    role.program_Id = vm.program_Id;
                }

                _db.SaveChanges();

                return Json(new { success = true, role = new { RoleId = role.Role_Id, RoleName = role.Role_Name } });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // 4. AJAX: Delete Role
        [HttpPost]
        public JsonResult DeleteRole(int id)
        {
            int? agentProgramId = Session["AgentProgramId"] as int?;
            if (!agentProgramId.HasValue)
                return Json(new { success = false, message = "Access denied" });

            var role = _db.Role.Find(id);
            if (role == null)
                return Json(new { success = false, message = "Role not found." });

            // User can ONLY delete roles from their program
            if (role.program_Id != agentProgramId)
                return Json(new { success = false, message = "Access denied" });

            if (_db.agent.Any(a => a.RoleId == id && a.ProgramId == agentProgramId))
                return Json(new { success = false, message = "Cannot delete: Users are assigned to this role." });

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