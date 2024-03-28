using Microsoft.AspNetCore.Mvc;
using VampireTheEverythingSheetNoReact.Models;

namespace VampireTheEverythingSheetNoReact.ViewComponents
{
    public class AutoGridViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(IEnumerable<Trait> traits, IEnumerable<int> columnIndices, int width, int startRow)
        {
            ViewData["Traits"] = traits;
            ViewData["ColumnIndices"] = columnIndices;
            ViewData["Width"] = width;
            ViewData["StartRow"] = startRow;
            return View("AutoGrid");
        }
    }
}
