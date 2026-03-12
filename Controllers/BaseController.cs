using System.Web.Mvc;
using System.Web.Routing;

namespace SRM.Controllers
{
    [Authorize] // This ensures the user must be logged in
    public class BaseController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // If the user is authenticated (cookie exists) but the session has died
            if (filterContext.HttpContext.User.Identity.IsAuthenticated &&
                Session["AgentPno"] == null)
            {
                // Kick them to login
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary {
                        { "controller", "Account" },
                        { "action", "Login" }
                    });
            }

            base.OnActionExecuting(filterContext);
        }
    }
}