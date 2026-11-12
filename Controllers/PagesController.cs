using Microsoft.AspNetCore.Mvc;

namespace LivestockMarket.Controllers
{
    public class PagesController : Controller
    {
        public IActionResult About() => View();
    }
}
