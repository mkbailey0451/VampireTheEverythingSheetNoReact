using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using VampireTheEverythingSheetNoReact.Models;

namespace VampireTheEverythingSheetNoReact.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            //TODO: display the character sheet in its present state
            string? savedData = "";
                //SessionExtensions.GetString(HttpContext.Session, "CharacterData");

            ViewData["CharacterModel"] =
                string.IsNullOrEmpty(savedData)
                    ? new("testChar")
                    : JsonConvert.DeserializeObject<Character>(savedData);

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
