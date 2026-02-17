using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using SRM.Models;

namespace SRM.Controllers
{
    public class BaseController : Controller
    {
        /// <summary>
        /// Retrieves the comma-separated privilege string from the Session.
        /// Example value: "LNR,FQR,SMS,SPWD"
        /// </summary>
        public string UserPrivilegeString => Session["UserPrivileges"]?.ToString() ?? string.Empty;

        /// <summary>
        /// Checks if the current user is flagged as a Super Admin in the Session.
        /// </summary>
        public bool IsAdmin => Session["IsAdmin"] != null && (bool)Session["IsAdmin"];

        /// <summary>
        /// Core logic to check if a user has a specific permission code.
        /// Usage: if (HasAccess("FOR")) { ... }
        /// </summary>
        /// <param name="code">The short code (e.g., "AFE", "LNR", "FOR")</param>
        /// <returns>True if the user is Admin or has the code in their string</returns>
        public bool HasAccess(string code)
        {
            // 1. If not logged in, no access
            if (Session["AgentPno"] == null) return false;

            // 2. Admins bypass all specific code checks
            if (IsAdmin) return true;

            // 3. If the privilege string is empty, no access
            if (string.IsNullOrEmpty(UserPrivilegeString)) return false;

            // 4. Split the string by comma and check for the specific code (case-insensitive)
            return UserPrivilegeString.Split(',')
                                      .Select(p => p.Trim())
                                      .Contains(code, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Global filter that runs before every Action.
        /// Ensures the user is logged in before allowing access to any controller inheriting BaseController.
        /// </summary>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Verify if the session key for the user exists
            if (Session["AgentPno"] == null)
            {
                string controller = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName;
                string action = filterContext.ActionDescriptor.ActionName;

                // Allow access to Login and Register actions without redirecting
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