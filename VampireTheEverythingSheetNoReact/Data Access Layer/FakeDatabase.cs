using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Data;
using System.Xml;
using VampireTheEverythingSheetNoReact.Data_Access_Layer.FakeData;
using VampireTheEverythingSheetNoReact.Models;
using VampireTheEverythingSheetNoReact.Models.DB;
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
    public class FakeDatabase : DatabaseAccessLayer
    {
        #region Public members

        /// <summary>
        /// Returns the singleton instance of this database.
        /// </summary>
        /// <returns></returns>
        public static new DatabaseAccessLayer GetDatabase()
        {
            return _db;
        }

        public override Character GetCharacterData(int uniqueID)
        {
            string path = Path.Combine(CharacterSavePath, "char" + uniqueID + ".txt");
            if(!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            if(!File.Exists(path))
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(new Character(uniqueID)));
            }

            return JsonConvert.DeserializeObject<Character>(File.ReadAllText(path)) 
                ?? throw new Exception("Character file " + path + " could not be parsed.");
        }

        public override void SaveCharacterData(Character character)
        {
            string path = Path.Combine(CharacterSavePath, character.UniqueID + ".txt");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            File.WriteAllText(path, JsonConvert.SerializeObject(character));
        }

        public override IEnumerable<DBRow> GetTraitTemplateData()
        {
            //Technically, we should do a deep copy here (and elsewhere in this class) to avoid mutability concerns, but this *is* a fake database anyway
            return from DataRow row in TraitTable.Data.Rows select new DBRow(row);
        }

        public override IEnumerable<DBRow> GetCharacterTemplateData()
        {
            return from DataRow row in CharacterTemplateTable.Data.Rows select new DBRow(row);
        }

        //TODO might need some renaming/redoing of this interface
        public override IEnumerable<DBRow> GetCharacterTemplateXTraitData()
        {
            return from DataRow row in CharacterTemplateXTraitTable.Data.Rows select new DBRow(row);
        }

        /// <summary>
        /// Returns a DataTable representing the moral Paths a character may follow.
        /// </summary>
        public override IEnumerable<DBRow> GetMoralPathData()
        {
            return from DataRow row in PathTable.Data.Rows select new DBRow(row);
        }

        public override ReadOnlyDictionary<string, SortedSet<int>> GetTraitIDsByName()
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

        private const string CharacterSavePath = "CharacterSaves";

        #endregion
    }
}
