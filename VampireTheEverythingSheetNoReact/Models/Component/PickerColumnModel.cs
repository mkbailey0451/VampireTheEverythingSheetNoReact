using static VampireTheEverythingSheetNoReact.Shared_Files.VtEConstants;

namespace VampireTheEverythingSheetNoReact.Models.Component
{
    public class PickerColumnModel : RectangularControlModel
    {
        public required IEnumerable<Trait> Traits { get; set; }

        public IEnumerable<Trait> VisibleTraits { get { return from trait in Traits where trait.Visible == TraitVisibility.Visible select trait; } }

        public IEnumerable<Trait> SelectableTraits { get { return from trait in Traits where trait.Visible == TraitVisibility.Selectable select trait; } }
    }
}
