using Microsoft.AspNetCore.Mvc;
using VampireTheEverythingSheetNoReact.Models;
using VampireTheEverythingSheetNoReact.Shared_Files;
using static VampireTheEverythingSheetNoReact.Shared_Files.VtEConstants;

namespace VampireTheEverythingSheetNoReact.ViewComponents
{
    public class PickerColumnViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(IEnumerable<Trait> traits, int column, int width, int startRow)
        {
            //I'm honestly not sure if running this LINQ query twice is faster or slower than allocating two new Lists and sorting this out in a foreach. It probably doesn't really matter.
            ViewData["VisibleTraits"] = from trait in traits where trait.Visible == TraitVisibility.Visible select trait;
            ViewData["PickableTraits"] = from trait in traits where trait.Visible == TraitVisibility.Selectable select trait;
            ViewData["Column"] = column;
            ViewData["Width"] = width;
            ViewData["StartRow"] = startRow;

            return View("PickerColumn");
        }
    }
}
