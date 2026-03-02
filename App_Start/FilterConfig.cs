using System.Web;
using System.Web.Mvc;

namespace SRM
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            // This forces EVERY page to check if the user is logged in
            filters.Add(new AuthorizeAttribute());
        }
    }
}
