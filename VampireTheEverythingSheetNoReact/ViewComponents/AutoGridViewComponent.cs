﻿using Microsoft.AspNetCore.Mvc;
using VampireTheEverythingSheetNoReact.Models;

namespace VampireTheEverythingSheetNoReact.ViewComponents
{
    public class AutoGridViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(Trait[] traits, int columns, int startRow)
        {
            ViewData["Traits"] = traits;
            ViewData["Columns"] = columns;
            ViewData["StartRow"] = startRow;
            return View("AutoGrid");
        }
    }
}
