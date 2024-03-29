using System.Data;
using VampireTheEverythingSheetNoReact.Shared_Files;

namespace VampireTheEverythingSheetNoReact.Data_Access_Layer.FakeData
{
    public static class PathTable
    {
        public static DataTable Data { get; private set; }

        static PathTable()
        {
            Data = BuildData();
        }

        private static DataTable BuildData()
        {
            DataTable pathData = new()
            {
                Columns =
                {
                    new DataColumn("PATH_NAME", typeof(string)),
                    new DataColumn("VIRTUES", typeof(string)),
                    new DataColumn("BEARING", typeof(string)),
                    new DataColumn("HIERARCHY_OF_SINS", typeof(string)),
                }
            };

            #region Build path data
            object[][] rawPathData =
            [
                [
                    "Humanity",
                    "Conscience and Self-Control",
                    "Humanity",
                    string.Join(Utils.ChunkSplitter,
                        "Selfish acts.",
                        "Injury to another (in Frenzy or otherwise, except in self-defense, etc).",
                        "Intentional injury to another (except self-defense, consensual, etc).",
                        "Theft.",
                        "Accidental violation (drinking a vessel dry out of starvation).",
                        "Intentional property damage.",
                        "Impassioned violation (manslaughter, killing a vessel in Frenzy).",
                        "Planned violation (outright murder, savored exsanguination).",
                        "Casual violation (thoughtless killing, feeding past satiation).",
                        "Gleeful or “creative” violation (let’s not go there)."
                    )
                ],
                [
                    "Assamia",
                    "Conviction and Self-Control",
                    "Resolve",
                    string.Join(Utils.ChunkSplitter,
                        "Feeding on a mortal without consent",
                        "Breaking a word of honor to a Clanmate",
                        "Refusing to offer a non-Assamite an opportunity to convert",
                        "Failing to take an opportunity to destroy an apostate from the Clan",
                        "Succumbing to frenzy",
                        "Wronging a mortal (such as by injury or theft), except by feeding",
                        "Killing a mortal in Frenzy, failing to take an opportunity to harm a wicked Kindred",
                        "Refusal to further the cause of Assamia, even when doing so is safe",
                        "Outright murder of a mortal",
                        "Acting against another Assamite, casual murder"
                    )
                ],
                [
                    "The Ophidian Path",
                    "Conviction and Self-Control",
                    "Devotion",
                    string.Join(Utils.ChunkSplitter,
                        "Pursuing one’s own indulgences instead of another’s",
                        "Refusing to aid another follower of the Path",
                        "Aiding a vampire in Golconda or anyone with True Faith",
                        "Failing to observe Apophidian religious ritual",
                        "Failing to undermine the current social order in favor of the Apophidians",
                        "Failing to do whatever is necessary to corrupt another",
                        "Failing to pursue arcane knowledge",
                        "Obstructing another Apophidian’s efforts, outright murder",
                        "Failing to take advantage of another’s weakness, casual killing",
                        "Refusing to aid in Set’s resurrection, gleeful killing"
                    )
                ],
                [
                    "The Path of the Archivist",
                    "Conviction and Self-Control",
                    "Sagacity",
                    string.Join(Utils.ChunkSplitter,
                        "Refusing to share knowledge with another",
                        "Refusing to pursue existing knowledge, going hungry",
                        "Refusing to research and expand the horizons of knowledge",
                        "Refusing to maintain a storehouse of knowledge",
                        "Acting with negligence in a library or other storehouse of knowledge",
                        "Burning a book (or destroying any other store of knowledge) or Frenzying for any reason other than to research something",
                        "Killing in a Frenzy, killing a knowledgeable person",
                        "Outright murder, killing a scholar or scientist",
                        "Casual violation, killing a fellow Archivist",
                        "Gleeful or creative violation, allowing knowledge to be permanently destroyed"
                    )
                ],
                [
                    "The Path of Bones",
                    "Conviction and Self-Control",
                    "Silence",
                    string.Join(Utils.ChunkSplitter,
                        "Showing a fear of death",
                        "Failing to study an occurrence of death",
                        "Causing the suffering of another for no personal gain",
                        "Postponing feeding when hungry",
                        "Succumbing to frenzy",
                        "Showing concern for another’s well-being",
                        "Accidental killing (such as in Frenzy), making a decision based on emotion rather than logic",
                        "Outright murder, inconveniencing oneself for another’s benefit",
                        "Casual murder, preventing a death for personal gain",
                        "Gleeful or creative violation, preventing a death for no personal gain"
                    )
                ],
                [
                    "The Path of Caine",
                    "Conviction and Instinct",
                    "Faith",
                    string.Join(Utils.ChunkSplitter,
                        "Failing to engage in research or study each night, regardless of circumstances",
                        "Failing to instruct other vampires in the Path of Caine",
                        "Befriending or co-existing with mortals",
                        "Showing disrespect to other students of Caine",
                        "Failing to ride the wave in frenzy",
                        "Succumbing to Rötschreck",
                        "Aiding a “humane” vampire, killing in a Frenzy",
                        "Failing to regularly test the limits of abilities and Disciplines, outright murder",
                        "Failing to pursue lore about vampirism when the opportunity arises, casual murder",
                        "Denying vampiric needs (by refusing to feed, showing compassion, or failing to learn about one’s vampiric abilities), gleeful or creative violation"
                    )
                ],
                [//TODO
                    "Humanity",
                    "Conscience and Self-Control",
                    "Humane",
                    string.Join(Utils.ChunkSplitter,
                        "",
                        "",
                        "",
                        "",
                        "",
                        "",
                        "",
                        "",
                        "",
                        ""
                    )
                ],
                [
                    "Humanity",
                    "Conscience and Self-Control",
                    "Humane",
                    string.Join(Utils.ChunkSplitter,
                        "",
                        "",
                        "",
                        "",
                        "",
                        "",
                        "",
                        "",
                        "",
                        ""
                    )
                ],
            ];
            #endregion

            foreach (object[] row in rawPathData)
            {
                pathData.Rows.Add(row);
            }

            return pathData;
        }
    }
}
