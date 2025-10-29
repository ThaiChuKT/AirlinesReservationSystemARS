using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ARS.Data;
using ARS.Models;

namespace ARS.Controllers
{
    public class TestController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TestController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Test/Database
        public async Task<IActionResult> Database()
        {
            var cities = await _context.Cities.ToListAsync();
            var pricingPolicies = await _context.PricingPolicies.ToListAsync();
            var users = await _context.Users.ToListAsync();

            ViewBag.Cities = cities;
            ViewBag.PricingPolicies = pricingPolicies;
            ViewBag.Users = users;
            ViewBag.DatabaseName = _context.Database.GetDbConnection().Database;

            return View();
        }
    }
}
