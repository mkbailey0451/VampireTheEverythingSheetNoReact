using Microsoft.Data.SqlClient;
using System.Collections.ObjectModel;
using System.Data;
using VampireTheEverythingSheetNoReact.Models;
using VampireTheEverythingSheetNoReact.Models.DB;
using VampireTheEverythingSheetNoReact.Shared_Files;
using static VampireTheEverythingSheetNoReact.Shared_Files.VtEConstants;

namespace VampireTheEverythingSheetNoReact.Data_Access_Layer
{
    public class VtEDatabaseAccessLayer : DatabaseAccessLayer
    {
        #region Public interface

        public static new DatabaseAccessLayer GetDatabase()
        {
            return _db;
        }

        public override Character GetCharacterData(int uniqueID)
        {
            HashSet<TemplateKey> charTemplates = [];
            Dictionary<int, string> traitValues = [];

            foreach(DBRow row in GenericGet(@"
                SELECT 
	                CHARTEMP.CHARACTER_ID,
	                CHARTEMP.CHAR_TEMPLATE_ID,
	                CHAR_TEMPLATE_X_TRAIT.TRAIT_ID,
	                ISNULL(CHARACTER_DATA.TRAIT_VALUE, TRAIT_TEMPLATES.DEFAULT_VALUE) AS TRAIT_VALUE
                FROM
                (
	                SELECT DISTINCT
		                DUMMY.CHARACTER_ID,
		                ISNULL(CHARACTER_X_TEMPLATE.CHAR_TEMPLATE_ID, 0) AS CHAR_TEMPLATE_ID
	                FROM 
	                (
		                SELECT @UniqueId AS CHARACTER_ID
	                ) DUMMY
	                LEFT JOIN CHARACTER_X_TEMPLATE
	                ON DUMMY.CHARACTER_ID = CHARACTER_X_TEMPLATE.CHARACTER_ID
                ) CHARTEMP
                INNER JOIN CHAR_TEMPLATE_X_TRAIT
                ON CHARTEMP.CHAR_TEMPLATE_ID = CHAR_TEMPLATE_X_TRAIT.CHAR_TEMPLATE_ID
                INNER JOIN TRAIT_TEMPLATES
                ON CHAR_TEMPLATE_X_TRAIT.TRAIT_ID = TRAIT_TEMPLATES.TRAIT_ID
                LEFT JOIN CHARACTER_DATA
                ON CHARTEMP.CHARACTER_ID = CHARACTER_DATA.CHARACTER_ID
	                AND TRAIT_TEMPLATES.TRAIT_ID = CHARACTER_DATA.TRAIT_ID",
                new SqlParameter("UniqueId", uniqueID)))
            {
                charTemplates.Add((TemplateKey)Utils.TryGetInt(row["CHAR_TEMPLATE_ID"], (int)TemplateKey.Mortal));
                traitValues[Utils.TryGetInt(row["TRAIT_ID"], 0)] = Utils.TryGetString(row["TRAIT_VALUE"], "");
            }

            return new Character(uniqueID, charTemplates, traitValues);
        }

        public override void SaveCharacterData(Character character)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<DBRow> GetTraitTemplateData()
        {
            return GenericGetCached(
                @"
                    SELECT
                        TRAIT_ID,
                        TRAIT_NAME,
                        TRAIT_TYPE,
                        TRAIT_CATEGORY,
                        TRAIT_SUBCATEGORY,
                        TRAIT_DATA,
                        SUBTRAITS,
                        DEFAULT_VALUE
                    FROM
                        TRAIT_TEMPLATES
                ");
        }

        public override IEnumerable<DBRow> GetCharacterTemplateData()
        {
            return GenericGetCached(
                @"
                    SELECT
                        CHAR_TEMPLATE_ID,
                        CHAR_TEMPLATE_NAME
                    FROM
                        CHARACTER_TEMPLATES
                ");
        }

        public override IEnumerable<DBRow> GetCharacterTemplateXTraitData()
        {
            return GenericGetCached(
                @"
                    SELECT
                        CHAR_TEMPLATE_ID,
                        TRAIT_ID
                    FROM
                        CHAR_TEMPLATE_X_TRAIT
                ");
        }

        public override IEnumerable<DBRow> GetMoralPathData()
        {
            return GenericGetCached(
                @"
                    SELECT
                        PATH_ID,
                        PATH_NAME,
                        VIRTUES,
                        BEARING,
                        HIERARCHY_OF_SINS
                    FROM
                        MORAL_PATHS
                ");
        }

        public override ReadOnlyDictionary<string, SortedSet<int>> GetTraitIDsByName()
        {
            return _traitIDsByName;
        }

        #endregion

        #region Private members

        /// <summary>
        /// The singleton instance of this database.
        /// </summary>
        private static readonly VtEDatabaseAccessLayer _db;

        static VtEDatabaseAccessLayer()
        {
            _db = new();
        }

        private const string connectionString = 
            @"Data Source=(localdb)\MSSQLLocalDB;
            Initial Catalog=VtEDatabase;
            Integrated Security=True;
            Connect Timeout=30;
            Encrypt=False;
            Trust Server Certificate=False;
            Application Intent=ReadWrite;
            Multi Subnet Failover=False";

        private VtEDatabaseAccessLayer()
        {
            _getCache = [];
            _traitIDsByName = BuildTraitIDsByName();
        }

        private readonly ReadOnlyDictionary<string, SortedSet<int>> _traitIDsByName;
        private ReadOnlyDictionary<string, SortedSet<int>> BuildTraitIDsByName()
        {
            IEnumerable<DBRow> traitTemplates = GetTraitTemplateData();

            Dictionary<string, SortedSet<int>> dict = new(traitTemplates.Count());

            foreach (DBRow traitTemplate in traitTemplates)
            {
                string name = Utils.TryGetString(traitTemplate["TRAIT_NAME"], "");
                int id = Utils.TryGetInt(traitTemplate["TRAIT_ID"], -1);

                if(dict.TryGetValue(name, out SortedSet<int>? ids))
                {
                    ids.Add(id);
                }
                else
                {
                    dict[name] = [id];
                }
            }

            return new(dict);
        }

        private readonly Dictionary<string, IEnumerable<DBRow>> _getCache;
        private IEnumerable<DBRow> GenericGetCached(string query)
        {
            if(!_getCache.TryGetValue(query, out IEnumerable<DBRow>? result))
            {
                result = GenericGet(query);
                _getCache[query] = result;
            }

            return result;
        }

        private IEnumerable<DBRow> GenericGet(string query, params SqlParameter[] parameters)
        {
            //TODO: may be better to keep a single live connection?
            using SqlConnection connection = new(connectionString);
            SqlDataReader reader;
            try
            {
                connection.Open();
                SqlCommand command = new(query, connection);
                if (parameters.Length > 0)
                {
                    command.Parameters.AddRange(parameters);
                }
                reader = command.ExecuteReader();
            }
            catch (Exception e)
            {
                //TODO log
                throw;
            }
            while (reader.Read())
            {
                yield return new DBRow(reader);
            }
        }

        private static void InitializeFromFakeDatabase()
        {
            //TODO delete from in different order if we ever have to do this again - FK violations
            DataTable[] collections =
            {
                //FakeData.CharacterTemplateTable.Data,
                //FakeData.CharacterTemplateXTraitTable.Data,
                //FakeData.PathTable.Data,
                FakeData.TraitTable.Data
            };

            foreach(DataTable table in collections)
            {
                //using (SqlConnection connection = new(connectionString))
                //{
                //    connection.Open();
                //    SqlCommand trunc = new SqlCommand("DELETE FROM " + table.TableName, connection);
                //    trunc.ExecuteNonQuery();
                //}

                SqlBulkCopy bulk = new(connectionString)
                {
                    DestinationTableName = "" + table.TableName,
                };
                try
                {
                    bulk.WriteToServer(table);
                }
                catch (Exception e)
                {
                }
            }
        }

        #endregion
    }
}
