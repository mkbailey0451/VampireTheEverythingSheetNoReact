using System.Collections.ObjectModel;
using System.Data;

namespace VampireTheEverythingSheetNoReact.Data_Access_Layer
{
    public interface IDatabaseAccessLayer
    {
        /// <summary>
        /// Get access to an instance of the access layer. The constructors for this subclass may be hidden to allow for singletons and load balancers.
        /// </summary>
        public abstract static IDatabaseAccessLayer GetDatabase();

        /// <summary>
        /// Returns a DataTable representing the TRAIT table in the database.
        /// </summary>
        public IEnumerable<DataRow> GetTraitData();

        /// <summary>
        /// Returns a DataTable representing the TRAIT table in the database.
        /// </summary>
        public ReadOnlyDictionary<string, SortedSet<int>> GetTraitIDsByName();

        /// <summary>
        /// Returns a DataTable representing all character templates in the database.
        /// </summary>
        public IEnumerable<DataRow> GetCharacterTemplateData();

        /// <summary>
        /// Returns a DataTable representing the mapping of character templates to traits in the database.
        /// </summary>
        public IEnumerable<DataRow> GetCharacterTemplateXTraitData();

        public IEnumerable<DataRow> GetPathData();

    }
}
