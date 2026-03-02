using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace SRM.Helpers
{
    public static class PrivilegeHelper
    {
        private static readonly string _superAdminPNo;

        static PrivilegeHelper()
        {
            try
            {
                // Custom .env reader for .NET 4.5
                string path = HttpContext.Current.Server.MapPath("~/.env");
                if (File.Exists(path))
                {
                    foreach (var line in File.ReadAllLines(path))
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                        var parts = line.Split(new[] { '=' }, 2);
                        if (parts.Length == 2 && parts[0].Trim() == "SUPER_ADMIN_PNO")
                        {
                            _superAdminPNo = parts[1].Trim().Trim('"'); // Remove quotes if present
                            break;
                        }
                    }
                }
            }
            catch { _superAdminPNo = string.Empty; }
        }
        public static string GetEnvVariable(string key, string defaultValue = "")
        {
            try
            {
                string path = HttpContext.Current.Server.MapPath("~/.env");
                if (File.Exists(path))
                {
                    // Read every time or cache it in a static Dictionary for better performance
                    foreach (var line in File.ReadAllLines(path))
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                        var parts = line.Split(new[] { '=' }, 2);
                        if (parts.Length == 2 && parts[0].Trim() == key)
                        {
                            return parts[1].Trim().Trim('"');
                        }
                    }
                }
            }
            catch { }
            return defaultValue;
        }
        public static bool IsSuperAdmin()
        {
            var currentPNo = HttpContext.Current?.Session["AgentPno"]?.ToString();
            if (string.IsNullOrWhiteSpace(currentPNo) || string.IsNullOrWhiteSpace(_superAdminPNo))
                return false;

            return currentPNo.Trim().Equals(_superAdminPNo, StringComparison.OrdinalIgnoreCase);
        }

        public static bool HasPrivilege(string privilegeCode)
        {
            if (IsSuperAdmin()) return true;
            return GetUserPrivileges().Contains(privilegeCode.Trim());
        }

        private static List<string> GetUserPrivileges()
        {
            var rawPrivs = HttpContext.Current?.Session["UserPrivileges"] as string;
            return string.IsNullOrEmpty(rawPrivs)
                ? new List<string>()
                : rawPrivs.Split(',').Select(p => p.Trim()).ToList();
        }
    }
}