using Microsoft.AspNetCore.Mvc;
using VampireTheEverythingSheetNoReact.Models;

namespace VampireTheEverythingSheetNoReact.ViewComponents
{
    public class AutoColumnViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(IEnumerable<Trait> traits, int column, int width, int startRow)
        {
            ViewData["Traits"] = traits;
            ViewData["Column"] = column;
            ViewData["Width"] = width;
            ViewData["StartRow"] = startRow;
            return View("AutoColumn");
        }
    }
}
