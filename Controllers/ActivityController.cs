using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SRM.Controllers
{
    public class ActivityController : Controller
    {
        // GET: Activity
        public ActionResult LogNewActivity()
        {
            return View("~/Views/ActivityManagement/LogNewActivity.cshtml");
        }
        public ActionResult ShowAllActivities()
        {
            return View("~/Views/ActivityManagement/ShowAllActivities.cshtml");
        }

    }
}