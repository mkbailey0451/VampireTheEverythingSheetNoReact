using Microsoft.AspNetCore.Mvc;
using VampireTheEverythingSheetNoReact.Models.Component;

namespace VampireTheEverythingSheetNoReact.ViewComponents
{
    public class PickerColumnViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(PickerColumnModel model)
        {
            return View("PickerColumn", model);
        }
    }
}
