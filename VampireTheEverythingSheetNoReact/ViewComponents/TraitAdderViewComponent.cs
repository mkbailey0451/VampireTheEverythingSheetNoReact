using Microsoft.AspNetCore.Mvc;
using VampireTheEverythingSheetNoReact.Models;

namespace VampireTheEverythingSheetNoReact.ViewComponents
{
    public class TraitAdderViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(Trait[] traits, int column, int row)
        {
            ViewData["Traits"] = traits;
            ViewData["Column"] = column;
            ViewData["Row"] = row;
            return View("TraitAdder");
        }
    }
}
