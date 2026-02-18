using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SRM.Controllers
{
    public class MessageController : Controller
    {
        // GET: MessageBoard
        public ActionResult MessageBoard()
        {
            return View("~/Views/SystemSetup/MessageBoard.cshtml");
        }
    }
}