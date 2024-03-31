using Microsoft.AspNetCore.Html;

namespace VampireTheEverythingSheetNoReact.Models.Component
{
    public class GridElementModel : RectangularControlModel
    {
        public GridElementModel() : base("GridElement") { }

        public required Task<IHtmlContent> Content { get; set; }

        //unlike most controls, GridElement does *not* determine its own height
        public new required int Height { get; set; }
    }
}
