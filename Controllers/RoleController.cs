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
    public class RoleController : Controller
    {
        private AppDbContext _db = new AppDbContext();

        // 1. Initial Page Load (Shell)
        public ActionResult ManageRole()
        {
            var vm = new ManageRoleVM();

            // Initial load of existing roles for the table
            vm.ExistingRoles = _db.Roles.Select(r => new ManageRoleVM
            {
                RoleId = r.Role_Id,
                RoleName = r.Role_Name,
                UserCount = _db.agent.Count(a => a.RoleId == r.Role_Id)
            }).ToList();

            return View("~/Views/SystemSetup/ManageRole.cshtml", vm);
        }

        // 2. AJAX: Fetch All Roles (for refreshing the table)
        [HttpGet]
        public JsonResult GetAllRoles()
        {
            var roles = _db.Roles.Select(r => new {
                role_id = r.Role_Id,
                role_name = r.Role_Name,
                UserCount = _db.agent.Count(a => a.RoleId == r.Role_Id)
            }).OrderByDescending(r => r.role_id).ToList();

            return Json(roles, JsonRequestBehavior.AllowGet);
        }

        // 3. AJAX: Save Role (Create or Update)
        [HttpPost]
        public JsonResult SaveRole(ManageRoleVM vm)
        {
            if (vm == null || string.IsNullOrWhiteSpace(vm.RoleName))
                return Json(new { success = false, message = "Invalid data" });

            try
            {
                // Duplication Check
                bool exists = _db.Roles.Any(r => r.Role_Name.Trim().ToLower() == vm.RoleName.Trim().ToLower() && r.Role_Id != vm.RoleId);
                if (exists) return Json(new { success = false, message = "Role name already exists" });

                Roles role;
                Privilege priv;

                if (vm.RoleId == 0)
                {
                    // --- CREATE MODE ---
                    priv = new Privilege();
                    MapPrivilegesFromVM(vm, priv);
                    _db.Privileges.Add(priv);
                    _db.SaveChanges(); // Get the generated privilege_id

                    role = new Roles
                    {
                        Role_Name = vm.RoleName.Trim(),
                        Privilege_Id = priv.privilege_id // Link manually
                    };
                    _db.Roles.Add(role);
                }
                else
                {
                    // --- EDIT MODE ---
                    role = _db.Roles.Find(vm.RoleId);
                    if (role == null) return Json(new { success = false, message = "Role not found" });

                    role.Role_Name = vm.RoleName.Trim();

                    priv = _db.Privileges.Find(role.Privilege_Id);
                    if (priv != null)
                    {
                        MapPrivilegesFromVM(vm, priv);
                    }
                }

                _db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // 4. AJAX: Get Specific Details (Populate form for Edit)
        [HttpGet]
        public JsonResult GetRoleDetails(int id)
        {
            var role = _db.Roles.Find(id);
            if (role == null) return Json(new { success = false, message = "Not found" }, JsonRequestBehavior.AllowGet);

            var priv = _db.Privileges.FirstOrDefault(p => p.privilege_id == role.Privilege_Id);

            // Return an object that JS can easily loop through
            return Json(new
            {
                success = true,
                data = new
                {
                    RoleId = role.Role_Id,
                    RoleName = role.Role_Name,
                    // Mapping every privilege explicitly for the AJAX result
                    CanAddEditEngineer = priv?.CanAddEditEngineer ?? false,
                    CanLogNewRequest = priv?.CanLogNewRequest ?? false,
                    CanViewForwardAny = priv?.CanViewForwardAny ?? false,
                    CanOnlyViewAny = priv?.CanOnlyViewAny ?? false,
                    CanViewForwardOwn = priv?.CanViewForwardOwn ?? false,
                    CanAddEditGroups = priv?.CanAddEditGroups ?? false,
                    CanAddNewIncident = priv?.CanAddNewIncident ?? false,
                    CanViewIncident = priv?.CanViewIncident ?? false,
                    CanLogNOC = priv?.CanLogNOC ?? false,
                    CanViewOwnGroup = priv?.CanViewOwnGroup ?? false,
                    CanReopenAny = priv?.CanReopenAny ?? false,
                    CanViewEditMessage = priv?.CanViewEditMessage ?? false,
                    CanSendSMS = priv?.CanSendSMS ?? false,
                    CanHardware = priv?.CanHardware ?? false,
                    CanSendPassword = priv?.CanSendPassword ?? false,
                    CanViewReports = priv?.CanViewReports ?? false,
                    CanAddEditAsset = priv?.CanAddEditAsset ?? false
                }
            }, JsonRequestBehavior.AllowGet);
        }

        // 5. AJAX: Delete Role
        [HttpPost]
        public JsonResult DeleteRole(int id)
        {
            var role = _db.Roles.Find(id);
            if (role == null) return Json(new { success = false, message = "Role not found." });

            if (_db.agent.Any(a => a.RoleId == id))
                return Json(new { success = false, message = "Cannot delete: Users are assigned to this role." });

            var priv = _db.Privileges.Find(role.Privilege_Id);
            if (priv != null) _db.Privileges.Remove(priv);

            _db.Roles.Remove(role);
            _db.SaveChanges();

            return Json(new { success = true });
        }

        // Helper Method
        private void MapPrivilegesFromVM(ManageRoleVM vm, Privilege priv)
        {
            priv.CanAddEditEngineer = vm.CanAddEditEngineer;
            priv.CanLogNewRequest = vm.CanLogNewRequest;
            priv.CanViewForwardAny = vm.CanViewForwardAny;
            priv.CanOnlyViewAny = vm.CanOnlyViewAny;
            priv.CanViewForwardOwn = vm.CanViewForwardOwn;
            priv.CanAddEditGroups = vm.CanAddEditGroups;
            priv.CanAddNewIncident = vm.CanAddNewIncident;
            priv.CanViewIncident = vm.CanViewIncident;
            priv.CanLogNOC = vm.CanLogNOC;
            priv.CanViewOwnGroup = vm.CanViewOwnGroup;
            priv.CanReopenAny = vm.CanReopenAny;
            priv.CanViewEditMessage = vm.CanViewEditMessage;
            priv.CanSendSMS = vm.CanSendSMS;
            priv.CanHardware = vm.CanHardware;
            priv.CanSendPassword = vm.CanSendPassword;
            priv.CanViewReports = vm.CanViewReports;
            priv.CanAddEditAsset = vm.CanAddEditAsset;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}