using SRM.Data;
using SRM.Models;
using SRM.Models.ViewModels;
using SRM.Services;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SRM.Controllers
{
    public class AssetIssuanceController : BaseController
    {
        private readonly AppDbContext db = new AppDbContext();

        // 🔹 LOAD FORM: Initial page load with dropdowns and recent assets
        public ActionResult Issuance()
        {
            var vm = new AssetIssuanceVM();

            // 1. Populate Dropdowns
            vm.CategoryList = db.invCategory.Select(x => new SelectListItem
            { Value = x.CategoryID.ToString(), Text = x.CategoryName }).ToList();

            vm.BrandList = db.invBrand.Select(x => new SelectListItem
            { Value = x.ID.ToString(), Text = x.Name }).ToList();

            vm.LocationList = db.Locations.Select(l => new SelectListItem
            { Value = l.sno.ToString(), Text = l.Location_Description }).ToList();

            vm.LocationList.Insert(0, new SelectListItem { Text = "-- Select Location --", Value = "" });

            // 2. Fetch Recent Assets (Joining for Display Names)
            var recentQuery = from issue in db.InvIssueDetails
                              join cat in db.invCategory on issue.CategoryID equals cat.CategoryID into catJoin
                              from cat in catJoin.DefaultIfEmpty()
                              join brand in db.invBrand on issue.BrandID equals brand.ID into brandJoin
                              from brand in brandJoin.DefaultIfEmpty()
                              orderby issue.entry_date descending
                              select new { IssueRecord = issue, CatName = cat.CategoryName, BName = brand.Name };

            vm.RecentAssets = recentQuery.Take(10).AsEnumerable().Select(x =>
            {
                x.IssueRecord.CategoryName = x.CatName ?? "N/A";
                x.IssueRecord.BrandName = x.BName ?? "N/A";
                return x.IssueRecord;
            }).ToList();

            vm.Issue = new InvIssueDetail();
            return View(vm);
        }

        // 🔹 GET EMPLOYEE: Uses the centralized Service to handle Local DB + API Sync
        [HttpGet]
        public async Task<JsonResult> GetEmployee(string pno)
        {
            if (string.IsNullOrWhiteSpace(pno))
                return Json(new { success = false, message = "PNO is required" }, JsonRequestBehavior.AllowGet);

            // This calls your service: logic is encapsulated there
            var profile = await EmployeeProfileService.GetOrFetchAsync(db, pno);

            if (profile != null)
            {
                return Json(new
                {
                    success = true,
                    name = profile.emp_name,
                    designation = profile.Emp_designation,
                    department = profile.DEPT,
                    email = profile.Email,
                    mobile = profile.mobileno,
                    // 🔹 Make sure these match the JavaScript below
                    office_ext = profile.Office_ext,
                    roomNo = profile.roomno,
                    IpAddress = profile.ip_address,
                    Location = profile.Location
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = false, message = "Employee not found." }, JsonRequestBehavior.AllowGet);
        }

        // 🔹 SAVE ASSET: Handles both New Issuance and Updates
        [HttpPost]
        public JsonResult SaveAsset(InvIssueDetail Issue)
        {
            try
            {
                if (Issue == null) return Json(new { success = false, message = "No data received." });

                Issue.entry_date = DateTime.Now;
                Issue.enter_by = User.Identity.Name ?? "System";

                if (Issue.sno > 0)
                {
                    db.Entry(Issue).State = EntityState.Modified;
                    db.SaveChanges();
                    return Json(new { success = true, message = "Asset updated successfully!" });
                }
                else
                {
                    db.InvIssueDetails.Add(Issue);
                    db.SaveChanges();
                    return Json(new { success = true, message = "Asset saved successfully!" });
                }
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "DB Error: " + msg });
            }
        }

        // 🔹 SAVE USER: Syncs/Updates Employee Profile details
        [HttpPost]
        public async Task<JsonResult> SaveUser(EmployeeProfile emp)
        {
            try
            {
                if (emp == null || string.IsNullOrEmpty(emp.Pno))
                    return Json(new { success = false, message = "Invalid data." });

                // Sync with Service first to ensure we have the record
                var existing = await EmployeeProfileService.GetOrFetchAsync(db, emp.Pno);

                if (existing == null)
                {
                    db.EmployeeProfiles.Add(emp);
                }
                else
                {
                    // Map form fields to existing record
                    existing.emp_name = emp.emp_name;
                    existing.Emp_designation = emp.Emp_designation;
                    existing.DEPT = emp.DEPT;
                    existing.Email = emp.Email;
                    existing.mobileno = emp.mobileno;
                    existing.Office_ext = emp.Office_ext;
                    existing.roomno = emp.roomno;
                    existing.ip_address = emp.ip_address;
                    existing.Location = emp.Location;
                    existing.UPDATED_BY = User.Identity.Name ?? "System";
                    existing.UPDATED_ON = DateTime.Now;
                }

                await db.SaveChangesAsync();
                return Json(new { success = true, message = "User profile updated." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // 🔹 LIST VIEW: Shows all issued assets with filters
        public ActionResult IssuedAssets(string pno, int? brandId)
        {
            var vm = new IssuedAssetListVM();

            // Populate Filter Dropdowns
            vm.BrandList = db.invBrand.Select(b => new SelectListItem { Value = b.ID.ToString(), Text = b.Name }).ToList();
            vm.BrandList.Insert(0, new SelectListItem { Text = "-- All Brands --", Value = "" });

            vm.PnoList = db.InvIssueDetails.Select(x => x.Issued_to_PNO).Distinct()
                           .Select(p => new SelectListItem { Value = p, Text = p }).ToList();
            vm.PnoList.Insert(0, new SelectListItem { Text = "-- All Employees --", Value = "" });

            // Join query to get descriptive names for Grid
            var query = from issue in db.InvIssueDetails
                        join cat in db.invCategory on issue.CategoryID equals cat.CategoryID into catJoin
                        from cat in catJoin.DefaultIfEmpty()
                        join brand in db.invBrand on issue.BrandID equals brand.ID into brandJoin
                        from brand in brandJoin.DefaultIfEmpty()
                        join loc in db.Locations on issue.Location_ID equals loc.sno.ToString() into locJoin
                        from loc in locJoin.DefaultIfEmpty()
                        select new
                        {
                            Data = issue,
                            CatName = cat.CategoryName,
                            BName = brand.Name,
                            LocName = loc.Location_Description
                        };

            var resultList = query.AsEnumerable().Select(x =>
            {
                x.Data.CategoryName = x.CatName;
                x.Data.BrandName = x.BName;
                x.Data.Locations = x.LocName;
                return x.Data;
            }).ToList();

            // Apply logic filters
            if (!string.IsNullOrEmpty(pno)) resultList = resultList.Where(x => x.Issued_to_PNO == pno).ToList();
            if (brandId.HasValue) resultList = resultList.Where(x => x.BrandID == brandId.Value).ToList();

            vm.Assets = resultList.OrderByDescending(x => x.entry_date).ToList();
            return View(vm);
        }

        // 🔹 DETAILS: Specific asset view
        public ActionResult Details(int id)
        {
            var asset = (from issue in db.InvIssueDetails
                         where issue.sno == id
                         join cat in db.invCategory on issue.CategoryID equals cat.CategoryID into catJoin
                         from cat in catJoin.DefaultIfEmpty()
                         join brand in db.invBrand on issue.BrandID equals brand.ID into brandJoin
                         from brand in brandJoin.DefaultIfEmpty()
                         join loc in db.Locations on issue.Location_ID equals loc.sno.ToString() into locJoin
                         from loc in locJoin.DefaultIfEmpty()
                         select new { Record = issue, CName = cat.CategoryName, BName = brand.Name, LName = loc.Location_Description })
                         .FirstOrDefault();

            if (asset == null) return HttpNotFound();

            asset.Record.CategoryName = asset.CName;
            asset.Record.BrandName = asset.BName;
            asset.Record.Locations = asset.LName;

            return View(asset.Record);
        }

        [HttpGet]
        public JsonResult GetAssetById(int id)
        {
            var asset = db.InvIssueDetails.FirstOrDefault(x => x.sno == id);
            if (asset == null) return Json(new { success = false }, JsonRequestBehavior.AllowGet);

            return Json(new
            {
                success = true,
                data = asset,
                issueDate = asset.IssueDate.ToString("yyyy-MM-dd")
            }, JsonRequestBehavior.AllowGet);
        }
    }
}