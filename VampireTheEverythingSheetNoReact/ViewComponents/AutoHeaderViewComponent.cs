using Microsoft.AspNetCore.Mvc;

namespace VampireTheEverythingSheetNoReact.ViewComponents
{
    public class AutoHeaderViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(int column, int width, int row, int headerLevel, string headerText)
        {
            ViewData["Column"] = column;
            ViewData["Width"] = width;
            ViewData["Row"] = row;
            ViewData["HeaderLevel"] = headerLevel;
            ViewData["HeaderText"] = headerText;
            return View("AutoHeader");
        }
    }
}
