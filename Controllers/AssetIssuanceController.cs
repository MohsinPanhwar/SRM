using SRM.Data;
using SRM.Models.ViewModels;
using SRM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.ComponentModel;
using SRM.Services;

namespace SRM.Controllers
{
    public class AssetIssuanceController : BaseController
    {
        private AppDbContext db = new AppDbContext();

        // 🔹 LOAD FORM
        public ActionResult Issuance()
        {
            var vm = new AssetIssuanceVM();

            vm.CategoryList = db.invCategory
                .Select(x => new SelectListItem
                {
                    Value = x.CategoryID.ToString(),
                    Text = x.CategoryName
                }).ToList();

            vm.BrandList = db.invBrand
                .Select(x => new SelectListItem
                {
                    Value = x.ID.ToString(),
                    Text = x.Name
                }).ToList();

            // 🔹 ✅ LOCATION DROPDOWN FIX ADDED HERE
            vm.LocationList = db.Locations
                .Select(l => new SelectListItem
                {
                    Value = l.sno.ToString(), // Change this to the ID (integer)
                    Text = l.Location_Description
                }).ToList();

            vm.LocationList.Insert(0, new SelectListItem
            {
                Text = "-- Select Location --",
                Value = ""
            });

            var recentQuery = from issue in db.InvIssueDetails
                              join cat in db.invCategory on issue.CategoryID equals cat.CategoryID into catJoin
                              from cat in catJoin.DefaultIfEmpty()
                              join brand in db.invBrand on issue.BrandID equals brand.ID into brandJoin
                              from brand in brandJoin.DefaultIfEmpty()
                              orderby issue.entry_date descending
                              select new
                              {
                                  IssueRecord = issue,
                                  CategoryName = cat.CategoryName,
                                  BrandName = brand.Name
                              };

            // 3. Map the names to your [NotMapped] properties
            vm.RecentAssets = recentQuery.Take(10).AsEnumerable().Select(x =>
            {
                x.IssueRecord.CategoryName = x.CategoryName ?? "N/A";
                x.IssueRecord.BrandName = x.BrandName ?? "N/A";
                return x.IssueRecord;
            }).ToList();


            vm.Issue = new InvIssueDetail();

            return View(vm);
        }


        [HttpPost]
        public JsonResult SaveAsset(InvIssueDetail Issue)
        {
            try
            {
                if (Issue == null) return Json(new { success = false, message = "No data received." });

                // Standard audits
                Issue.entry_date = DateTime.Now;
                Issue.enter_by = User.Identity.Name ?? "System";

                // 🔹 THE FIX: Check if we are updating or inserting
                if (Issue.sno > 0)
                {
                    // Tell Entity Framework this is an existing record to be updated
                    db.Entry(Issue).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                    return Json(new { success = true, message = "Asset updated successfully!" });
                }
                else
                {
                    // This is a brand new record
                    db.InvIssueDetails.Add(Issue);
                    db.SaveChanges();
                    return Json(new { success = true, message = "Asset saved successfully!" });
                }
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.InnerException?.Message ?? ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "DB Error: " + msg });
            }
        }
        public async Task<JsonResult> GetEmployee(string pno)
        {
            // Use centralized service to fetch or retrieve profile
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
                    mobile = profile.mobileno
                }, JsonRequestBehavior.AllowGet);
            }

            // Fallback to local DB (should be redundant but kept for safety)
            var emp = db.EmployeeProfiles.FirstOrDefault(x => x.Pno == pno);

            if (emp == null)
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);

            return Json(new
            {
                success = true,
                name = emp.emp_name,
                designation = emp.Emp_designation,
                department = emp.DEPT,
                email = emp.Email,
                mobile = emp.mobileno
            }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult SaveUser(EmployeeProfile emp)
        {
            try
            {
                var existing = db.EmployeeProfiles.FirstOrDefault(x => x.Pno == emp.Pno);

                if (existing == null)
                {
                    // INSERT
                    db.EmployeeProfiles.Add(emp);
                }
                else
                {
                    // UPDATE - Property names must match your model exactly
                    existing.emp_name = emp.emp_name;
                    existing.Emp_designation = emp.Emp_designation;
                    existing.DEPT = emp.DEPT;
                    existing.Email = emp.Email;
                    existing.mobileno = emp.mobileno;
                    existing.Office_ext = emp.Office_ext;
                    existing.roomno = emp.roomno;
                    existing.ip_address = emp.ip_address;
                    existing.Location = emp.Location;

                    // Optional: Update metadata
                    existing.UPDATED_BY = "System"; // Or current user
                    existing.UPDATED_ON = DateTime.Now;
                }

                db.SaveChanges();
                return Json(new { success = true, message = "User saved successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Save(AssetIssuanceVM model)
        {
            if (!ModelState.IsValid)
            {
                // This will show you EXACTLY which fields are failing
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { Field = x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage) })
                    .ToList();

                // Temporarily return errors so you can see them in browser
                return Content(string.Join("<br/>", errors.Select(e => e.Field + ": " + string.Join(", ", e.Errors))));
            }

            model.Issue.entry_date = DateTime.Now;
            model.Issue.Issued_to_PNO = model.Pno;

            try
            {
                db.InvIssueDetails.Add(model.Issue);
                db.SaveChanges();
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
            {
                var errorMessages = dbEx.EntityValidationErrors
                    .SelectMany(x => x.ValidationErrors)
                    .Select(x => x.PropertyName + ": " + x.ErrorMessage);
                var fullErrorMessage = string.Join("; ", errorMessages);
                return Json(new { success = false, message = "Validation Failed: " + fullErrorMessage });
            }

            return RedirectToAction("Issuance");
        }

        public ActionResult IssuedAssets(string pno, int? brandId)
        {
            var vm = new IssuedAssetListVM();

            // 1. Populate Dropdowns (Keep existing logic)
            vm.BrandList = db.invBrand.Select(b => new SelectListItem { Value = b.ID.ToString(), Text = b.Name }).ToList();
            vm.BrandList.Insert(0, new SelectListItem { Text = "-- All Brands --", Value = "" });

            vm.PnoList = db.InvIssueDetails.Select(x => x.Issued_to_PNO).Distinct().Select(p => new SelectListItem { Value = p, Text = p }).ToList();
            vm.PnoList.Insert(0, new SelectListItem { Text = "-- All Employees --", Value = "" });

            // ══════════════════════════════════════════════════════════════
            // THE FIX: JOIN TABLES TO FETCH THE STATION/LOCATION NAME
            // ══════════════════════════════════════════════════════════════
            var query = from issue in db.InvIssueDetails
                        join cat in db.invCategory on issue.CategoryID equals cat.CategoryID into catJoin
                        from cat in catJoin.DefaultIfEmpty()
                        join brand in db.invBrand on issue.BrandID equals brand.ID into brandJoin
                        from brand in brandJoin.DefaultIfEmpty()
                            // Link the 'station' (Location_ID) to the Locations table
                        join loc in db.Locations on issue.Location_ID equals loc.sno.ToString() into locJoin
                        from loc in locJoin.DefaultIfEmpty()
                        select new
                        {
                            Data = issue,
                            CatName = cat.CategoryName,
                            BName = brand.Name,
                            LocName = loc.Location_Description
                        };

            // 2. Map results back to [NotMapped] fields for the View
            var resultList = query.AsEnumerable().Select(x => {
                x.Data.CategoryName = x.CatName;
                x.Data.BrandName = x.BName;
                x.Data.Locations = x.LocName; // This fills the column in your grid
                return x.Data;
            }).ToList();

            // 3. Apply Filters
            if (!string.IsNullOrEmpty(pno))
            {
                resultList = resultList.Where(x => x.Issued_to_PNO == pno).ToList();
                vm.SelectedPno = pno;
            }
            if (brandId.HasValue)
            {
                resultList = resultList.Where(x => x.BrandID == brandId.Value).ToList();
                vm.SelectedBrandId = brandId;
            }

            vm.Assets = resultList.OrderByDescending(x => x.entry_date).ToList();
            return View(vm);
        }
        public ActionResult Details(int id)
        {
            // Fetch the specific record and JOIN with Category, Brand, and Location tables
            var asset = (from issue in db.InvIssueDetails
                         where issue.sno == id
                         join cat in db.invCategory on issue.CategoryID equals cat.CategoryID into catJoin
                         from cat in catJoin.DefaultIfEmpty()
                         join brand in db.invBrand on issue.BrandID equals brand.ID into brandJoin
                         from brand in brandJoin.DefaultIfEmpty()
                         join loc in db.Locations on issue.Location_ID equals loc.sno.ToString() into locJoin
                         from loc in locJoin.DefaultIfEmpty()
                         select new
                         {
                             Record = issue,
                             CName = cat.CategoryName,
                             BName = brand.Name,
                             LName = loc.Location_Description
                         }).FirstOrDefault();

            if (asset == null)
            {
                return HttpNotFound();
            }

            // Map the descriptive names to your [NotMapped] properties so the View can see them
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
                // Ensure date is formatted for HTML5 date input (yyyy-MM-dd)
                issueDate = asset.IssueDate.ToString("yyyy-MM-dd")
            }, JsonRequestBehavior.AllowGet);
        }

    }




}