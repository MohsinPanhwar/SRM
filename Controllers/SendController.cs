using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using SRM.Models;
using SRM.Data;

namespace SRM.Controllers
{
    [Authorize]
    public class SendController : BaseController
    {
        private readonly AppDbContext _context = new AppDbContext();

        // GET: Send/SendSMS
        public ActionResult SendSMS()
        {
            // We fetch the full agent objects to ensure mobileno is available for the View's data-attribute
            var agents = _context.agent
                                 .Where(a => a.Status == "A")
                                 .OrderBy(a => a.Name)
                                 .ToList();

            ViewBag.AgentList = agents;

            // Using your specific view path
            return View("~/Views/ServiceRequest/SendSMS.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProcessSMS(string agentPno, string message)
        {
            if (string.IsNullOrEmpty(agentPno) || string.IsNullOrEmpty(message))
            {
                TempData["Error"] = "Recipient and message are required.";
                return RedirectToAction("SendSMS");
            }

            // 1. Fetch the actual agent to get their phone number for the SMS API
            var targetAgent = _context.agent.FirstOrDefault(a => a.Pno == agentPno);

            if (targetAgent != null && !string.IsNullOrEmpty(targetAgent.Mobile))
            {
                // 2. PLACEHOLDER: Call your SMS Gateway here
                // Example: MySmsProvider.Send(targetAgent.mobileno, message);

                System.Diagnostics.Debug.WriteLine($"Sending to: {targetAgent.Mobile}, Msg: {message}");
                TempData["Success"] = $"SMS sent successfully to {targetAgent.Name}!";
            }
            else
            {
                TempData["Error"] = "Could not find a valid mobile number for this agent.";
            }

            return RedirectToAction("SendSMS");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _context.Dispose();
            base.Dispose(disposing);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        public ActionResult SendPassword()
        {
            // We fetch the full agent objects to ensure mobileno is available for the View's data-attribute
            var agents = _context.agent
                                 .Where(a => a.Status == "A")
                                 .OrderBy(a => a.Name)
                                 .ToList();

            ViewBag.AgentList = agents;

            // Using your specific view path
            return View("~/Views/ServiceRequest/SendPassword.cshtml");
        }
    }
}