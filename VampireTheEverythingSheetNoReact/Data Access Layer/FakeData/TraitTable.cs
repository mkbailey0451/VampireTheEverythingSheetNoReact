using System.Collections.ObjectModel;
using System.Data;
using VampireTheEverythingSheetNoReact.Shared_Files;
using static VampireTheEverythingSheetNoReact.Shared_Files.VtEConstants;

namespace VampireTheEverythingSheetNoReact.Data_Access_Layer.FakeData
{
    public static class TraitTable
    {
        public static DataTable Data { get; private set; }

        public static ReadOnlyDictionary<string, SortedSet<int>> TraitIDsByName { get; private set; }

        public static ReadOnlyDictionary<int, DataRow> TraitsByID { get; private set; }

        static TraitTable()
        {
            Data = BuildData();
            TraitIDsByName = BuildTraitIDsByName();
            TraitsByID = BuildTraitsByID();
        }

        /// <summary>
        /// A Dictionary mapping a trait name to all trait IDs which have that name.
        /// In most cases, this is a one-to-one mapping, but some traits share a name 
        /// (such as the Generation Background and its derived top trait, or magic paths belonging to multiple main Disciplines).
        /// </summary>
        private static ReadOnlyDictionary<string, SortedSet<int>> BuildTraitIDsByName()
        {
            Dictionary<string, SortedSet<int>> traitIDsByName = [];

            foreach(DataRow row in Data.Rows)
            {
                int traitID = (int)row["TRAIT_ID"];
                string traitName = (string)row["TRAIT_NAME"];

                if(traitIDsByName.TryGetValue(traitName, out SortedSet<int>? traitIDs))
                {
                    traitIDs.Add(traitID);
                }
                else
                {
                    traitIDsByName[traitName] = [traitID];
                }
            }

            return new ReadOnlyDictionary<string, SortedSet<int>>(traitIDsByName);
        }

        //This may be identical to just indexing the thing but I don't trust it and this will future-proof it anyway
        private static ReadOnlyDictionary<int, DataRow> BuildTraitsByID()
        {
            Dictionary<int, DataRow> traitsByID = [];

            foreach (DataRow row in Data.Rows)
            {
                int traitID = (int)row["TRAIT_ID"];
                traitsByID[traitID] = row;
            }

            return new ReadOnlyDictionary<int, DataRow>(traitsByID);
        }

        private static DataTable BuildData()
        {
            DataTable traitTable = new()
            {
                Columns =
                {
                    new DataColumn("TRAIT_ID", typeof(int)),
                    new DataColumn("TRAIT_NAME", typeof(string)),
                    new DataColumn("TRAIT_TYPE", typeof(string)),
                    new DataColumn("TRAIT_CATEGORY", typeof(int)),
                    new DataColumn("TRAIT_SUBCATEGORY", typeof(int)),
                    new DataColumn("TRAIT_DATA", typeof(string)),
                    new DataColumn("SUBTRAITS", typeof(string)),
                }
            };

            object?[][] rawTraits = RawData();

            const int traitDataColumn = 5;

            //maps main trait IDs to the listing of subtrait IDs for them - we won't be passing SUBTRAIT chunks out to the caller in TRAIT_DATA, so we build SUBTRAITS using this
            Dictionary<int, SortedSet<int>> subtraitMap = [];

            //do some processing to the data, like associatting trait names to actual trait IDs - things we'd normally do in the DB as part of table creation
            TraitTableCleanup(rawTraits, traitDataColumn, subtraitMap);

            //build the actual datatable rows
            foreach (object?[] row in rawTraits)
            {
                int traitID = row[0] as int? ?? -1;
                string subtraits;
                if(subtraitMap.TryGetValue(traitID, out SortedSet<int>? subtraitSet))
                {
                    subtraits = string.Join(Utils.MiniChunkSplitter, subtraitSet);
                }
                else
                {
                    subtraits = "";
                }
                traitTable.Rows.Add([
                    row[0],
                    row[1],
                    row[2],
                    row[3],
                    row[4],
                    row.Length > 5
                        ? row[5]
                        : "",
                    subtraits
                ]);
            }

            return traitTable;
        }

        private static void TraitTableCleanup(object?[][] rawTraits, int traitDataColumn, Dictionary<int, SortedSet<int>> subtraitMap)
        {
            //maps the names of main traits to their IDs
            Dictionary<string, int> mainTraitMap = [];

            //at this level, we mainly want to just break it down by rows so we don't get stuck deep in nested-loop madness
            foreach (object?[] row in rawTraits)
            {
                //no need to sanitize rows with no TRAIT_DATA
                if (row.Length > traitDataColumn)
                {
                    TraitRowCleanup(row, traitDataColumn, mainTraitMap, subtraitMap);
                }
            }
        }

        private static void TraitRowCleanup(object?[] row, int traitDataColumn, Dictionary<string, int> mainTraitMap, Dictionary<int, SortedSet<int>> subtraitMap)
        {
            //now we're at row scope, so we need the trait ID and name - these will be needed for registering the trait as various things
            int traitID = row[0] as int? ?? -1;
            string traitName = row[1] as string ?? "";

            //we also, naturally, need the trait data
            string? traitData = row[traitDataColumn] as string;

            //this should never happen but we check anyway
            if (string.IsNullOrEmpty(traitData))
            {
                return;
            }

            //each "chunk" defines its own behavior, so we need to check them all
            List<string> chunks = new(traitData.Split(Utils.ChunkSplitter));

            for (int x = 0; x < chunks.Count; x++)
            {
                //the chunks are sometimes changed, so we need to update them in our array - this is why we can't use a foreach
                string chunk = TraitChunkCleanup(chunks[x], traitID, traitName, mainTraitMap, subtraitMap);

                //Some chunks can be nullified by this process
                if(string.IsNullOrEmpty(chunk))
                {
                    chunks.RemoveAt(x);
                    x--;
                    continue;
                }

                chunks[x] = chunk;
            }

            //fortunately, we maintain a reference to the original data at this level, so we can update the full trait data like this
            row[traitDataColumn] = string.Join(Utils.ChunkSplitter, chunks);
        }

        private static string TraitChunkCleanup(string chunk, int traitID, string traitName, Dictionary<string, int> mainTraitMap, Dictionary<int, SortedSet<int>> subtraitMap)
        {
            if (string.IsNullOrEmpty(chunk))
            {
                return chunk;
            }

            //each chunk has "mini-chunks", which work like function parameters

            string[] miniChunks = chunk.Split(Utils.MiniChunkSplitter);

            //main traits need to be registered as such, so we can reference them later
            if (miniChunks[0] == VtEKeywords.MainTraitCount || miniChunks[0] == VtEKeywords.MainTraitMax)
            {
                if (miniChunks.Length > 1)
                {
                    //this is a main trait with a special annotated name, which must be added to the registry
                    mainTraitMap[miniChunks[1]] = traitID;
                }
                else
                {
                    //this is a main trait without a special annotated name, so we add the trait's actual name instead
                    mainTraitMap[traitName] = traitID;
                }
                //either way, we don't keep the annotation and just return the main trait designator - the subtraits will reference the main trait by ID so it's not needed anymore
                return miniChunks[0];
            }

            //subtraits need to stop referencing names (or annotated names) and use the ID instead
            if (miniChunks[0] == VtEKeywords.SubTrait)
            {
                if (mainTraitMap.TryGetValue(miniChunks[1], out int mainTraitID))
                {
                    //one main trait can have many subtraits, so we use sets to map this relationship
                    if (subtraitMap.TryGetValue(mainTraitID, out SortedSet<int>? subtraitSet))
                    {
                        subtraitSet.Add(traitID);
                    }
                    else
                    {
                        subtraitMap[mainTraitID] = [traitID];
                    }

                    return string.Join(Utils.MiniChunkSplitter, miniChunks[0], mainTraitID);
                }
                //If you get this exception, it's probably because you left MainTraitCount or MainTraitMax out of a main trait's data definition.
                throw new ArgumentOutOfRangeException("Could not find trait ID for subtrait lookup key " + miniChunks[1]);
            }

            //TODO: any further cleanup as needed

            return chunk;
        }

        private static object?[][] RawData()
        {
            //template:
            /*
                [
                    traitID++,
                    "",
                    (int)TraitType.FreeTextTrait,
                    (int)TraitCategory.TopText,
                    (int)TraitSubCategory.None,
                    ""
                ],
             */
            int traitID = 0;
            return
            [
                #region Hidden Traits
                //TODO: Any?
                #endregion

                #region Top Traits
                [
                    traitID++,
                    "Name", //name
                    (int)TraitType.FreeTextTrait,
                    (int)TraitCategory.TopText,
                    (int)TraitSubCategory.None
                ],
                [
                    traitID++,
                    "Player", //name
                    (int)TraitType.FreeTextTrait,
                    (int)TraitCategory.TopText,
                    (int)TraitSubCategory.None
                ],
                [
                    traitID++,
                    "Chronicle", //name
                    (int)TraitType.FreeTextTrait,
                    (int)TraitCategory.TopText,
                    (int)TraitSubCategory.None
                ],
                [
                    traitID++,
                    "Nature", //name
                    (int)TraitType.DropdownTrait,
                    (int)TraitCategory.TopText,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.PossibleValues}{Utils.MiniChunkSplitter}" + string.Join(Utils.MiniChunkSplitter, RefTables.Archetypes())
                ],
                [
                    traitID++,
                    "Demeanor", //name
                    (int)TraitType.DropdownTrait,
                    (int)TraitCategory.TopText,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.PossibleValues}{Utils.MiniChunkSplitter}" + string.Join(Utils.MiniChunkSplitter, RefTables.Archetypes())
                ],
                [
                    traitID++,
                    "Concept", //name
                    (int)TraitType.FreeTextTrait,
                    (int)TraitCategory.TopText,
                    (int)TraitSubCategory.None
                ],
                [
                    traitID++,
                    "Clan", //name
                    (int)TraitType.DropdownTrait,
                    (int)TraitCategory.TopText,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.PossibleValues}{Utils.MiniChunkSplitter}" + string.Join(Utils.MiniChunkSplitter, RefTables.Clans())
                ],
                [
                    traitID++,
                    "Generation", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.TopText,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.IsVar}{Utils.MiniChunkSplitter}GENERATION"
                ],
                [
                    traitID++,
                    "Sire", //name
                    (int)TraitType.FreeTextTrait,
                    (int)TraitCategory.TopText,
                    (int)TraitSubCategory.None
                ],
                [
                    traitID++,
                    "Brood", //name
                    (int)TraitType.DropdownTrait,
                    (int)TraitCategory.TopText,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.IsVar}{Utils.MiniChunkSplitter}BROOD{Utils.ChunkSplitter}{VtEKeywords.PossibleValues}{Utils.MiniChunkSplitter}" + string.Join(Utils.MiniChunkSplitter, RefTables.Broods())
                ],
                [
                    traitID++,
                    "Breed", //name
                    (int)TraitType.DropdownTrait,
                    (int)TraitCategory.TopText,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.DerivedOption}{Utils.MiniChunkSplitter}[animal]{Utils.MiniChunkSplitter}BROOD{Utils.MiniChunkSplitter}" + string.Join(Utils.MiniChunkSplitter, RefTables.BroodBreedSwitch()) + $"{Utils.MiniChunkSplitter}{VtEKeywords.PossibleValues}{Utils.MiniChunkSplitter}" + string.Join(Utils.MiniChunkSplitter, RefTables.Breeds())
                ],
                [
                    traitID++,
                    "Tribe", //name
                    (int)TraitType.DropdownTrait,
                    (int)TraitCategory.TopText,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.PossibleValues}{Utils.MiniChunkSplitter}" + string.Join(Utils.MiniChunkSplitter, RefTables.Tribes())
                ],
                [
                    traitID++,
                    "Auspice", //name
                    (int)TraitType.DropdownTrait,
                    (int)TraitCategory.TopText,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.PossibleValues}{Utils.MiniChunkSplitter}" + string.Join(Utils.MiniChunkSplitter, RefTables.Auspices())
                ],
                //TODO more
                #endregion

                #region Attributes
                [
                    traitID++,
                    "Strength", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Attribute,
                    (int)TraitSubCategory.Physical,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}1{Utils.MiniChunkSplitter}TRAITMAX" //min, max
                ],
                [
                    traitID++,
                    "Dexterity", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Attribute,
                    (int)TraitSubCategory.Physical,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}1{Utils.MiniChunkSplitter}TRAITMAX"
                ],
                [
                    traitID++,
                    "Stamina", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Attribute,
                    (int)TraitSubCategory.Physical,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}1{Utils.MiniChunkSplitter}TRAITMAX"
                ],

                [
                    traitID++,
                    "Charisma", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Attribute,
                    (int)TraitSubCategory.Social,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}1{Utils.MiniChunkSplitter}TRAITMAX"
                ],
                [
                    traitID++,
                    "Manipulation", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Attribute,
                    (int)TraitSubCategory.Social,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}1{Utils.MiniChunkSplitter}TRAITMAX"
                ],
                [
                    traitID++,
                    "Composure", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Attribute,
                    (int)TraitSubCategory.Social,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}1{Utils.MiniChunkSplitter}TRAITMAX"
                ],

                [
                    traitID++,
                    "Intelligence", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Attribute,
                    (int)TraitSubCategory.Mental,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}1{Utils.MiniChunkSplitter}TRAITMAX"
                ],
                [
                    traitID++,
                    "Wits", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Attribute,
                    (int)TraitSubCategory.Mental,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}1{Utils.MiniChunkSplitter}TRAITMAX"
                ],
                [
                    traitID++,
                    "Resolve", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Attribute,
                    (int)TraitSubCategory.Mental,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}1{Utils.MiniChunkSplitter}TRAITMAX"
                ],
                #endregion

                #region Skills
                [
                    traitID++,
                    "Athletics", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Physical,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX"
                ],
                [
                    traitID++,
                    "Brawl", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Physical,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX"
                ],
                [
                    traitID++,
                    "Drive", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Physical,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX"
                ],
                [
                    traitID++,
                    "Firearms", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Physical,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX"
                ],
                [
                    traitID++,
                    "Larceny", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Physical,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX"
                ],
                [
                    traitID++,
                    "Stealth", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Physical,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX"
                ],
                [
                    traitID++,
                    "Survival", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Physical,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX"
                ],
                [
                    traitID++,
                    "Weaponry", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Physical,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX"
                ],

                [
                    traitID++,
                    "Animal Ken", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Social,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX"
                ],
                [
                    traitID++,
                    "Empathy", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Social,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX"
                ],
                [
                    traitID++,
                    "Expression", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Social,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX"
                ],
                [
                    traitID++,
                    "Intimidation", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Social,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX"
                ],
                [
                    traitID++,
                    "Persuasion", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Social,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX"
                ],
                [
                    traitID++,
                    "Socialize", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Social,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX"
                ],
                [
                    traitID++,
                    "Streetwise", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Social,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX"
                ],
                [
                    traitID++,
                    "Subterfuge", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Social,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX"
                ],

                [
                    traitID++,
                    "Academics", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Mental,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX"
                ],
                [
                    traitID++,
                    "Computer", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Mental,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX"
                ],
                [
                    traitID++,
                    "Crafts", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Mental,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX"
                ],
                [
                    traitID++,
                    "Investigation", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Mental,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX"
                ],
                [
                    traitID++,
                    "Medicine", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Mental,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX"
                ],
                [
                    traitID++,
                    "Occult", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Mental,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX"
                ],
                [
                    traitID++,
                    "Politics", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Mental,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX"
                ],
                [
                    traitID++,
                    "Science", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Mental,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX"
                ],
                #endregion

                [
                    traitID++,
                    "True Faith", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Faith,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}5{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],

                //TODO: Physical Disciplines/etc need to have specific powers implemented a special way if we want to have all derived ratings - maybe don't and have a MINUSCOUNT rule or something?
                #region Disciplines
                [
                    traitID++,
                    "Animalism", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Auspex", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Celerity", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Chimerstry", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Dementation", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Dominate", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Fortitude", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Necromancy", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.MainTraitMax}"
                ],
                [
                    traitID++,
                    "The Ash Path", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Necromancy{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "The Bone Path", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Necromancy{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "The Cenotaph Path", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Necromancy{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "The Corpse in the Monster", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Necromancy{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "The Grave’s Decay", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Necromancy{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "The Path of the Four Humors", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Necromancy{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "The Sepulchre Path", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Necromancy{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "The Vitreous Path", //name
                    (int)TraitType.DerivedTrait, //TODO: Might beome IntegerTrait
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Necromancy{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Obeah", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Obfuscate", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Obtenebration", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Potence", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Presence", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Protean", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Quietus", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Serpentis", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Thaumaturgy", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.MainTraitMax}"
                ],
                [
                    traitID++,
                    "Elemental Mastery", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Thaumaturgy{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "The Green Path", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Thaumaturgy{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Hands of Destruction", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Thaumaturgy{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Movement of the Mind", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Thaumaturgy"
                ],
                [
                    traitID++,
                    "Neptune’s Might", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Thaumaturgy{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "The Lure of Flames", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Thaumaturgy"
                ],
                [
                    traitID++,
                    "The Path of Blood", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Thaumaturgy{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "The Path of Conjuring", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Thaumaturgy{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "The Path of Corruption", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Thaumaturgy{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "The Path of Mars", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Thaumaturgy{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "The Path of Technomancy", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Thaumaturgy{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "The Path of the Father’s Vengeance", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Thaumaturgy{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Weather Control", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Thaumaturgy"
                ],
                [
                    traitID++,
                    "Vicissitude", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],

                //Branch Disciplines
                [
                    traitID++,
                    "Ogham", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Temporis", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Valeren", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],


                [
                    traitID++,
                    "Assamite Sorcery", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.MainTraitMax}"
                ],
                [
                    traitID++,
                    "Awakening of the Steel", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Assamite Sorcery{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}{Utils.MiniChunkSplitter}A:Awakening of the Steel"
                ],
                [
                    traitID++,
                    "Hands of Destruction", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Assamite Sorcery{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}{Utils.MiniChunkSplitter}A:Hands of Destruction"
                ],
                [
                    traitID++,
                    "Movement of the Mind", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Assamite Sorcery"
                ],
                [
                    traitID++,
                    "The Lure of Flames", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Assamite Sorcery"
                ],
                [
                    traitID++,
                    "The Path of Blood", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Assamite Sorcery{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}{Utils.MiniChunkSplitter}A:The Path of Blood"
                ],
                [
                    traitID++,
                    "The Path of Conjuring", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Assamite Sorcery{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}{Utils.MiniChunkSplitter}A:The Path of Conjuring"
                ],

                [
                    traitID++,
                    "Bardo", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Countermagic", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Daimoinon", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Flight", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Koldunic Sorcery", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.MainTraitMax}"
                ],
                [
                    traitID++,
                    "The Way of Earth", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Koldunic Sorcery{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "The Way of Fire", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Koldunic Sorcery{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "The Way of Water", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Koldunic Sorcery{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "The Way of Wind", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}MAGICMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Koldunic Sorcery{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Melpominee", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Mytherceria", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Sanguinus", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Visceratika", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.MainTraitCount}"
                ],
                #endregion

                //TODO: Lores, Tapestries, Knits, Arcana

                #region Specific Powers
                [
                    traitID++,
                    "Feral Whispers (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Animalism{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Beckoning (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Animalism{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Animal Succulence (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Animalism{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Quell the Beast (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Animalism{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Species Speech (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Animalism{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Subsume the Spirit (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Animalism{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Drawing Out the Beast (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Animalism{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shared Soul (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Animalism{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Heart of the Pack (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Animalism{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Conquer the Beast (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Animalism{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Nourish the Savage Beast (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Animalism{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Subsume the Pack (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Animalism{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Taunt The Caged Beast (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Animalism{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Heart of the Wild (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Animalism{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Unchain the Beast (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Animalism{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Animalism Supremacy (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Animalism{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Heightened Senses (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Auspex{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Aura Perception (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Auspex{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Spirit’s Touch (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Auspex{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Ever-Watchful Eye (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Auspex{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Telepathy (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Auspex{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Breach The Mind’s Sanctum (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Auspex{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Mind to Mind (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Auspex{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Karmic Sight (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Auspex{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Through Another’s Eyes (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Auspex{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Into Another’s Heart (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Auspex{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Eyes of the Grave (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Auspex{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "False Slumber (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Auspex{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Auspex Supremacy (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Auspex{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Enrich the Spirit (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Bardo{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Quell the Ravening Serpent (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Bardo{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Vows Unbroken (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Bardo{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Gift of Apis (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Bardo{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Whisper of Dawn (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Bardo{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Boon of Anubis (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Bardo{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Bring Forth the Dawn (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Bardo{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Pillar of Osiris (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Bardo{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Mummification (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Bardo{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Ra’s Blessing (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Bardo{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Celerity Supremacy (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Celerity{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Ignis Fatuus (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Chimerstry{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Fata Morgana (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Chimerstry{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Apparition (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Chimerstry{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Permanency (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Chimerstry{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Horrid Reality (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Chimerstry{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "False Resonance (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Chimerstry{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Fatuus Mastery (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Chimerstry{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shared Nightmare (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Chimerstry{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Far Fatuus (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Chimerstry{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Suspension of Disbelief (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Chimerstry{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Figment (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Chimerstry{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Through The Cracks (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Chimerstry{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Chimerstry Supremacy (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Chimerstry{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Sense The Sin (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Daimoinon{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Fear of the Void Below (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Daimoinon{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Conflagration (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Daimoinon{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Psychomachia (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Daimoinon{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Beastly Pact (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Daimoinon{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Concordance (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Daimoinon{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Herald of Topheth (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Daimoinon{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Contagion (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Daimoinon{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Call the Great Beast (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Daimoinon{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Passion (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Daimoinon{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Fracture (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Daimoinon{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Eyes of Chaos (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Daimoinon{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Voice of Insanity (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Daimoinon{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Total Madness (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Daimoinon{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Babble (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Daimoinon{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Sibyl’s Tongue (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Daimoinon{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Weaving the Tapestry (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Daimoinon{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shattered Mirror (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Daimoinon{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Speak To The Stars (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Daimoinon{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Father’s Blood (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Daimoinon{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Command (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Dominate{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Mesmerize (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Dominate{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Forgetful Mind (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Dominate{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Conditioning (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Dominate{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Obedience (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Dominate{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Possession (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Dominate{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Chain the Psyche (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Dominate{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Loyalty (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Dominate{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Mass Manipulation (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Dominate{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Still the Mortal Flesh (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Dominate{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Far Mastery (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Dominate{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Speak Through the Blood (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Dominate{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Dominate Supremacy (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Dominate{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Personal Armor (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Fortitude{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shared Strength (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Fortitude{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Fortitude Supremacy (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Fortitude{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Missing Voice (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Melpominee{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Phantom Speaker (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Melpominee{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Madrigal (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Melpominee{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Virtuosa (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Melpominee{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Siren’s Beckoning (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Melpominee{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Persistent Echo (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Melpominee{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shattering Crescendo (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Melpominee{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Riddle (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Mytherceria{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Fae Sight (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Mytherceria{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Oath of Iron (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Mytherceria{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Walk In Dreaming (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Mytherceria{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Fae Words (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Mytherceria{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Iron In The Mind (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Mytherceria{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Elysian Glade (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Mytherceria{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Geas (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Mytherceria{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Wyrd (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Mytherceria{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Sense Vitality (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obeah{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Anesthetic Touch (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obeah{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Corpore Sano (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obeah{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Mens Sana (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obeah{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Truce (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obeah{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Blood Of My Blood (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obeah{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Flesh Of My Flesh (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obeah{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Beating Heart (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obeah{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Safe Passage (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obeah{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Unburdening the Bestial Soul (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obeah{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Lifesense (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obeah{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Renewed Vigor (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obeah{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Life Through Death (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obeah{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Keeper Of The Flock (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obeah{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Obeah Supremacy (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obeah{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Cloak of Shadows (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obfuscate{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Unseen Presence (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obfuscate{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Mask of a Thousand Faces (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obfuscate{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Vanish from the Mind’s Eye (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obfuscate{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Cloak the Gathering (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obfuscate{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Conceal (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obfuscate{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Soul Mask (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obfuscate{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Cache (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obfuscate{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Veil of Blissful Ignorance (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obfuscate{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Old Friend (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obfuscate{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Avalonian Mist (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obfuscate{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Create Name (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obfuscate{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Obfuscate Supremacy (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obfuscate{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shadow Play (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obtenebration{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shroud of Night (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obtenebration{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Arms of the Abyss (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obtenebration{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Black Metamorphosis (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obtenebration{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Tenebrous Form (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obtenebration{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Tenebrous Mastery (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obtenebration{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Darkness Within (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obtenebration{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shadowstep (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obtenebration{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shadow Twin (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obtenebration{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Witness in Darkness (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obtenebration{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Oubliette (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obtenebration{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Ahriman’s Demesne (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obtenebration{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Keeper of the Shadowlands (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obtenebration{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Obtenebration Supremacy (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Obtenebration{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Consecrate the Grove (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Ogham{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Crimson Woad (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Ogham{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Inscribe the Curse (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Ogham{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Aspect of the Beast (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Ogham{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Moon and Sun (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Ogham{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Drink Dry the Earth (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Ogham{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Earthshock (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Potence{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Flick (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Potence{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Potence Supremacy (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Potence{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Awe (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Presence{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Dread Gaze (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Presence{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Entrancement (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Presence{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Summon (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Presence{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Majesty (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Presence{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Love (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Presence{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Paralyzing Glance (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Presence{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Spark of Rage (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Presence{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Cooperation (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Presence{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Ironclad Command (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Presence{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Pulse of the City (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Presence{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Presence Supremacy (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Presence{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Eyes of the Beast (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Protean{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Feral Claws (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Protean{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Earth Meld (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Protean{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Mist Form (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Protean{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shape of the Beast (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Protean{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Restore the Mortal Visage (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Protean{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Earth Control (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Protean{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Flesh of Marble (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Protean{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shape of the Beast’s Wrath (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Protean{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Spectral Body (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Protean{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Purify the Impaled Beast (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Protean{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Inward Focus (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Protean{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Protean Supremacy (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Protean{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Silence of Death (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Quietus{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Scorpion’s Touch (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Quietus{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Dagon’s Call (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Quietus{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Baal’s Caress (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Quietus{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Blood Burn (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Quietus{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Taste of Death (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Quietus{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Purification (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Quietus{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Ripples of the Heart (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Quietus{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Selective Silence (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Quietus{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Baal’s Bloody Talons (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Quietus{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Poison the Well of Life (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Quietus{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Songs of Distant Vitae (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Quietus{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Condemn the Sins of  the Father (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Quietus{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Quietus Supremacy (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Quietus{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Brother’s Blood (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Sanguinus{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Octopod (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Sanguinus{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Gestalt (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Sanguinus{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Walk of Caine (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Sanguinus{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Coagulated Entity (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Sanguinus{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Eyes of the Serpent (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Serpentis{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Tongue of the Asp (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Serpentis{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Skin of the Adder (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Serpentis{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Form of the Cobra (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Serpentis{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Heart of Darkness (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Serpentis{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Cobra Fangs (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Serpentis{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Divine Image (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Serpentis{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Heart Thief (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Serpentis{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shadow of Apep (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Serpentis{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Serpentis Supremacy (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Serpentis{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Hourglass of the Mind (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Temporis{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Recurring Contemplation (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Temporis{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Leaden Moment (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Temporis{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Patience of the Norns (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Temporis{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Clotho’s Gift (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Temporis{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Kiss of Lachesis (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Temporis{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "See Between Moments (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Temporis{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Clio’s Kiss (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Temporis{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Cheat the Fates (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Temporis{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Sense Infirmity (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Valeren{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Seek the Hated Foe (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Valeren{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Touch of Abaddon (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Valeren{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Armor of Caine’s Fury (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Valeren{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Sword of Michael (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Valeren{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Malleable Visage (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Vicissitude{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Fleshcraft (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Vicissitude{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Bonecraft (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Vicissitude{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Horrid Form (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Vicissitude{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Bloodform (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Vicissitude{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Chiropteran Marauder (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Vicissitude{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Cocoon (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Vicissitude{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Breath of the Dragon (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Vicissitude{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Earth’s Vast Haven (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Vicissitude{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Zahhak (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Vicissitude{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Vicissitude Supremacy (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Vicissitude{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Stoneskin (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Claws of Stone (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Scry the Hearthstone (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Humble As The Earth (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Reshape the Fortress (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Sand Form (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Flesh to Stone (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Golem (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Heightened Senses (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Aura Perception (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Spirit’s Touch (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Ever-Watchful Eye (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Telepathy (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Breach The Mind’s Sanctum (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Mask of a Thousand Faces (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Vanish from the Mind’s Eye (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Cloak the Gathering (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Eyes of the Beast (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Earth Meld (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Mist Form (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shape of the Beast (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Restore the Mortal Visage (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Mind to Mind (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Conceal (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Soul Mask (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Earth Control (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Flesh of Marble (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Through Another’s Eyes (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Cache (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Veil of Blissful Ignorance (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shape of the Beast’s Wrath (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Spectral Body (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Into Another’s Heart (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Old Friend (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Purify the Impaled Beast (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Eyes of the Grave (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "False Slumber (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Avalonian Mist (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Create Name (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Inward Focus (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Visceratika{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],


                //Assamite Sorcery
                [
                    traitID++,
                    "Confer with the Blade (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}A:Awakening of the Steel{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Grasp of the Mountain (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}A:Awakening of the Steel{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Pierce Steel’s Skin (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}A:Awakening of the Steel{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Razor’s Shield (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}A:Awakening of the Steel{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Spirit of Zulfiqar (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}A:Awakening of the Steel{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Decay (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}A:Hands of Destruction{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Gnarl Wood (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}A:Hands of Destruction{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Acidic Touch (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}A:Hands of Destruction{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Atrophy (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}A:Hands of Destruction{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Turn to Dust (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}A:Hands of Destruction{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "A Taste for Blood (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}A:The Path of Blood{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Blood Rage (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}A:The Path of Blood{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Blood of Potency (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}A:The Path of Blood{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Theft of Vitae (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}A:The Path of Blood{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Cauldron of Blood (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}A:The Path of Blood{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Summon the Simple Form (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}A:The Path of Conjuring{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Permanency (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}A:The Path of Conjuring{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Magic of the Smith (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}A:The Path of Conjuring{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Reverse Conjuration (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}A:The Path of Conjuring{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Power Over Life (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}A:The Path of Conjuring{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],


                //Koldunic Sorcery
                [
                    traitID++,
                    "Grasping Soil (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Way of Earth{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Endurance of Stone (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Way of Earth{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Hungry Earth (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Way of Earth{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Root of Vitality (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Way of Earth{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Kupala’s Fury (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Way of Earth{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Fiery Courage (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Way of Fire{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Combust (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Way of Fire{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Wall of Magma (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Way of Fire{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Heat Wave (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Way of Fire{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Volcanic Blast (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Way of Fire{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Pool of Lies (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Way of Water{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Watery Haven (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Way of Water{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Fog Over Sea (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Way of Water{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Minions of the Deep (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Way of Water{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Dessicate (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Way of Water{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Doom Tide (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Way of Water{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Breath of Whispers (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Way of Wind{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Biting Gale (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Way of Wind{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Breeze of Lethargy (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Way of Wind{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Ride the Tempest (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Way of Wind{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Tempest (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Way of Wind{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],


                //Necromancy
                [
                    traitID++,
                    "Shroudsight (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Ash Path{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Lifeless Tongues (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Ash Path{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Dead Hand (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Ash Path{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Ex Nihilo (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Ash Path{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shroud Mastery (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Ash Path{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Tremens (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Bone Path{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Apprentice’s Brooms (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Bone Path{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shambling Hordes (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Bone Path{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Soul Stealing (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Bone Path{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Daemonic Possession (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Bone Path{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "A Touch of Death (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Cenotaph Path{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Reveal the Catene (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Cenotaph Path{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Tread Upon the Grave (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Cenotaph Path{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Death Knell (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Cenotaph Path{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Ephemeral Binding (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Cenotaph Path{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Masque of Death (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Corpse in the Monster{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Cold of the Grave (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Corpse in the Monster{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Curse of Life (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Corpse in the Monster{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Gift of the Corpse (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Corpse in the Monster{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Gift of Life (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Corpse in the Monster{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Destroy the Husk (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Grave’s Decay{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Rigor Mortis (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Grave’s Decay{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Wither (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Grave’s Decay{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Corrupt the Undead Flesh (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Grave’s Decay{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Dissolve the Flesh (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Grave’s Decay{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Whispers to the Soul (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of the Four Humors{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Kiss of the Dark Mother (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of the Four Humors{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Dark Humors (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of the Four Humors{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Clutching the Shroud (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of the Four Humors{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Black Breath (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of the Four Humors{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Witness of Death (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Sepulchre Path{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Summon Soul (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Sepulchre Path{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Compel Soul (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Sepulchre Path{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Haunting (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Sepulchre Path{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Torment (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Sepulchre Path{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Eyes of the Dead (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Vitreous Path{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Aura of Decay (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Vitreous Path{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Soul Feast (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Vitreous Path{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Breath of Thanatos (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Vitreous Path{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Night Cry (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Vitreous Path{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],


                //Thaumaturgy
                [
                    traitID++,
                    "Elemental Strength (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Elemental Mastery{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Wooden Tongues (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Elemental Mastery{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Animate the Unmoving (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Elemental Mastery{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Elemental Form (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Elemental Mastery{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Summon Elemental (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Elemental Mastery{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Herbal Wisdom (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Green Path{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Speed the Season’s Passing (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Green Path{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Dance of Vines (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Green Path{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Verdant Haven (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Green Path{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Awaken the Forest Giants (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Green Path{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Decay (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Hands of Destruction{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Gnarl Wood (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Hands of Destruction{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Acidic Touch (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Hands of Destruction{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Atrophy (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Hands of Destruction{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Turn to Dust (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Hands of Destruction{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Eyes of the Sea (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Neptune’s Might{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Prison of Water (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Neptune’s Might{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Blood to Water (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Neptune’s Might{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Flowing Wall (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Neptune’s Might{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Dehydrate (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}Neptune’s Might{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "A Taste for Blood (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of Blood{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Blood Rage (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of Blood{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Blood of Potency (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of Blood{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Theft of Vitae (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of Blood{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Cauldron of Blood (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of Blood{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Summon the Simple Form (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of Conjuring{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Permanency (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of Conjuring{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Magic of the Smith (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of Conjuring{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Reverse Conjuration (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of Conjuring{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Power Over Life (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of Conjuring{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Contradict (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of Corruption{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Subvert (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of Corruption{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Dissociate (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of Corruption{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Addiction (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of Corruption{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Dependence (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of Corruption{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "War Cry (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of Mars{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Strike True (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of Mars{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Wind Dance (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of Mars{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Fearless Heart (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of Mars{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Comrades at Arms (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of Mars{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Analyze (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of Technomancy{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Burnout (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of Technomancy{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Encrypt/Decrypt (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of Technomancy{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Remote Access (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of Technomancy{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Telecommute (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of Technomancy{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Zillah’s Litany (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of the Father’s Vengeance{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Crone’s Pride (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of the Father’s Vengeance{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Feast of Ashes (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of the Father’s Vengeance{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Uriel’s Disfavor (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of the Father’s Vengeance{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Valediction (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}The Path of the Father’s Vengeance{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Turning (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}True Faith{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Scourging (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}True Faith{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Laying on Hands (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}True Faith{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Sanctification (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}True Faith{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Fear Not (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}{Utils.ChunkSplitter}{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}TRAITMAX{Utils.ChunkSplitter}{VtEKeywords.SubTrait}{Utils.MiniChunkSplitter}True Faith{Utils.ChunkSplitter}{VtEKeywords.PowerLevel}"
                ],

                //TODO: More Specific Powers (oh golly...)
                #endregion

                #region Backgrounds
                //TODO: Put all in, sort alphabetically so trait order matches
                [
                    traitID++,
                    "Allies",
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Background,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}BACKGROUNDMAX"
                ],
                [
                    traitID++,
                    "Alternate Identity",
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Background,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}BACKGROUNDMAX"
                ],
                [
                    traitID++,
                    "Contacts",
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Background,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}BACKGROUNDMAX"
                ],
                [
                    traitID++,
                    "Domain",
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Background,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}BACKGROUNDMAX"
                ],
                [
                    traitID++,
                    "Fame",
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Background,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}BACKGROUNDMAX"
                ],
                [
                    traitID++,
                    "Generation",
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Background,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}GENERATIONMAX"
                ],
                [
                    traitID++,
                    "Herd",
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Background,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}BACKGROUNDMAX"
                ],
                [
                    traitID++,
                    "Influence",
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Background,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}BACKGROUNDMAX"
                ],
                [
                    traitID++,
                    "Mentor",
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Background,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}BACKGROUNDMAX"
                ],
                [
                    traitID++,
                    "Resources",
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Background,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}BACKGROUNDMAX"
                ],
                [
                    traitID++,
                    "Retainers",
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Background,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}BACKGROUNDMAX"
                ],
                [
                    traitID++,
                    "Status",
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Background,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}BACKGROUNDMAX"
                ],
                #endregion

                [
                    traitID++,
                    "Path",
                    (int)TraitType.PathTrait,
                    (int)TraitCategory.MoralPath,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.MinMax}{Utils.MiniChunkSplitter}0{Utils.MiniChunkSplitter}PATHMAX"
                ],
                //TODO: Create a PathInfo table with the data we need to populate the other fields (Bearing etc) on the front end and handle logic on the back end

                #region Vital Statistics

                [
                    traitID++,
                    "Size",
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.VitalStatistic,
                    (int)TraitSubCategory.None,
                    ""
                ],
                [
                    traitID++,
                    "Speed",
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.VitalStatistic,
                    (int)TraitSubCategory.None,
                    ""
                ],
                [
                    traitID++,
                    "Run Speed",
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.VitalStatistic,
                    (int)TraitSubCategory.None,
                    ""
                ],
                [
                    traitID++,
                    "Health",
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.VitalStatistic,
                    (int)TraitSubCategory.None,
                    ""
                ],
                [
                    traitID++,
                    "Willpower",
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.VitalStatistic,
                    (int)TraitSubCategory.None,
                    ""
                ],
                [
                    traitID++,
                    "Defense",
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.VitalStatistic,
                    (int)TraitSubCategory.None,
                    ""
                ],
                [
                    traitID++,
                    "Initiative",
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.VitalStatistic,
                    (int)TraitSubCategory.None,
                    ""
                ],
                [
                    traitID++,
                    "Soak",
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.VitalStatistic,
                    (int)TraitSubCategory.None,
                    ""
                ],
                [
                    traitID++,
                    "Blood Pool",
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.VitalStatistic,
                    (int)TraitSubCategory.None,
                    ""
                ],

                #endregion

                //TODO: Other Traits, Merits and Flaws, Weapons, Physical Description

            ];
        }
    }
}
