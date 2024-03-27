using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using VampireTheEverythingSheetNoReact.Models;

namespace VampireTheEverythingSheetNoReact.ViewComponents
{
    public class GridElementViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(Task<IHtmlContent> contents, int column, int row, int width, int height)
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
