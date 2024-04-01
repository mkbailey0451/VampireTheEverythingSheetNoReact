using System.Data;
using VampireTheEverythingSheetNoReact.Models;
using VampireTheEverythingSheetNoReact.Shared_Files;
using static VampireTheEverythingSheetNoReact.Shared_Files.VtEConstants;

namespace VampireTheEverythingSheetNoReact.Data_Access_Layer.FakeData
{
    public static class CharacterTemplateXTraitTable
    {
        public static DataTable Data { get; private set; }

        static CharacterTemplateXTraitTable()
        {
            Data = BuildData();
        }

        private static DataTable BuildData()
        {
            DataTable output = new()
            {
                Columns =
                {
                    new DataColumn("TEMPLATE_ID", typeof(int)),
                    new DataColumn("TRAIT_ID", typeof(int)),
                }
            };

            BuildTemplate(output, TemplateKey.Mortal,
                [
                    "Name",
                    "Player",
                    "Chronicle",
                    "Nature",
                    "Demeanor",
                    "Concept",

                    "Strength",
                    "Dexterity",
                    "Stamina",

                    "Charisma",
                    "Manipulation",
                    "Composure",

                    "Intelligence",
                    "Wits",
                    "Resolve",

                    "Athletics",
                    "Brawl",
                    "Drive",
                    "Firearms",
                    "Larceny",
                    "Stealth",
                    "Survival",
                    "Weaponry",

                    "Animal Ken",
                    "Empathy",
                    "Expression",
                    "Intimidation",
                    "Persuasion",
                    "Socialize",
                    "Streetwise",
                    "Subterfuge",

                    "Academics",
                    "Computer",
                    "Crafts",
                    "Investigation",
                    "Medicine",
                    "Occult",
                    "Politics",
                    "Science",

                    "True Faith",

                    "Allies",
                    "Alternate Identity",
                    "Contacts",
                    "Fame",
                    "Influence",
                    "Mentor",
                    "Resources",
                    "Retainers",

                    "Size",
                    "Health",
                    "Willpower",
                    "Defense",
                    "Speed",
                    "Run Speed",
                    "Initiative",
                    "Soak",

                    "Path Name",
                    "Path Score",
                    "Effective Humanity",
                    "Virtues",
                    "Bearing",
                    "Resolve Penalty",
                    "Stains",


                    //TODO: Weapons, Physical Description
                ]);

            //TODO: Other templates

            return output;
        }

        private static void BuildTemplate(DataTable template_x_trait, TemplateKey key, IEnumerable<string> traitNames)
        {
            int templateID = (int)key;
            DataRowCollection rows = template_x_trait.Rows;

            //All traits belonging to the template
            SortedSet<int> allTraits = new(AllTraitsByNames(traitNames));

            //Traits to search for subtraits of - starts off identical to the above set
            SortedSet<int> searchTraits = new(allTraits);

            //Subtraits found in a given iteration of the search
            SortedSet<int> allFoundSubtraits;

            do
            {
                allFoundSubtraits = [];

                foreach (int trait in searchTraits)
                {
                    //if the trait ID is valid (it should always be)
                    if (TraitTable.TraitsByID.TryGetValue(trait, out DataRow? traitData))
                    {
                        //we split up the subtraits of its trait data
                        IEnumerable<int> foundSubtraits =
                            from string idString in (traitData["SUBTRAITS"] as string ?? "").Split(Utils.MiniChunkSplitter)
                            where !string.IsNullOrEmpty(idString)
                            select int.Parse(idString);

                        //and add them all to the list of traits we found this iteration
                        allFoundSubtraits.UnionWith(foundSubtraits);
                    }
                }

                //add the found subtraits to the master list, or else why did we search for them in the first place
                allTraits.UnionWith(allFoundSubtraits);

                //next iteration we will search for subtraits of the subtraits we just found (if any)
                searchTraits = allFoundSubtraits;
            } while (allFoundSubtraits.Count != 0);

            foreach (int traitID in allTraits)
            {
                rows.Add(new object[] { templateID, traitID });
            }
        }

        private static IEnumerable<int> AllTraitsByNames(IEnumerable<string> traitNames)
        {
            foreach (string traitName in traitNames)
            {
                foreach (int traitID in TraitTable.TraitIDsByName[traitName])
                {
                    yield return traitID;
                }
            }
        }
    }
}
