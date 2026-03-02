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
using SRM.Helpers;
using SRM.Services;

namespace SRM.Controllers
{
    public class NewRequestController : BaseController
    {
        // Use your actual Context class name here (e.g., SRMEntities or MyDbContext)
        private AppDbContext _db = new AppDbContext();

        public async Task<ActionResult> LogNewRequest(string searchPno)
        {
            string viewPath = "~/Views/ServiceRequest/LogNewRequest.cshtml";

            // 1. Fetch Workshop ID from DB
            var workshopProgram = _db.Programs.FirstOrDefault(p => p.Program_Name == "Workshop");
            int? workshopIdFromDb = workshopProgram?.Program_Id;

            // 2. Get User Session Info
            int? globalProgramId = Session["AgentProgramId"] as int?;
            bool isSuperAdmin = Session["IsAdmin"]?.ToString() == "Y";

            // 3. Authorization Logic
            ViewBag.WorkshopId = workshopIdFromDb;
            ViewBag.IsWorkshopUser = (globalProgramId != null && globalProgramId == workshopIdFromDb) || isSuperAdmin;

            // 4. POPULATE THE LIST (The fix for your error)
            var programsQuery = isSuperAdmin
                ? _db.Programs.ToList()
                : _db.Programs.Where(p => p.Program_Id == globalProgramId).ToList();

            // Assign the list to ViewBag. Use the ID from session as the selected value
            ViewBag.ProgramList = new SelectList(programsQuery, "Program_Id", "Program_Name", globalProgramId);

            ViewBag.CurrentProgramId = globalProgramId?.ToString() ?? "0";
            // 5. Initial Load (Empty search)
            if (string.IsNullOrEmpty(searchPno))
            {
                return View(viewPath, new EmployeeProfile());
            }

            // 6. Search Logic - Use centralized service
            var profile = await EmployeeProfileService.GetOrFetchAsync(_db, searchPno);

            if (profile == null)
            {
                TempData["Error"] = "Employee not found.";
                // Even on error, we must return the view with the filled ViewBag
                return View(viewPath, new EmployeeProfile());
            }

            return View(viewPath, profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SaveRequest(EmployeeProfile model, string Priority, string[] Area, string Summary, string Details, FormCollection form)
        {
            using (var transaction = _db.Database.BeginTransaction())
            {
                try
                {
                    // 1. Get Global Program ID from Session
                    int? globalProgramId = Session["AgentProgramId"] as int?;

                    // 2. Fetch the "Workshop" ID from DB 
                    var workshopProgram = _db.Programs.FirstOrDefault(p => p.Program_Name == "Workshop");
                    int? workshopIdFromDb = workshopProgram?.Program_Id;

                    // 3. Update Employee Contact Info
                    var existingEmp = _db.EmployeeProfiles.FirstOrDefault(e => e.Pno == model.Pno);
                    if (existingEmp != null)
                    {
                        existingEmp.Email = model.Email;
                        existingEmp.mobileno = model.mobileno;
                        existingEmp.roomno = model.roomno;
                        existingEmp.Location = model.Location;
                        existingEmp.ip_address = model.ip_address;
                        existingEmp.Office_ext = model.Office_ext;

                        // EF tracks these changes automatically
                    }
                    string userIp = Request.UserHostAddress;
                    // 4. Create the Master Request
                    var newRequest = new Request_Master
                    {
                        RequestDate = DateTime.Now,
                        RequestLogBy = Session["AgentPno"]?.ToString() ?? "ADMIN",
                        RequestFor = model.Pno,
                        program_id = globalProgramId,
                        Priority = MapPriority(Priority),
                        parea = Area != null ? string.Join(", ", Area) : "Other",
                        status = "Q",
                        ReqSummary = Summary,
                        ReqDetails = Details,
                        Location = model.Location,
                        RequestedIPAddress = userIp
                    };

                    _db.Request_Master.Add(newRequest);
                    await _db.SaveChangesAsync(); // This generates the RequestID

                    // 5. Workshop Logic: Save if the program matches "Workshop"
                    if (globalProgramId != null && globalProgramId == workshopIdFromDb)
                    {
                        var workshopEntry = new NewIncoming
                        {
                            RequestID = newRequest.RequestID, // Link to Master
                            Location = model.Location,
                            Ext = int.TryParse(model.Office_ext, out int extVal) ? (int?)extVal : null,
                            UserName = model.emp_name,
                            Staffno = int.TryParse(model.Pno, out int sNo) ? (int?)sNo : null,
                            IP = model.ip_address,

                            ProdName = form["ProductName"],
                            Model = form["ModelName"],
                            Serial = form["SerialNo"],
                            Ram = form["Ram"],
                            HDD = form["HDD"],
                            LNIATA = form["LNIATA"],
                            Fault = form["Fault"],
                            Diagnosed = "null",
                            DateIn = DateTime.Now.ToString("yyyy-MM-dd"),
                            time_stamp = DateTime.Now,
                            Status = form["Status"],
                            userip = Request.UserHostAddress,
                            DataBackup = form["DataBackup"],
                            userdetails = Request.Browser.Browser + " " + Request.Browser.Version

                        };

                        _db.NewIncomings.Add(workshopEntry);
                        await _db.SaveChangesAsync(); // This will now work because of the SQL Identity change!
                    }

                    transaction.Commit();
                    TempData["Success"] = $"Request #{newRequest.RequestID} logged successfully!";
                    return RedirectToAction("ViewAllRequests", "ViewAllRequests");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    // Provide a detailed error message if it still fails
                    string inner = ex.InnerException?.InnerException?.Message ?? ex.InnerException?.Message ?? ex.Message;
                    TempData["Error"] = $"Save Failed: {inner}";
                    return RedirectToAction("LogNewRequest", new { searchPno = model.Pno });
                }
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