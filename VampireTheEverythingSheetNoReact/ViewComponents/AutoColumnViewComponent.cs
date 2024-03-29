using Microsoft.AspNetCore.Mvc;
using VampireTheEverythingSheetNoReact.Models;
using VampireTheEverythingSheetNoReact.Models.Component;

namespace VampireTheEverythingSheetNoReact.ViewComponents
{
    public class AutoColumnViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(AutoColumnModel model)
        {
            return View("AutoColumn", model);
        }
    }
}
