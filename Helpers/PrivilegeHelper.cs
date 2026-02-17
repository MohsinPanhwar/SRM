using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SRM.Helpers
{
    public static class PrivilegeHelper
    {
        /// <summary>
        /// Check if user has a specific privilege
        /// </summary>
        public static bool HasPrivilege(string privilegeCode)
        {
            var userPrivileges = HttpContext.Current?.Session["UserPrivileges"] as string ?? string.Empty;
            return userPrivileges.Split(',').Select(p => p.Trim()).Contains(privilegeCode);
        }

        /// <summary>
        /// Check if user has ANY of the privileges
        /// </summary>
        public static bool HasAnyPrivilege(params string[] privilegeCodes)
        {
            var userPrivileges = HttpContext.Current?.Session["UserPrivileges"] as string ?? string.Empty;
            var userPrivList = userPrivileges.Split(',').Select(p => p.Trim()).ToList();
            return privilegeCodes.Any(code => userPrivList.Contains(code));
        }

        /// <summary>
        /// Check if user has ALL of the privileges
        /// </summary>
        public static bool HasAllPrivileges(params string[] privilegeCodes)
        {
            var userPrivileges = HttpContext.Current?.Session["UserPrivileges"] as string ?? string.Empty;
            var userPrivList = userPrivileges.Split(',').Select(p => p.Trim()).ToList();
            return privilegeCodes.All(code => userPrivList.Contains(code));
        }
    }
}