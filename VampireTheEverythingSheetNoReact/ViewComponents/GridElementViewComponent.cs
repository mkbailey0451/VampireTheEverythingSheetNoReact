using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using VampireTheEverythingSheetNoReact.Models;
using VampireTheEverythingSheetNoReact.Models.Component;

namespace VampireTheEverythingSheetNoReact.ViewComponents
{
    public class GridElementViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(GridElementModel model)
        {
            return View("GridElement", model);
        }
    }
}
