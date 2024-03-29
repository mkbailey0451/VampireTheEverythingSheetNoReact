namespace VampireTheEverythingSheetNoReact.Models.Component
{
    public abstract class RectangularControlModel
    {
        public required int Row {  get; set; }
        public required int Column { get; set; }
        //controls generally determine their own height
        public int? Height { get; set; }
        public required int Width { get; set; }

        public int RowEnd { get { return Row + (Height ?? 1); } }

        public int ColumnEnd { get { return Column + Width; } }
    }
}
