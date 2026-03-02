using System;
using System.Linq;
using System.Web.Mvc;
using SRM.Data;
using SRM.Models;
using System.Web;

namespace SRM.Controllers
{
    [Authorize]
    public class HomeController : BaseController
    {
        private readonly AppDbContext _db = new AppDbContext();

        // GET: Home
        public ActionResult Home()
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));

            var pno = Session["AgentPno"] as string;
            

            // Fetch the agent from DB - This is our source of truth
            var agent = _db.agent.FirstOrDefault(a => a.Pno == pno);
            if (agent == null)
                return HttpNotFound();

            // 1. FIX ROLE: Try Session first, then fall back to the Agent's actual Database RoleId
            int? roleId = null;
            if (Session["AgentRoleId"] != null)
            {
                roleId = Convert.ToInt32(Session["AgentRoleId"]);
            }
            else
            {
                roleId = agent.RoleId; // Use the property name from your Agent Model
            }

            var role = _db.Role.FirstOrDefault(r => r.Role_Id == roleId);

            // DEBUG: If it still says "User", it's because 'role' is null. 
            // This confirms the ID in the database doesn't have a matching name in the Role table.
            ViewBag.RoleName = role?.Role_Name ?? "User";

            // 2. FETCH PROGRAM NAME
            int? progId = (Session["AgentProgramId"] != null) ? Convert.ToInt32(Session["AgentProgramId"]) : agent.ProgramId;
            var program = _db.Programs.FirstOrDefault(p => p.Program_Id == progId);
            ViewBag.ProgramName = program?.Program_Name ?? "No Program Assigned";

            // 3. FETCH GROUP NAME (Using your specific Gid/gid/gname columns)
            int? groupId = agent.Gid;
            var group = _db.groups.FirstOrDefault(g => g.gid == groupId);
            ViewBag.GroupName = group?.gname ?? "None";

            return View(agent);
        }
    }
}
