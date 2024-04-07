using System.Collections.ObjectModel;
using System.Data;
using VampireTheEverythingSheetNoReact.Models;
using VampireTheEverythingSheetNoReact.Models.DB;

namespace VampireTheEverythingSheetNoReact.Data_Access_Layer
{
    public abstract class DatabaseAccessLayer
    {
        public abstract Character GetCharacterData(int uniqueID);

        public abstract void SaveCharacterData(Character character);

        /// <summary>
        /// Get access to an instance of the access layer. The constructors for this subclass may be hidden to allow for singletons and load balancers.
        /// </summary>
        public static DatabaseAccessLayer GetDatabase()
        {
            return VtEDatabaseAccessLayer.GetDatabase();
        }

        /// <summary>
        /// Returns a DataTable representing the TRAIT table in the database.
        /// </summary>
        public abstract IEnumerable<DBRow> GetTraitTemplateData();

        /// <summary>
        /// Returns a DataTable representing the TRAIT table in the database.
        /// </summary>
        public abstract ReadOnlyDictionary<string, SortedSet<int>> GetTraitIDsByName();

        /// <summary>
        /// Returns a DataTable representing all character templates in the database.
        /// </summary>
        public abstract IEnumerable<DBRow> GetCharacterTemplateData();

        /// <summary>
        /// Returns a DataTable representing the mapping of character templates to traits in the database.
        /// </summary>
        public abstract IEnumerable<DBRow> GetCharacterTemplateXTraitData();

        public abstract IEnumerable<DBRow> GetMoralPathData();

    }
}
