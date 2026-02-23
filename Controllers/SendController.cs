using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SRM.Controllers
{
    public class SendController : Controller
    {
        // GET: Send
        public ActionResult SendSMS()
        {
            return View("~/Views/ServiceRequest/SendSMS.cshtml");
        }
        public ActionResult SendPassword()
        {
            return View("~/Views/ServiceRequest/SendPassword.cshtml");
        }
    }
}