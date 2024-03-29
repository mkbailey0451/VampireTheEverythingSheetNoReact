namespace VampireTheEverythingSheetNoReact.Models.Component
{
    public class AutoColumnModel : RectangularControlModel
    {
        public required IEnumerable<Trait> Traits { get; set; }

        public int HeadingLevel { get; set; } = 0;

        public string HeadingText { get; set; } = "";
    }
}
