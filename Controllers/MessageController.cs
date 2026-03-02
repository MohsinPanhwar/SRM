using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SRM.Data;

namespace SRM.Controllers
{
    public class MessageController : BaseController
    {
        private readonly AppDbContext db = new AppDbContext();

        // GET: MessageBoard
        public ActionResult MessageBoard()
        {
            // Get Global Filter from Session
            int? globalProgramId = Session["AgentProgramId"] as int?;

            // 1. Populate Agents (Filtered)
            ViewBag.Agents = db.agent
                .Where(a => !globalProgramId.HasValue || a.ProgramId == globalProgramId)
                .OrderBy(a => a.Name).ToList();

            // 2. Populate Groups (Filtered)
            ViewBag.Groups = db.groups
                .Where(g => !globalProgramId.HasValue || g.program_id == globalProgramId)
                .OrderBy(g => g.gname).ToList();

            // Return an empty list or null since we aren't using a history table yet
            return View("~/Views/SystemSetup/MessageBoard.cshtml");
        }
    }
}