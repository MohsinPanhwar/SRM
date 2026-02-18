using System;
using System.Linq;
using System.Web.Mvc;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using SRM.Models; // <--- MUST HAVE THIS
using SRM.Data;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

namespace SRM.Controllers
{
    public class NewRequestController : Controller
    {
        // Use your actual Context class name here (e.g., SRMEntities or MyDbContext)
        private AppDbContext _db = new AppDbContext();
        private readonly string soapUrl = "https://piacconnect.piac.com.pk/piamobileApp/services.asmx";

        public async Task<ActionResult> LogNewRequest(string searchPno)
        {
            // Define the path once so we don't make mistakes
            string viewPath = "~/Views/ServiceRequest/LogNewRequest.cshtml";

            // 1. Initial Load (Empty search)
            if (string.IsNullOrEmpty(searchPno))
            {
                return View(viewPath, new EmployeeProfile());
            }

            // 2. Search Local DB
            var profile = _db.EmployeeProfiles.FirstOrDefault(e => e.Pno == searchPno);

            if (profile == null)
            {
                // 3. Fallback to API
                profile = await FetchFromPiaSoapApi(searchPno);

                if (profile != null)
                {
                    // 4. Found in API - Save to Local DB
                    _db.EmployeeProfiles.Add(profile);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    // 5. Not found anywhere
                    TempData["Error"] = "Employee not found in local records or PIAC system.";
                    return View(viewPath, new EmployeeProfile());
                }
            }

            // 6. Return the found profile to the specific view path
            return View(viewPath, profile);
        }
        private async Task<EmployeeProfile> FetchFromPiaSoapApi(string pno)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var requestData = new FormUrlEncodedContent(new[]
                    {
                new KeyValuePair<string, string>("PNO", pno),
                new KeyValuePair<string, string>("tokenid", "BFE8DDB76AA373BE816038BD36D0D70F")
            });

                    string postUrl = "http://piacconnect.piac.com.pk/piamobileApp/services.asmx/SignUP";
                    var response = await client.PostAsync(postUrl, requestData);

                    if (response.IsSuccessStatusCode)
                    {
                        // This is the string starting with "[{"pno":"60987"..."
                        var jsonString = await response.Content.ReadAsStringAsync();

                        if (!string.IsNullOrEmpty(jsonString))
                        {
                            // Directly parse the JSON (No XDocument needed!)
                            var results = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(jsonString);
                            var emp = results?.FirstOrDefault();

                            if (emp != null)
                            {
                                return new EmployeeProfile
                                {
                                    Pno = emp.ContainsKey("pno") ? emp["pno"] : pno,
                                    emp_name = emp.ContainsKey("name") ? emp["name"] : "N/A",
                                    Emp_designation = emp.ContainsKey("Emp_designation") ? emp["Emp_designation"] : "N/A",

                                    // Mapping JSON 'Department' to your 'ATTENDANCE_DEPT_CODE'
                                    ATTENDANCE_DEPT_CODE = emp.ContainsKey("Department") ? emp["Department"] : "N/A",

                                    // Mapping JSON 'email' to your 'Email'
                                    Email = emp.ContainsKey("email") ? emp["email"] : "",

                                    // Mapping JSON 'Phone_Num' to your 'mobileno'
                                    mobileno = emp.ContainsKey("Phone_Num") ? emp["Phone_Num"] : "",

                                    // Mapping JSON 'Emp_NIC' to your existing 'NIC' property
                                    NIC = emp.ContainsKey("Emp_NIC") ? emp["Emp_NIC"] : "",

                                    // Mapping JSON 'Organization' to your 'Location' property
                                    Location = emp.ContainsKey("Organization") ? emp["Organization"] : "",

                                    // Mapping JSON dates to your existing DateTime properties
                                    Emp_dob = emp.ContainsKey("dob") && !string.IsNullOrEmpty(emp["dob"])
                                              ? DateTime.Parse(emp["dob"]) : (DateTime?)null,

                                    Appointment_Date = emp.ContainsKey("doj") && !string.IsNullOrEmpty(emp["doj"])
                                                       ? DateTime.Parse(emp["doj"]) : (DateTime?)null,

                                    // Capturing Employee Status into your Status_id or ContractType if you prefer
                                    Status_id = emp.ContainsKey("status") ? emp["status"] : "",
                                    ContractType = emp.ContainsKey("person_type") ? emp["person_type"] : "",

                                    // Housekeeping
                                    UPDATED_ON = DateTime.Now,
                                    UPDATED_BY = "SYSTEM_API"
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Parsing Error: " + ex.Message);
            }
            return null;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SaveRequest(EmployeeProfile model, string Priority, string[] Area, string Summary, string Details)
        {
            try
            {
                // 1. Get Program ID from Session (Set by Switcher for SuperAdmin or Login for Agents)
                int? sessionProgramId = Session["AgentProgramId"] as int?;

                // 2. Update Employee Contact Info (Syncing changes made on the form)
                var existingEmp = _db.EmployeeProfiles.FirstOrDefault(e => e.Pno == model.Pno);
                if (existingEmp != null)
                {
                    existingEmp.Email = model.Email;
                    existingEmp.mobileno = model.mobileno;
                    existingEmp.roomno = model.roomno;
                    existingEmp.Location = model.Location;
                    existingEmp.ip_address = model.ip_address;
                }

                // 3. Create the New Service Request
                var newRequest = new Request_Master
                {
                    RequestDate = DateTime.Now,
                    RequestLogBy = Session["AgentPno"]?.ToString() ?? "ADMIN",
                    RequestFor = model.Pno,

                    // --- CRITICAL CHANGE: Assign the Program ID from Session ---
                    program_id = sessionProgramId,

                    Priority = MapPriority(Priority),
                    parea = Area != null ? string.Join(", ", Area) : "Other",
                    status = "Q", // Default status: Queue
                    ReqSummary = Summary,
                    ReqDetails = Details,
                    Location = model.Location,
                    RequestedIPAddress = model.ip_address,
                    LogPortal = "Internal"
                };

                _db.Request_Master.Add(newRequest);

                // 4. Save Everything to DB
                await _db.SaveChangesAsync();

                TempData["Success"] = $"Request #{newRequest.RequestID} logged successfully for Program ID: {sessionProgramId}!";
                return RedirectToAction("ViewAllRequests", "ViewAllRequests");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Save Failed: " + ex.Message;
                return RedirectToAction("LogNewRequest", new { searchPno = model.Pno });
            }
        }

        // Helper to map Priority string to Integer
        private int MapPriority(string p)
        {
            switch (p)
            {
                case "Critical": return 1;
                case "Urgent": return 2;
                case "Important": return 3;
                default: return 4; // Normal
            }
        }
    }
}