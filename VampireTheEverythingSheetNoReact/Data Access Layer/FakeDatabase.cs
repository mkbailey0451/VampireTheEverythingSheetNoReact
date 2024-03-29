using System.Collections.ObjectModel;
using System.Data;
using VampireTheEverythingSheetNoReact.Data_Access_Layer.FakeData;
using VampireTheEverythingSheetNoReact.Models;
using VampireTheEverythingSheetNoReact.Shared_Files;
using static VampireTheEverythingSheetNoReact.Shared_Files.VtEConstants;

namespace VampireTheEverythingSheetNoReact.Data_Access_Layer
{
    //TODO: Replace with a real database using LocalDB
    /// <summary>
    /// Normally, an application of this kind would interface to a SQL backend via a class much like this one.
    /// However, this project is designed as a demonstration of the author's ability to develop code in React, and
    /// to be able to run on a local machine with no connection to a database for demo purposes. As such, this
    /// fake database layer with hardcoded values is provided. An interface has been created to allow for its easy replacement
    /// in the case that such is desirable.
    /// </summary>
    public class FakeDatabase : IDatabaseAccessLayer
    {
        #region Public members

        /// <summary>
        /// Returns the singleton instance of this database.
        /// </summary>
        /// <returns></returns>
        public static IDatabaseAccessLayer GetDatabase()
        {
            return _db;
        }

        public IEnumerable<DataRow> GetTraitData()
        {
            //Technically, we should do a deep copy here (and elsewhere in this class) to avoid mutability concerns, but this *is* a fake database anyway
            return from DataRow row in TraitTable.Data.Rows select row;
        }

        public IEnumerable<DataRow> GetCharacterTemplateData()
        {
            return from DataRow row in CharacterTemplateTable.Data.Rows select row;
        }

        //TODO might need some renaming/redoing of this interface
        public IEnumerable<DataRow> GetCharacterTemplateXTraitData()
        {
            return from DataRow row in CharacterTemplateXTraitTable.Data.Rows select row;
        }

        /// <summary>
        /// Returns a DataTable representing the moral Paths a character may follow.
        /// </summary>
        public IEnumerable<DataRow> GetPathData()
        {
            return from DataRow row in PathTable.Data.Rows select row;
        }

        public ReadOnlyDictionary<string, SortedSet<int>> GetTraitIDsByName()
        {
            return TraitTable.TraitIDsByName;
        }

        #endregion

        #region Private members

        /// <summary>
        /// The singleton instance of this database.
        /// </summary>
        private static readonly FakeDatabase _db;

        static FakeDatabase()
        {
            _db = new();
        }

        /// <summary>
        /// Since this database is a singleton, we naturally want its constructor to be private.
        /// </summary>
        private FakeDatabase() { }

        #endregion
    }
}
