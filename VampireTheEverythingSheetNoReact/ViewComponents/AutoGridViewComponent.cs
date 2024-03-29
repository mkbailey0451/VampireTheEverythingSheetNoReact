using Microsoft.AspNetCore.Mvc;
using VampireTheEverythingSheetNoReact.Models.Component;

namespace VampireTheEverythingSheetNoReact.ViewComponents
{
    public class AutoGridViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(AutoGridModel model)
        {
            return View("AutoGrid", model);
        }
    }
}
