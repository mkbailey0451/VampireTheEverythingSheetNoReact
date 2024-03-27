using System.Collections.ObjectModel;
using System.Data;
using VampireTheEverythingSheetNoReact.Data_Access_Layer;
using VampireTheEverythingSheetNoReact.Shared_Files;
using static VampireTheEverythingSheetNoReact.Shared_Files.VtEConstants;

namespace VampireTheEverythingSheetNoReact.Models
{
    /// <summary>
    /// The TraitInfo class contains information on a specific Trait that can then be used to create a concrete example of that Trait 
    /// to be associated with a given Character. In other words, a specific character's Strength trait may be thought of as an "instance" of 
    /// the "class" represented by the TraitInfo instance corresponding to Strength traits in general. (These are both instances of separate
    /// classes in C#, but in the internal logic of the Storyteller System, the relation holds.)
    /// 
    /// This makes it easier to define character templates which can easily be applied to or removed from characters.
    /// </summary>
    public class TraitInfo
    {
        /// <summary>
        /// A complete listing of all trait information, keyed on the trait ID.
        /// </summary>
        public static ReadOnlyDictionary<int, TraitInfo> AllTraitInfo { get; } = GetAllTraitInfo();

        /// <summary>
        /// The trait ID of this Trait. Each different Trait has a unique ID.
        /// </summary>
        public int UniqueID { get; private set; }

        /// <summary>
        /// The name of the Trait, such as Strength, Path, or Generation. There are NOT guaranteed to be unique!
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The type of Trait, which determines its validation rules and how it is rendered on the front end.
        /// (This could have been implemented as subclasses, but the amount of duplicated code across different subclasses was becoming unreasonable.)
        /// </summary>
        public TraitType Type { get; private set; }

        /// <summary>
        /// The category of the Trait, which helps determine where on the page it will be rendered.
        /// </summary>
        public TraitCategory Category { get; private set; }

        /// <summary>
        /// The subcategory of the Trait, which helps determine where on the page it will be rendered and what character templates can use it.
        /// </summary>
        public TraitSubCategory SubCategory { get; private set; }

        public bool Visible { get; private set; }

        /// <summary>
        /// The TRAIT_DATA field from the database.
        /// It's easier to reprocess this every time (to ensure variables get properly registered and so on) than to try to sensibly copy all the data structures
        /// created by ProcessTraitData.
        /// </summary>
        public string Data { get; private set; }

        private static ReadOnlyDictionary<int, TraitInfo> GetAllTraitInfo()
        {
            SortedDictionary<int, TraitInfo> allTraits = [];
            foreach (DataRow row in FakeDatabase.GetDatabase().GetTraitData().Rows)
            {
                TraitInfo template = new(row);
                allTraits[template.UniqueID] = template;
            }
            return new ReadOnlyDictionary<int, TraitInfo>(allTraits);
        }

        private static ReadOnlyDictionary<string, List<int>> GetAllTraitIDsByName()
        {
            SortedDictionary<string, List<int>> allTraits = [];

            foreach(int traitID in AllTraitInfo.Keys)
            {
                string name = AllTraitInfo[traitID].Name;
                if(allTraits.TryGetValue(name, out List<int>? list))
                {
                    list.Add(traitID);
                }
                else
                {
                    allTraits[name] = [traitID];
                }
            }

            return new(allTraits);
        }

        private TraitInfo(DataRow row)
        {
            UniqueID = Utils.TryGetInt(row["TRAIT_ID"], -1);
            Name = Utils.TryGetString(row["TRAIT_NAME"], "");
            Type = (TraitType)Utils.TryGetInt(row["TRAIT_TYPE"], 0);
            Category = (TraitCategory)Utils.TryGetInt(row["TRAIT_CATEGORY"], 0);
            SubCategory = (TraitSubCategory)Utils.TryGetInt(row["TRAIT_SUBCATEGORY"], 0);

            Data = Utils.TryGetString(row["TRAIT_DATA"], "");

            Visible = VisibleByDefault();
        }

        private bool VisibleByDefault()
        {
            switch(Category)
            {
                case TraitCategory.Power:
                case TraitCategory.SpecificPower:
                case TraitCategory.Background:
                case TraitCategory.MeritFlaw:
                case TraitCategory.Weapon:
                    return false;
                default: return true;
            }
        }
    }
}
