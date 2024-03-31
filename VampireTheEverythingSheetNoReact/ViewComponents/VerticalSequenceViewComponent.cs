using Microsoft.AspNetCore.Mvc;
using VampireTheEverythingSheetNoReact.Models.Component;

namespace VampireTheEverythingSheetNoReact.ViewComponents
{
    public class VerticalSequenceViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(VerticalSequenceModel model)
        {
            return View("VerticalSequence", model);
        }
    }
}
