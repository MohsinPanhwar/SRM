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

            // Get logged-in user's Pno from session
            var pno = Session["AgentPno"] as string;
            if (string.IsNullOrEmpty(pno))
                return RedirectToAction("Login", "Account");

            // Fetch agent info from database
            var agent = _db.agent.FirstOrDefault(a => a.Pno == pno);
            if (agent == null)
                return HttpNotFound();

            // Pass agent as model to the view
            return View(agent);
        }
    }
}
