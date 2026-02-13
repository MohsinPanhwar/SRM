using System.Web.Mvc;
using SRM.Models;


namespace SRM.Controllers
{
    public class BaseController : Controller
    {
        // A helper property to quickly get permissions in any Controller
        public Privilege CurrentPrivileges => Session["UserPrivileges"] as Privilege;

        // A helper to check if the user is a Super Admin (if applicable)
        public bool IsAdmin => Session["IsAdmin"] != null && (bool)Session["IsAdmin"];

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Basic check: If session is lost, send them to login
            if (Session["AgentPno"] == null)
            {
                // For MVC 5, we use RedirectToRouteResult
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary(
                        new { controller = "Account", action = "Login" }));
            }
            base.OnActionExecuting(filterContext);
        }

        // Shared method to check a specific permission
        public bool HasPermission(string permissionName)
        {
            // STAGE 1: Get the privilege object from Session
            var privs = Session["UserPrivileges"] as SRM.Models.Privilege;

            // If session is lost or object is null, deny everything
            if (privs == null) return false;

            // STAGE 2: Use Reflection to find the property (checkbox) by name
            var prop = privs.GetType().GetProperty(permissionName);
            if (prop == null) return false;

            // STAGE 3: Return the actual value of the database column (true/false)
            return (bool)(prop.GetValue(privs, null) ?? false);
        }
    }
}