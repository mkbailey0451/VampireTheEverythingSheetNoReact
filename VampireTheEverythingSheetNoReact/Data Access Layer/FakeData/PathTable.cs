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
                TableName = "MORAL_PATHS",
                Columns =
                {
                    new DataColumn("PATH_ID", typeof(int)),
                    new DataColumn("PATH_NAME", typeof(string)),
                    new DataColumn("VIRTUES", typeof(string)),
                    new DataColumn("BEARING", typeof(string)),
                    new DataColumn("HIERARCHY_OF_SINS", typeof(string)),
                }
            };

            int pathID = 0;
            #region Build path data
            object[][] rawPathData =
            [
                [
                    pathID++,
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
                    pathID++,
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
                    pathID++,
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
                    pathID++,
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
                    pathID++,
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
                    pathID++,
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
                    pathID++,
                    "The Path of Cathari",
                    "Conviction and Instinct",
                    "Seduction",
                    string.Join(Utils.ChunkSplitter,
                        "Exercising restraint",
                        "Showing trust",
                        "Failing to pass on the Curse to the passionately wicked or virtuous",
                        "Failing to ride the wave in frenzy",
                        "Refraining from indulgence",
                        "Sacrificing gratification for someone else’s convenience",
                        "Killing in Frenzy",
                        "Failing to encourage another to vice, outright murder",
                        "Casual murder",
                        "Encouraging others to exercise restraint, gleeful killing"
                    )
                ],
                [
                    pathID++,
                    "The Path of the Feral Heart",
                    "Conviction and Instinct",
                    "Menace",
                    string.Join(Utils.ChunkSplitter,
                        "Wearing clothing or using tools",
                        "Hunting with means other than your own vampiric powers",
                        "Engaging in politics",
                        "Feeding past satiation, acting in an overly cruel manner",
                        "Failing to ride the wave in a frenzy",
                        "Remaining in the presence of fire or sunlight, except to kill an enemy, failing to support one’s pack or allies (if any)",
                        "Killing in Frenzy (human or animal), failing to follow one’s instincts",
                        "Failing to hunt when hungry, outright murder, deliberate killing of an animal",
                        "Casual murder or slaughter",
                        "Gleeful murder or slaughter, refusing to hunt to survive"
                    )
                ],
                [
                    pathID++,
                    "The Path of the Fox",
                    "Conscience and Self-Control",
                    "Trustworthiness",
                    string.Join(Utils.ChunkSplitter,
                        "Selfish acts, except as payment for services rendered",
                        "Accidental injury to another",
                        "Refusing an opportunity to enjoy oneself, except for a greater good",
                        "Theft from the innocent, except as necessary to teach a lesson",
                        "Frenzying, accidental violation",
                        "Failing to take an opportunity to teach through trickery, intentional property damage",
                        "Killing in a Frenzy, failing to aid an ally",
                        "Outright murder, aiding an oppressor",
                        "Casual murder",
                        "Tricking someone with no benefit to others, gleeful murder"
                    )
                ],
                [
                    pathID++,
                    "The Path of Harmony",
                    "Conscience and Instinct",
                    "Harmony",
                    string.Join(Utils.ChunkSplitter,
                        "Acting in a selfish manner except when necessary to survive or aid one’s allies",
                        "Failing to develop one’s vampiric abilities, refusing to aid the innocent when they are in danger",
                        "Failing to learn about nature",
                        "Theft or trespassing",
                        "Failing to ride the wave in a frenzy",
                        "Remaining in the presence of fire or sunlight, except to kill an enemy or aid an ally, failing to follow one’s instincts",
                        "Killing in Frenzy (human or animal), failing to aid one’s pack or allies, feeding past satiation, deliberate and unnecessary cruelty",
                        "Failing to hunt when hungry, outright murder, deliberate killing of an animal",
                        "Casual murder or slaughter",
                        "Gleeful murder or slaughter, refusing to hunt to survive"
                    )
                ],
                [
                    pathID++,
                    "The Path of Honorable Accord",
                    "Conviction and Self-Control",
                    "Devotion",
                    string.Join(Utils.ChunkSplitter,
                        "Failing to uphold all the precepts of your group",
                        "Failing to show hospitality to your allies",
                        "Associating with the dishonorable",
                        "Failing to participate in your group’s rituals",
                        "Disobeying your leader",
                        "Failing to protect your allies",
                        "Placing personal concerns over duty, killing in a frenzy",
                        "Showing cowardice, outright murder",
                        "Casual killing",
                        "Breaking your word or oath; failing to honor an agreement; gleeful killing"
                    )
                ],
                [
                    pathID++,
                    "The Path of Indulgence",
                    "Conviction and Instinct",
                    "Need",
                    string.Join(Utils.ChunkSplitter,
                        "Showing concern for others, except in anticipation of their later usefulness",
                        "Failing to ride the wave in a frenzy",
                        "Refusing to explore new and interesting pleasures",
                        "Refusing to ghoul another when the opportunity arises",
                        "Refusing to attain wealth, fame, or power, except in the pursuit of pleasure or survival",
                        "Inconveniencing oneself for another without a guarantee of reward",
                        "Killing in a frenzy, refusing to exploit another for personal pleasure",
                        "Inconveniencing oneself for another without anticipation of reward, outright murder",
                        "Refusing to indulge in any pleasure that does not threaten one’s survival, casual killing",
                        "Risking one’s existence, gleeful killing"
                    )
                ],
                [
                    pathID++,
                    "The Path of Lilith",
                    "Conviction and Instinct",
                    "Tribulation",
                    string.Join(Utils.ChunkSplitter,
                        "Feeding immediately when hungry",
                        "Pursuing temporal wealth or power",
                        "Not correcting the errors of others regarding Caine and Lilith",
                        "Feeling remorse for bringing pain to someone",
                        "Failing to participate in a Bahari ritual",
                        "Fearing death",
                        "Killing in a frenzy",
                        "Outright murder, not seeking out the teachings of Lilith",
                        "Failing to dispense nonlethal pain and anguish, casual murder",
                        "Shunning nonlethal pain, gleeful killing"
                    )
                ],
                [
                    pathID++,
                    "The Path of Metamorphosis",
                    "Conviction and Instinct",
                    "Inhumanity",
                    string.Join(Utils.ChunkSplitter,
                        "Engaging in politics or any social activity, postponing feeding when hungry",
                        "Indulging in pleasure",
                        "Asking another for knowledge",
                        "Sharing knowledge with another",
                        "Failure to experiment on others when something may be gained from it",
                        "Failing to ride out a frenzy",
                        "Considering the needs of others, killing in a Frenzy",
                        "Failure to experiment, even at risk to oneself, outright murder",
                        "Neglecting to alter one’s own body, casual killing",
                        "Exhibiting compassion for others, gleeful killing"
                    )
                ],
                [
                    pathID++,
                    "The Path of Night",
                    "Conviction and Instinct",
                    "Darkness",
                    string.Join(Utils.ChunkSplitter,
                        "Engaging in pleasures, except at the expense of others",
                        "Engaging in politics",
                        "Failing to be innovative in one’s depredations",
                        "Asking aid of another",
                        "Failing to ride out a Frenzy, showing compassion or sorrow",
                        "Bowing to another Kindred’s will, failing to harm a mortal when the opportunity arises",
                        "Killing in Frenzy, accidental killing",
                        "Aiding or acting in the interests of another, outright murder",
                        "Accepting another’s claim to superiority, casual killing",
                        "Repenting one’s behavior, gleeful killing"
                    )
                ],
                [
                    pathID++,
                    "The Path of Power and the Inner Voice",
                    "Conviction and Instinct",
                    "Command",
                    string.Join(Utils.ChunkSplitter,
                        "Denying responsibility for your actions",
                        "Treating your underlings poorly when they have been successful",
                        "Failing to respect your superiors",
                        "Helping others when it is not to your advantage",
                        "Accepting defeat",
                        "Failing to further one’s own interests",
                        "Submitting to the error of others, killing in Frenzy",
                        "Not using the most effective tools for control, outright murder",
                        "Not punishing failure, casual killing",
                        "Turning down the opportunity for power, gleeful killing"
                    )
                ],
                [
                    pathID++,
                    "The Rising Path",
                    "Conscience and Self-Control",
                    "Mentorship",
                    string.Join(Utils.ChunkSplitter,
                        "Selfish acts",
                        "Injury to another, refusing to improve someone who asks",
                        "Intentional injury to another, refusing to offer useful advice",
                        "Theft, refusing the advice of a fellow Sculptor",
                        "Accidental violation, refusing hospitality to a fellow Sculptor",
                        "Experimenting recklessly on others",
                        "Killing in a Frenzy, altering another without permission",
                        "Outright murder, deliberately harming another with Vicissitude",
                        "Casual violation, killing a fellow Sculptor",
                        "Gleeful or creative violation, refusing to improve oneself"
                    )
                ],
                [
                    pathID++,
                    "Via Angeli (The Road of the Angel)",
                    "Conscience and Self-Control",
                    "Angelic",
                    string.Join(Utils.ChunkSplitter,
                        "Acting on any sinful impulse.",
                        "Frenzying, engaging in the pleasures of the flesh beyond the needs of survival.",
                        "Using one’s time unwisely.",
                        "Theft or acting out of covetousness.",
                        "Killing innocents during a frenzy, directing the actions of another.",
                        "Refusing to experiment on oneself when doing so may result in improvements.",
                        "Feeding from the innocent without permission, harming the innocent, having or faking sexual intercourse.",
                        "The murder of innocents, the permanent destruction of useful knowlege.",
                        "Worshipping false idols or other gods, lying, blasphemy, or heresy; allowing others to believe that one is an angel (or a supernatural being other than a vampire).",
                        "Denying the Resurrection of Jesus Christ or the doctrine of salvation; accepting worship in any form."
                    )
                ],
                [
                    pathID++,
                    "Via Caeli (The Road of Heaven)",
                    "Conscience and Self-Control",
                    "Holiness",
                    string.Join(Utils.ChunkSplitter,
                        "Acting on any sinful impulse",
                        "Leading a brother to stumble, frenzying, refusing to forgive a sin against you",
                        "Bearing false witness",
                        "Theft",
                        "Adultery, killing innocents during a frenzy",
                        "Blasphemy or heresy in word or deed",
                        "Feeding from the innocent without permission, harming the innocent, rape",
                        "The murder of innocents",
                        "Worshiping false idols or other gods",
                        "Denying the Resurrection of Jesus Christ or the doctrine of salvation"
                    )
                ],
                [
                    pathID++,
                    "Via Vir-Fortis (The Road of the Avenger)",
                    "Conscience and Self-Control",
                    "Heroism",
                    string.Join(Utils.ChunkSplitter,
                        "Refusing to search nightly for villainy",
                        "Refusing to aid an ally",
                        "Allowing evil to occur when one can prevent it",
                        "Stealing from the innocent",
                        "Frenzying, allowing an ally to come to harm",
                        "Aiding a known killer",
                        "Killing in a frenzy, allowing a murder when one can prevent it",
                        "Outright murder, betraying an ally",
                        "Casual killing",
                        "Gleeful killing"
                    )
                ]
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
