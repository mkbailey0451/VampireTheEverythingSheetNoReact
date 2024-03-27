using Microsoft.AspNetCore.Mvc;
using VampireTheEverythingSheetNoReact.Models;

namespace VampireTheEverythingSheetNoReact.ViewComponents
{
    public class TraitRendererViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(Trait[] trait)
        {
            ViewData["Trait"] = trait;
            return View("TraitAdder");
        }
    }
}
