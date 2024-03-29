namespace VampireTheEverythingSheetNoReact.Models.Component
{
    public class AutoGridModel : RectangularControlModel
    {
        public required int ColumnCount { get; set; }

        public required int BumperWidth { get; set; }

        public required IEnumerable<Trait> Traits { get; set; }
    }
}
