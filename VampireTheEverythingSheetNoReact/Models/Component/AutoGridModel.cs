using Microsoft.AspNetCore.Html;

namespace VampireTheEverythingSheetNoReact.Models.Component
{
    public class AutoGridModel : RectangularControlModel
    {
        public AutoGridModel() : base("AutoGrid") { }

        public required int ColumnCount { get; set; }

        public required int BumperWidth { get; set; }

        public required IEnumerable<Task<IHtmlContent>> Elements { get; set; }

        public int HeadingLevel { get; set; } = 0;

        public string HeadingText { get; set; } = "";
    }
}
