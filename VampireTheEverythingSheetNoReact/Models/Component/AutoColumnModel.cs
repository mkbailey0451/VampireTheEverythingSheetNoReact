using Microsoft.AspNetCore.Html;

namespace VampireTheEverythingSheetNoReact.Models.Component
{
    public class AutoColumnModel : RectangularControlModel
    {
        public AutoColumnModel() : base("AutoColumn") { }

        public required IEnumerable<Task<IHtmlContent>> Elements { get; set; }

        public int HeadingLevel { get; set; } = 0;

        public string HeadingText { get; set; } = "";
    }
}
