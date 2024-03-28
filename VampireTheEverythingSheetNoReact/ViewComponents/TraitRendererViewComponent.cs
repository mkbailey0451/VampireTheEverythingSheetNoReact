using Microsoft.AspNetCore.Mvc;
using VampireTheEverythingSheetNoReact.Models;
using static VampireTheEverythingSheetNoReact.Shared_Files.VtEConstants;

namespace VampireTheEverythingSheetNoReact.ViewComponents
{
    public class TraitRendererViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(Trait trait)
        {
            switch(trait.Type)
            {
                case TraitType.FreeTextTrait:
                    return View("FreeTextTrait", trait);
                case TraitType.DropdownTrait:
                    return View("DropdownTrait", trait);
                case TraitType.IntegerTrait:
                    return View("IntegerTrait", trait);
            }
            return View("Default", trait);
        }
    }
}
