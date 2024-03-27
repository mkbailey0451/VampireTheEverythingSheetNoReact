using Microsoft.AspNetCore.Mvc;

namespace VampireTheEverythingSheetNoReact.ViewComponents
{
    public class AutoHeaderViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(int columns, int row, int headerLevel, string headerText)
        {
            ViewData["Columns"] = columns;
            ViewData["Row"] = row;
            ViewData["HeaderLevel"] = headerLevel;
            ViewData["HeaderText"] = headerText;
            return View("AutoHeader");
        }
    }
}
