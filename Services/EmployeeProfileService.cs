using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SRM.Data;
using SRM.Models;
using SRM.Helpers;

namespace SRM.Services
{
    public static class EmployeeProfileService
    {
        // Returns existing local profile or fetches from remote API and saves locally
        public static async Task<EmployeeProfile> GetOrFetchAsync(AppDbContext db, string pno)
        {
            if (string.IsNullOrWhiteSpace(pno)) return null;

            // 1. Try local DB
            var existing = db.EmployeeProfiles.FirstOrDefault(e => e.Pno == pno);
            if (existing != null) return existing;

            // 2. Fetch from remote API
            var fetched = await FetchFromPiaApiAsync(pno);
            if (fetched != null)
            {
                try
                {
                    db.EmployeeProfiles.Add(fetched);
                    await db.SaveChangesAsync();
                }
                catch
                {
                    // swallow DB save errors to avoid breaking callers; return fetched anyway
                }
            }

            return fetched;
        }

        private static async Task<EmployeeProfile> FetchFromPiaApiAsync(string pno)
        {
            try
            {
                string token = PrivilegeHelper.GetEnvVariable("PIAC_API_TOKEN");
                using (var client = new HttpClient())
                {
                    var requestData = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("PNO", pno),
                        new KeyValuePair<string, string>("tokenid", token ?? string.Empty)
                    });

                    string postUrl = "http://piacconnect.piac.com.pk/piamobileApp/services.asmx/SignUP";
                    var response = await client.PostAsync(postUrl, requestData);
                    if (!response.IsSuccessStatusCode) return null;

                    var jsonString = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(jsonString)) return null;

                    var results = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(jsonString);
                    var emp = results?.FirstOrDefault();
                    if (emp == null) return null;

                    var profile = new EmployeeProfile
                    {
                        Pno = emp.ContainsKey("pno") ? emp["pno"] : pno,
                        emp_name = emp.ContainsKey("name") ? emp["name"] : "N/A",
                        Emp_designation = emp.ContainsKey("Emp_designation") ? emp["Emp_designation"] : "N/A",
                        DEPT = emp.ContainsKey("Department") ? emp["Department"] : "N/A",
                        Email = emp.ContainsKey("email") ? emp["email"] : string.Empty,
                        mobileno = emp.ContainsKey("Phone_Num") ? emp["Phone_Num"] : string.Empty,
                        NIC = emp.ContainsKey("Emp_NIC") ? emp["Emp_NIC"] : string.Empty,
                        Location = emp.ContainsKey("Organization") ? emp["Organization"] : string.Empty,
                        Emp_dob = ParseDate(emp.ContainsKey("dob") ? emp["dob"] : null),
                        Appointment_Date = ParseDate(emp.ContainsKey("doj") ? emp["doj"] : null),
                        Status_id = emp.ContainsKey("status") ? emp["status"] : string.Empty,
                        ContractType = emp.ContainsKey("person_type") ? emp["person_type"] : string.Empty,
                        UPDATED_ON = DateTime.Now,
                        UPDATED_BY = "SYSTEM_API"
                    };

                    return profile;
                }
            }
            catch
            {
                return null;
            }
        }

        private static DateTime? ParseDate(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (DateTime.TryParse(s, out var d)) return d;
            return null;
        }
    }
}
