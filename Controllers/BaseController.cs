using System;
using System.Web.Mvc;
using System.Web.Routing; // Required for RedirectToRouteResult
using SRM.Models;

namespace SRM.Controllers
{
    public class BaseController : Controller
    {
        // Access the privilege object stored in Session
        public Privilege UserPrivileges => Session["UserPrivileges"] as Privilege;

        // Check if the user is a Super Admin
        public bool IsAdmin => Session["IsAdmin"] != null && (bool)Session["IsAdmin"];

        /// <summary>
        /// Logic to check if user has a specific boolean permission.
        /// Usage: if (!HasAccess(p => p.CanHardware)) { ... }
        /// </summary>
        public bool HasAccess(Func<Privilege, bool> permissionCheck)
        {
            if (Session["AgentPno"] == null) return false;
            if (IsAdmin) return true; // Admins bypass all checks

            if (UserPrivileges != null)
            {
                return permissionCheck(UserPrivileges);
            }
            return false;
        }

        /// <summary>
        /// This runs before every action in any Controller that inherits from BaseController.
        /// It acts as a global "Logged-In" check.
        /// </summary>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // If session is empty and we aren't already on the Login page, send them to Login
            if (Session["AgentPno"] == null)
            {
                string controller = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName;
                string action = filterContext.ActionDescriptor.ActionName;

                // Don't redirect if we are already trying to Login or Register
                if (!(controller == "Account" && (action == "Login" || action == "Register")))
                {
                    filterContext.Result = new RedirectToRouteResult(
                        new RouteValueDictionary {
                            { "controller", "Account" },
                            { "action", "Login" }
                        });
                }
            }

            base.OnActionExecuting(filterContext);
        }
    }
}