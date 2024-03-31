namespace VampireTheEverythingSheetNoReact.Models.Component
{
    public class VerticalSequenceModel : RectangularControlModel
    {
        public VerticalSequenceModel() : base("VerticalSequence") { }

        public required IEnumerable<RectangularControlModel> Contents { get; set; }
    }
}
