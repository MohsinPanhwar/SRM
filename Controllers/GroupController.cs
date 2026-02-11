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

            // 3. Fetch existing groups and map them
            // We fetch the data first, then map the counts
            var groupData = db.groups.ToList();

            vm.ExistingGroups = groupData.Select(g => new ManageGroupVM
            {
                GroupId = g.gid,
                GroupName = g.gname,
                // Program lookup
                ProgramName = db.Programs
                    .Where(p => p.Program_Id == g.program_id)
                    .Select(p => p.Program_Name)
                    .FirstOrDefault() ?? "N/A",
                Manager = g.manager_pno,

                // Use the helper function here
                MembersCount = GetMemberCount(g.gid)
            }).ToList();

            return View("~/Views/SystemSetup/ManageGroup.cshtml", vm);
        }

        [HttpPost]
        public JsonResult SaveGroup(ManageGroupVM vm)
        {
            if (vm == null) return Json(new { success = false, message = "Invalid data" });

            // 1. DUPLICATION CHECK
            var normalizedName = vm.GroupName.Trim().ToLower();
            // Check if any other record has this name, excluding the current ID if editing
            bool exists = db.groups.Any(g => g.gname.Trim().ToLower() == normalizedName && g.gid != vm.GroupId);

            if (exists)
            {
                return Json(new { success = false, message = "This Group Name already exists." });
            }

            // Lookup Program ID from Name
            var program = db.Programs.FirstOrDefault(p => p.Program_Name == vm.ProgramName);
            if (program == null) return Json(new { success = false, message = "Selected Program not found" });

            Group group;
            if (vm.GroupId == 0)
            {
                group = new Group
                {
                    gname = vm.GroupName.Trim(),
                    program_id = program.Program_Id,
                    manager_pno = vm.Manager // PNo from dropdown
                };
                db.groups.Add(group);
            }
            else
            {
                group = db.groups.Find(vm.GroupId);
                if (group == null) return Json(new { success = false, message = "Group not found" });

                group.gname = vm.GroupName.Trim();
                group.program_id = program.Program_Id;
                group.manager_pno = vm.Manager;
            }

            db.SaveChanges();
            // Returning gid in lowercase to match common JS expectations
            return Json(new { success = true, group = new { gid = group.gid } });
        }
        [HttpPost]
        public JsonResult DeleteGroup(int id)
        {
            var group = db.groups.Find(id);
            if (group == null) return Json(new { success = false, message = "Group not found" });
            db.groups.Remove(group);
            db.SaveChanges();
            return Json(new { success = true });
        }
        private int GetMemberCount(int gid)
        {
            // Returns the count of agents assigned to this group ID
            return db.agent.Count(a => a.Gid == gid);
        }
    }

}