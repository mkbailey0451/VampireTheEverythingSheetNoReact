using System.Collections.ObjectModel;
using System.Data;
using VampireTheEverythingSheetNoReact.Data_Access_Layer;
using static VampireTheEverythingSheetNoReact.Shared_Files.VtEConstants;

namespace VampireTheEverythingSheetNoReact.Models
{
    public class CharacterTemplate
    {
        #region Public members
        /// <summary>
        /// A mapping of every TemplateKey to its unique CharacterTemplate instance.
        /// </summary>
        public static ReadOnlyDictionary<TemplateKey, CharacterTemplate> AllCharacterTemplates { get; } = new ReadOnlyDictionary<TemplateKey, CharacterTemplate>(GetAllCharacterTemplates());

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
        private static SortedDictionary<TemplateKey, CharacterTemplate> GetAllCharacterTemplates()
        {
            DataTable templateData = FakeDatabase.GetDatabase().GetCharacterTemplateData();

            if (templateData.Rows.Count == 0)
            {
                return [];
            }

            //We do this stupid loop-and-a-half thing because it lets us avoid a lot of type/ID/null checking and casting in the main body of the loop.
            TemplateKey templateKey = (TemplateKey)templateData.Rows[0]["TEMPLATE_ID"]; //TODO: Better type safety? Or should we trust the DB and let the exceptions get thrown? What exception even is this?
            CharacterTemplate? template = new(templateKey, (string)templateData.Rows[0]["TEMPLATE_NAME"]);


            SortedDictionary<TemplateKey, CharacterTemplate> output = new() { { templateKey, template } };

            for (int x = 1; x < templateData.Rows.Count; x++)
            {
                DataRow row = templateData.Rows[x];

                //In most cases, we can continue with the same template as we were just using, thanks to the ordering of data coming back from the DB.
                //This saves having to try to retrieve it every time.
                TemplateKey currentKey = (TemplateKey)row["TEMPLATE_ID"];
                if (currentKey != templateKey)
                {
                    templateKey = currentKey;
                    if (!output.TryGetValue(templateKey, out template))
                    {
                        template = new CharacterTemplate(templateKey, (string)row["TEMPLATE_NAME"]);
                        output.Add(templateKey, template);
                    }
                }

                template._traitIDs.Add((int)row["TRAIT_ID"]);
            }

            return output;
        }

        private CharacterTemplate(TemplateKey uniqueID, string name)
        {
            UniqueID = uniqueID;
            Name = name;
            _traitIDs = [];
        }

        private readonly SortedSet<int> _traitIDs = [];
        #endregion
    }
}
