using System.Collections.ObjectModel;
using System.Data;
using VampireTheEverythingSheetNoReact.Data_Access_Layer;
using VampireTheEverythingSheetNoReact.Shared_Files;

namespace VampireTheEverythingSheetNoReact.Models
{
    public class MoralPath
    {
        //TODO: Do we need session memoization for these objects? Probably. Unless we want to simulate pulling from the DB every time?
        public static ReadOnlyDictionary<string, MoralPath> AllPaths { get; } = GetAllPaths();

        //TODO: Unique ID? Shouldn't be necessary, since oaths all have unique names, but is it more Best Practicey(TM)?

        /// <summary>
        /// The name of this Path.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The Virtues of this Path, such as "Conscience and Self-Control".
        /// </summary>
        public string Virtues { get; private set; }

        /// <summary>
        /// The Bearing of this Path.
        /// </summary>
        public string Bearing { get; private set; }

        /// <summary>
        /// An integer representing the Resolve Penalty, if any, of this Path.
        /// This is always 0, -1, or -2.
        /// </summary>
        public int ResolvePenalty { get; private set; }

        /// <summary>
        /// An ordered, enumerated list of example sins for this Path, listed by level and starting from the least severe.
        /// </summary>
        public IEnumerable<string> HierarchyOfSins
        {
            get
            {
                foreach (string sin in _hierarchyOfSins)
                {
                    yield return sin; //TODO: This will need Oath logic
                }
            }
        }

        private static ReadOnlyDictionary<string, MoralPath> GetAllPaths()
        {
            Dictionary<string, MoralPath> output = [];
            foreach (DataRow row in FakeDatabase.GetDatabase().GetPathData().Rows)
            {
                output[Utils.TryGetString(row["PATH_NAME"], "")] = new MoralPath(row);
            }
            return new ReadOnlyDictionary<string, MoralPath>(output);
        }

        private readonly List<string> _hierarchyOfSins;

        private MoralPath(DataRow row)
        {
            Name = Utils.TryGetString(row["PATH_NAME"], "");
            Virtues = Utils.TryGetString(row["VIRTUES"].ToString(), "");
            Bearing = Utils.TryGetString(row["BEARING"].ToString(), "");
            _hierarchyOfSins = new(Utils.TryGetString(row["HIERARCHY_OF_SINS"].ToString(), "").Split('\n'));

            ResolvePenalty = 0;
            if (!Virtues.Contains("conscience", StringComparison.CurrentCultureIgnoreCase))
            {
                ResolvePenalty--;
            }
            if (!Virtues.Contains("self-control", StringComparison.CurrentCultureIgnoreCase))
            {
                ResolvePenalty--;
            }
        }
    }
}
