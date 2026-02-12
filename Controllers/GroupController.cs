using System;
using System.Linq;
using System.Web.Mvc;
using SRM.Models;
using SRM.Data;
using SRM.Models.ViewModels;
using System.Collections.Generic;

namespace SRM.Controllers
{
    public class GroupController : Controller
    {
        private readonly AppDbContext db = new AppDbContext();

        public ActionResult ManageGroup()
        {
            var vm = new ManageGroupVM();

            // 1. Populate Programs Dropdown
            vm.ProgramList = db.Programs.Select(p => new SelectListItem
            {
                Text = p.Program_Name,
                Value = p.Program_Name
            }).ToList();

            // 2. Populate agents Dropdown
            vm.AgentList = db.agent.Select(a => new SelectListItem
            {
                Text = a.Pno + " - " + a.Name,
                Value = a.Pno
            }).ToList();

            // 3. Fetch existing groups
            var groupData = db.groups.ToList();

            vm.ExistingGroups = groupData.Select(g => new ManageGroupVM
            {
                GroupId = g.gid,
                GroupName = g.gname,
                ProgramName = db.Programs
                    .Where(p => p.Program_Id == g.program_id)
                    .Select(p => p.Program_Name)
                    .FirstOrDefault() ?? "N/A",
                Manager = g.manager_pno,
                MembersCount = GetMemberCount(g.gid)
            }).ToList();

            return View("~/Views/SystemSetup/ManageGroup.cshtml", vm);
        }

        [HttpPost]
        public JsonResult SaveGroup(ManageGroupVM vm)
        {
            try
            {
                if (vm == null || string.IsNullOrEmpty(vm.GroupName))
                    return Json(new { success = false, message = "Invalid data: Group Name is required" });

                // 1. DUPLICATION CHECK
                var normalizedName = vm.GroupName.Trim().ToLower();
                bool exists = db.groups.Any(g => g.gname.Trim().ToLower() == normalizedName && g.gid != vm.GroupId);

                if (exists)
                {
                    return Json(new { success = false, message = "This Group Name already exists." });
                }

                // 2. Lookup Program ID from Name
                var program = db.Programs.FirstOrDefault(p => p.Program_Name == vm.ProgramName);
                if (program == null) return Json(new { success = false, message = "Selected Program not found" });

                Group group;
                if (vm.GroupId == 0) // Create New
                {
                    group = new Group
                    {
                        gname = vm.GroupName.Trim(),
                        program_id = program.Program_Id,
                        manager_pno = vm.Manager
                    };
                    db.groups.Add(group);
                }
                else // Edit Existing
                {
                    group = db.groups.Find(vm.GroupId);
                    if (group == null) return Json(new { success = false, message = "Group not found" });

                    group.gname = vm.GroupName.Trim();
                    group.program_id = program.Program_Id;
                    group.manager_pno = vm.Manager;
                }

                db.SaveChanges();

                // Return success with the specific group ID
                return Json(new { success = true, group = new { gid = group.gid } });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteGroup(int id)
        {
            try
            {
                var group = db.groups.Find(id);
                if (group == null) return Json(new { success = false, message = "Group not found" });

                // Optional: Check if members exist before deleting
                int memberCount = GetMemberCount(id);
                if (memberCount > 0)
                {
                    return Json(new { success = false, message = "Cannot delete group with active members" });
                }

                db.groups.Remove(group);
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        private int GetMemberCount(int gid)
        {
            return db.agent.Count(a => a.Gid == gid);
        }
    }
}