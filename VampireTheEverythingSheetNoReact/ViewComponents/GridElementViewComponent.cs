using Microsoft.AspNetCore.Mvc;
using VampireTheEverythingSheetNoReact.Models;

namespace VampireTheEverythingSheetNoReact.ViewComponents
{
    public class GridElementViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(ViewComponent contents, int column, int row, int width = 1, int height = 1)
        {
            ViewData["Contents"] = contents;
            ViewData["Column"] = column;
            ViewData["Row"] = row;
            ViewData["Width"] = width;
            ViewData["Height"] = height;
            return View("GridElement");
        }
    }
}
