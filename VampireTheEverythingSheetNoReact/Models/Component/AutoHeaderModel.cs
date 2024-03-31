namespace VampireTheEverythingSheetNoReact.Models.Component
{
    public class AutoHeaderModel : RectangularControlModel
    {
        public AutoHeaderModel() : base("AutoHeader") { }

        public required int HeadingLevel { get; set; }
        public required string HeadingText { get; set; }
    }
}
