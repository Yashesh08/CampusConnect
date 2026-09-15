using Microsoft.AspNetCore.Mvc;

namespace CampusConnect.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
}
