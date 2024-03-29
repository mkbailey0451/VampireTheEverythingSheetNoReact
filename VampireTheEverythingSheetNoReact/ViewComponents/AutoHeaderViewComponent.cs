using Microsoft.AspNetCore.Mvc;
using VampireTheEverythingSheetNoReact.Models.Component;

namespace VampireTheEverythingSheetNoReact.ViewComponents
{
    public class AutoHeaderViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(AutoHeaderModel model)
        {
            return View("AutoHeader", model);
        }
    }
}
