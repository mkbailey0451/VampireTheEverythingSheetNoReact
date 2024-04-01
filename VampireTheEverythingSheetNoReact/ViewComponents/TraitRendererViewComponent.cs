using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VampireTheEverythingSheetNoReact.Models;
using static VampireTheEverythingSheetNoReact.Shared_Files.VtEConstants;

namespace VampireTheEverythingSheetNoReact.ViewComponents
{
    public class TraitRendererViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(Trait trait)
        {
            if(trait.Visible == TraitVisibility.Hidden)
            {
                return View("HiddenTrait", trait);
            }
            return trait.Type switch
            {
                TraitType.FreeTextTrait => View("FreeTextTrait", trait),
                TraitType.DropdownTrait => View("DropdownTrait", trait),
                TraitType.IntegerTrait => View("IntegerTrait", trait),
                _ => View("Default", trait),
            };
        }
    }
}
