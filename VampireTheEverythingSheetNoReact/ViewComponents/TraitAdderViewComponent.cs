using Microsoft.AspNetCore.Mvc;
using VampireTheEverythingSheetNoReact.Models;

namespace VampireTheEverythingSheetNoReact.ViewComponents
{
    public class TraitAdderViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(Trait[] traits)
        {
            return View("TraitAdder", traits);
        }
    }
}
