using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using VampireTheEverythingSheetNoReact.Data_Access_Layer;
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

        private static readonly DatabaseAccessLayer _db = DatabaseAccessLayer.GetDatabase();

        public IActionResult Index()
        {
            //TODO: Session saving

            ViewData["CharacterModel"] = _db.GetCharacterData(0);

            return View();
        }

        [HttpPost]
        public string UpdateTrait(int characterID, int traitID, object value)
        {
            Character character = _db.GetCharacterData(characterID);

            if (character == null)
            {
                Response.StatusCode = 404;
                return "";
            }

            Trait? trait = character.GetTrait(traitID);

            if(trait == null || !trait.TryAssign(value))
            {
                Response.StatusCode = 400;
                return "";
            }

            //TODO
            return "";
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
