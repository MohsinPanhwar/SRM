using System;
using System.Linq;
using System.Web.Mvc;
using SRM.Data;
using SRM.Models;
using System.Web;

namespace SRM.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly AppDbContext _db = new AppDbContext();

        // GET: Home
        public ActionResult Home()
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));

            var pno = Session["AgentPno"] as string;
            if (string.IsNullOrEmpty(pno))
                return RedirectToAction("Login", "Account");

            var agent = _db.agent.FirstOrDefault(a => a.Pno == pno);
            if (agent == null)
                return HttpNotFound();

            // Fetch role name from Role table
            var roleId = Session["AgentRoleId"] as int?;
            var role = _db.Role.FirstOrDefault(r => r.Role_Id == roleId);
            ViewBag.RoleName = role?.Role_Name ?? "User";

            return View(agent);
        }
    }
}
