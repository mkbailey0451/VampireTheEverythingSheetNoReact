using System.Collections.ObjectModel;
using System.Data;
using VampireTheEverythingSheetNoReact.Data_Access_Layer;
using VampireTheEverythingSheetNoReact.Models.DB;
using static VampireTheEverythingSheetNoReact.Shared_Files.VtEConstants;

namespace VampireTheEverythingSheetNoReact.Models
{
    public class CharacterTemplate
    {
        #region Public members
        /// <summary>
        /// A mapping of every TemplateKey to its unique CharacterTemplate instance.
        /// </summary>
        public static ReadOnlyDictionary<TemplateKey, CharacterTemplate> AllCharacterTemplates { get; private set; }

        /// <summary>
        /// The unique template ID of this character template.
        /// </summary>
        public TemplateKey UniqueID { get; set; }

        /// <summary>
        /// The name of this character template.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// An ordered enumeration of all trait IDs associated with this character template.
        /// </summary>
        public IEnumerable<int> TraitIDs
        {
            get
            {
                foreach (int id in _traitIDs)
                {
                    yield return id;
                }
            }
        }
        #endregion

        #region Private members

        static CharacterTemplate()
        {
            _db = VtEDatabaseAccessLayer.GetDatabase();
            AllCharacterTemplates = InitAllCharacterTemplates();
        }

        private static readonly DatabaseAccessLayer _db;

        private static ReadOnlyDictionary<TemplateKey, CharacterTemplate> InitAllCharacterTemplates()
        {
            IEnumerable<DBRow> templateTable = _db.GetCharacterTemplateData();

            if (!templateTable.Any())
            {
                return new(new Dictionary<TemplateKey, CharacterTemplate>());
            }

            Dictionary<TemplateKey, CharacterTemplate> templates = new(templateTable.Count());

            //in real life, grabbing the whole tables at once would save us a lot of queries, so we'll do it that way here too
            IEnumerable<DBRow> template_x_trait = _db.GetCharacterTemplateXTraitData();

            foreach (DBRow templateInfo in templateTable)
            {
                TemplateKey templateKey = (TemplateKey)templateInfo["CHAR_TEMPLATE_ID"];
                string templateName = (string)templateInfo["CHAR_TEMPLATE_NAME"];
                templates[templateKey] = new(
                        templateKey,
                        templateName,
                        from DBRow row in template_x_trait
                        where (int)row["CHAR_TEMPLATE_ID"] == (int)templateInfo["CHAR_TEMPLATE_ID"]
                        select (int)row["TRAIT_ID"]
                    );
            }

            return new(templates);
        }

        private CharacterTemplate(TemplateKey uniqueID, string name, IEnumerable<int> traitIDs)
        {
            UniqueID = uniqueID;
            Name = name;
            _traitIDs = new(traitIDs);
        }

        private readonly SortedSet<int> _traitIDs;
        #endregion
    }
}
