using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SRM.Controllers
{
    public class HardwareDispatchController : BaseController
    {
        // GET: HardwareDispatch
        public ActionResult NewHardware()
        {
            return View("~/Views/Hardware Dispatch/HardwareDispatch.cshtml");
        }
    }
}