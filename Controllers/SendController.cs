using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using SRM.Models; // Ensure this matches your Agent model namespace
using SRM.Data;   // Ensure this matches your AppDbContext namespace

namespace SRM.Controllers
{
    [Authorize] // Optional: requires login to access
    public class SendController : Controller
    {
        private readonly AppDbContext _context = new AppDbContext();

        // GET: Send/SendSMS
        public ActionResult SendSMS()
        {
            // Fetch active agents to populate the recipient dropdown
            var agents = _context.agent
                                 .Where(a => a.Status == "A")
                                 .OrderBy(a => a.Name)
                                 .ToList();

            ViewBag.AgentList = agents;

            return View("~/Views/ServiceRequest/SendSMS.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProcessSMS(string agentPno, string message)
        {
            // Placeholder for your SMS API logic
            System.Diagnostics.Debug.WriteLine($"Recipient: {agentPno}, Message: {message}");

            TempData["Success"] = "SMS logic initialized.";
            return RedirectToAction("SendSMS");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _context.Dispose();
            base.Dispose(disposing);
        }
    }
}