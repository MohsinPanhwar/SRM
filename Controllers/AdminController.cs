using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SRM.Data;
using SRM.Helpers;

namespace SRM.Controllers
{
    public class AdminController : Controller
    {
        // GET: Admin
        [HttpPost]
        public ActionResult SetActiveProgram(int programId)
        {
            if (SRM.Helpers.PrivilegeHelper.IsSuperAdmin())
            {
                // We update the session variable used by your data queries
                Session["AgentProgramId"] = programId;
            }
            return Redirect(Request.UrlReferrer.ToString()); // Reload current page
        }
        private readonly AppDbContext _db = new AppDbContext();

        [ChildActionOnly]
        public ActionResult RenderProgramSwitcher()
        {
            // Only fetch if they are actually a SuperAdmin
            if (!PrivilegeHelper.IsSuperAdmin()) return Content("");

            var programs = _db.Programs
                .Select(p => new SelectListItem
                {
                    Value = p.Program_Id.ToString(),
                    Text = p.Program_Name
                }).ToList();

            return PartialView("_GlobalProgramSelector", programs);
        }
        [HttpPost]
        public ActionResult SetGlobalProgram(int? programId)
        {
            // Security check to ensure only SuperAdmins can switch contexts
            if (SRM.Helpers.PrivilegeHelper.IsSuperAdmin())
            {
                if (programId.HasValue)
                {
                    // 1. Find the program name
                    var program = _db.Programs.Find(programId.Value);
                    if (program != null)
                    {
                        Session["AgentProgramId"] = programId;
                        Session["AgentProgramName"] = program.Program_Name; // Store the Name
                    }
                }
                else
                {
                    // 2. Clear the session if "All Programs" is selected
                    Session["AgentProgramId"] = null;
                    Session["AgentProgramName"] = "All Programs";
                }

                TempData["Success"] = "Context switched successfully.";
            }

            // Redirect back to the page the user was already on
            return Redirect(Request.UrlReferrer?.ToString() ?? "/Home/home");
        }
    }

}