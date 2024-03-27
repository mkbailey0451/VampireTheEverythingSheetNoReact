using Microsoft.AspNetCore.Mvc;
using VampireTheEverythingSheetNoReact.Models;

namespace VampireTheEverythingSheetNoReact.ViewComponents
{
    public class AutoColumnViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(Trait[] traits, int column, int startRow)
        {
            ViewData["Traits"] = traits;
            ViewData["Column"] = column;
            ViewData["StartRow"] = startRow;
            return View("AutoColumn");
        }
    }
}
