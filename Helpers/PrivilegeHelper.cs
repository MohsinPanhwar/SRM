using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SRM.Helpers
{
    public static class PrivilegeHelper
    {
        // Replace "9999" with your actual SuperAdmin PNo
        private const string SuperAdminPNo = "P60987";

        public static bool IsSuperAdmin()
        {
            // Assuming you store the logged-in user's PNo in a Session variable
            var currentPNo = HttpContext.Current?.Session["AgentPno"]?.ToString();
            return currentPNo == SuperAdminPNo;
        }

        public static bool HasPrivilege(string privilegeCode)
        {
            if (IsSuperAdmin()) return true;

            var userPrivileges = HttpContext.Current?.Session["UserPrivileges"] as string ?? string.Empty;
            return userPrivileges.Split(',').Select(p => p.Trim()).Contains(privilegeCode);
        }

        public static bool HasAnyPrivilege(params string[] privilegeCodes)
        {
            if (IsSuperAdmin()) return true;

            var userPrivileges = HttpContext.Current?.Session["UserPrivileges"] as string ?? string.Empty;
            var userPrivList = userPrivileges.Split(',').Select(p => p.Trim()).ToList();
            return privilegeCodes.Any(code => userPrivList.Contains(code));
        }

        public static bool HasAllPrivileges(params string[] privilegeCodes)
        {
            if (IsSuperAdmin()) return true;

            var userPrivileges = HttpContext.Current?.Session["UserPrivileges"] as string ?? string.Empty;
            var userPrivList = userPrivileges.Split(',').Select(p => p.Trim()).ToList();
            return privilegeCodes.All(code => userPrivList.Contains(code));
        }
    }
}