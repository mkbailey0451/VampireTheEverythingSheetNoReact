using Microsoft.AspNetCore.Mvc;
using VampireTheEverythingSheetNoReact.Models;

namespace VampireTheEverythingSheetNoReact.ViewComponents
{
    public class TraitAdderViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(IEnumerable<Trait> traits)
        {
            return View("TraitAdder", traits);
        }
    }
    //TODO: Could we build a util function that passes back view controllers we need to invoke based on what data we have? That might solve the container problem... Or not.
}
