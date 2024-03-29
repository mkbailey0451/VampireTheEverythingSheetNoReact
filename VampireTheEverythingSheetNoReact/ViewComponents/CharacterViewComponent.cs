using Microsoft.AspNetCore.Mvc;
using VampireTheEverythingSheetNoReact.Models;

namespace VampireTheEverythingSheetNoReact.ViewComponents
{
    public class CharacterViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(Character model)
        {
            return View("Character", model);
        }
    }
}
