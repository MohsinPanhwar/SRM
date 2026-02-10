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

            // 2. Populate agents Dropdown (PNo + Name)
            vm.AgentList = db.agent.Select(a => new SelectListItem
            {
                Text = a.Pno + " - " + a.Name,
                Value = a.Pno
            }).ToList();

            // 3. Fetch existing groups
            vm.ExistingGroups = db.groups.ToList().Select(g => new ManageGroupVM
            {
                GroupId = g.gid,
                GroupName = g.gname,
                ProgramName = db.Programs.Where(p => p.Program_Id== g.program_id).Select(p => p.Program_Name).FirstOrDefault() ?? "N/A",
                Manager = g.manager_pno,
                MembersCount = 0
            }).ToList();

            return View("~/Views/SystemSetup/ManageGroup.cshtml", vm);
        }

        [HttpPost]
        public JsonResult SaveGroup(ManageGroupVM vm)
        {
            if (vm == null) return Json(new { success = false, message = "Invalid data" });

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
    }
}