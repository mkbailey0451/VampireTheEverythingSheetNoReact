using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Data;
using VampireTheEverythingSheetNoReact.Data_Access_Layer;
using VampireTheEverythingSheetNoReact.Shared_Files;
using static VampireTheEverythingSheetNoReact.Shared_Files.VtEConstants;

namespace VampireTheEverythingSheetNoReact.Models
{
    public class Character
    {
        public Character(string uniqueID, IEnumerable<TemplateKey>? templates = null)
        {
            UniqueID = uniqueID;

            if (templates == null || !templates.Any())
            {
                templates = [TemplateKey.Mortal];
            }

            foreach (TemplateKey template in templates)
            {
                AddTemplate(template);
            }
        }

        public Character(string uniqueID, Character character) : this(uniqueID, character._templateKeys) { }

        public string GameTitle
        {
            get
            {
                if(_templateKeys.Count != 1)
                {
                    return "Vampire: The Everything";
                }

                switch(_templateKeys.First())
                {
                    case TemplateKey.Mortal:
                        return "Vampire: The Everything";
                    case TemplateKey.Kindred:
                        return "Vampire: The Masquerade";
                    case TemplateKey.Kalebite:
                        return "Werewolf: The Hunt";
                    case TemplateKey.Fae:
                        return "Changeling: The Journey";
                    case TemplateKey.Mage:
                        return "Mage: The Illumination";
                }

                return "Vampire: The Everything";
            }
        }

        private readonly HashSet<TemplateKey> _templateKeys = [];
        public void AddTemplate(TemplateKey key)
        {
            if (_templateKeys.Contains(key))
            {
                return;
            }
            if (_templateKeys.Count > 0 && key == TemplateKey.Mortal)
            {
                return;
            }
            _templateKeys.Add(key);

            CharacterTemplate template = CharacterTemplate.AllCharacterTemplates[key] ?? throw new ArgumentException("Unrecognized template " + key + "in AddTemplate.");

            foreach (int traitID in template.TraitIDs)
            {
                AddTrait(traitID);
            }

            if (key != TemplateKey.Mortal)
            {
                _templateKeys.Remove(TemplateKey.Mortal);
            }
        }

        public void AddTrait(int traitID)
        {
            if (_traits.ContainsKey(traitID))
            {
                return;
            }
            _traits[traitID] = new Trait(this, TraitInfo.AllTraitInfo[traitID]);
            _readonlyTraitsByID = null;
            _readonlyTraitsByName = null;
        }

        public void RemoveTrait(int traitID)
        {
            if (!_traits.ContainsKey(traitID))
            {
                return;
            }

            //We have to remove the trait from the main list of traits, the variable registry, and the subtrait registry

            _traits.Remove(traitID);
            _readonlyTraitsByID = null;
            _readonlyTraitsByName = null;

            foreach (string key in _variables.Keys)
            {
                if (_variables[key] == traitID)
                {
                    _variables.Remove(key);
                }
            }

            foreach (HashSet<int> subTraits in _subTraitRegistry.Values)
            {
                subTraits.Remove(traitID);
            }

            //TODO: Is this everything?
        }

        public void RemoveTemplate(TemplateKey keyToRemove)
        {
            if (!_templateKeys.Contains(keyToRemove) || keyToRemove == TemplateKey.Mortal)
            {
                return;
            }
            _templateKeys.Remove(keyToRemove);

            //build set of traits to keep
            HashSet<int> keepTraits = [];

            foreach (TemplateKey templateKey in _templateKeys.Union([TemplateKey.Mortal]))
            {
                if (CharacterTemplate.AllCharacterTemplates.TryGetValue(templateKey, out CharacterTemplate? template))
                {
                    keepTraits.UnionWith(template.TraitIDs);
                }
            }

            //remove all others
            foreach (int traitID in _traits.Keys)
            {
                if (!keepTraits.Contains(traitID))
                {
                    RemoveTrait(traitID);
                }
            }
        }

        private static readonly ReadOnlyDictionary<string, List<int>> _traitIDsByName = FakeDatabase.GetDatabase().GetTraitIDsByName();


        public object? GetTraitValue(string? traitName)
        {
            if(traitName == null || !_traitIDsByName.TryGetValue(traitName, out List<int>? traitIDs))
            {
                return null;
            }

            return GetTraitValue(traitIDs[0]);
        }

        public object? GetTraitValue(int? traitID)
        {
            if(traitID == null || !_traits.TryGetValue((int)traitID, out Trait? trait))
            {
                return null;
            }

            return trait.Value;
        }

        public bool TryGetTraitValue<T>(string traitName, out T? value)
        {
            if (GetTraitValue(traitName) is T t)
            {
                value = t;
                return true;
            }
            value = default;
            return false;
        }

        public bool TryGetTraitValue<T>(int traitID, out T? value)
        {
            if (GetTraitValue(traitID) is T t)
            {
                value = t;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// A Dictionary mapping the names of variables to the unique IDs of Traits that represent these variables.
        /// This is done this way, rather than referencing the Trait directly, because it makes tracking their addition and removal easier.
        /// </summary>
        private readonly Dictionary<string, int> _variables = [];

        /// <summary>
        /// A set of reserved variables that have special behavior in the system, and therefore are hardcoded on the backend.
        /// We could create database-based rules for these, but the existing system is already close enough to the inner-platform effect
        /// without going that far.
        /// </summary>
        public readonly ImmutableHashSet<string> ReservedVariables =
        [
            "TRAITMAX",
            "MAGICMAX",
            "BACKGROUNDMAX",
            "PATHMAX",
            "GENERATIONMAX",
            "EFFECTIVEHUMANITY"
        ];

        /// <summary>
        /// If the supplied string is numeric, returns an int representation of it.
        /// If the supplied string is the name of a variable registered to the character, returns the current value of that variable.
        /// Otherwise, returns null.
        /// This method will automatically convert retrieved numeric strings to integers when retrieving them.
        /// </summary>
        public object? GetVariable(string? variableName)
        {
            if (string.IsNullOrEmpty(variableName))
            {
                return null;
            }

            if (ReservedVariables.Contains(variableName))
            {
                return GetReservedVariable(variableName);
            }

            //Since we do not accept numeric strings as variable names, we provide this automatic parsing functionality as a courtesy to calling methods.
            //We could wait for the Utils.TryGetInt call below to handle this case, but that would result in a lot of unnecessary parsing on the way.
            if (int.TryParse(variableName, out int result))
            {
                return result;
            }

            int multiplier = 1;

            //support for negative variables - useful for sums
            if (variableName.StartsWith('-'))
            {
                multiplier = -1;
                variableName = variableName[1..];
            }

            if (!_variables.TryGetValue(variableName, out int traitID))
            {
                return null;
            }

            //we handle the variable expansion to the greatest extent possible, but not all variables are of a straightforward type
            object val = _traits[traitID].Value;

            if (Utils.TryGetInt(val, out int intVal))
            {
                return intVal * multiplier;
            }

            return val;
        }

        public bool TryGetVariable<T>(string? variableName, out T? value)
        {
            if (GetVariable(variableName) is T t)
            {
                value = t;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// A mapping of subtrait names to the sets of unique trait IDs corresponding to those traits.
        /// Much like with _variables, we do this to make it easier to track the addition and removal of such Traits.
        /// </summary>
        private readonly Dictionary<string, HashSet<int>> _subTraitRegistry = [];

        public int GetMaxSubTrait(string mainTrait)
        {
            if (!_subTraitRegistry.TryGetValue(mainTrait, out var subTraits))
            {
                throw new ArgumentException("Unrecognized main trait " + mainTrait + " in CountSubTraits.");
            }

            return
            (
                from traitID in subTraits
                select Utils.TryGetInt(_traits[traitID].Value) ?? int.MinValue
            ).Max();
        }

        public int CountSubTraits(string mainTrait)
        {
            int count = 0;

            if (!_subTraitRegistry.TryGetValue(mainTrait, out HashSet<int>? subTraits))
            {
                throw new ArgumentException("Unrecognized main trait " + mainTrait + " in CountSubTraits.");
            }

            //we only want to return the count of selected subtraits (or in other words, traits with a "truthy" kind of value)
            foreach (int traitID in subTraits)
            {
                object val = _traits[traitID].Value;

                if (val is bool boolVal)
                {
                    if (boolVal)
                    {
                        count++;
                    }
                    continue;
                }
                if (Utils.TryGetInt(val, out int intVal))
                {
                    if (intVal > 0)
                    {
                        count++;
                    }
                    continue;
                }
                if (val is string strVal)
                {
                    if (!string.IsNullOrEmpty(strVal))
                    {
                        count++;
                    }
                    continue;
                }
                //TODO: I think Path will have some unique logic here, but we'll probably never use it

                throw new ArgumentException("Unrecognized datatype of val for trait " + _traits[traitID].Name + ": " + _traits[traitID].Value.GetType());
            }

            return count;
        }

        public void RegisterVariable(string variableName, Trait associatedTrait)
        {
            //we do not accept empty or numeric variable names
            if (variableName == "" || int.TryParse(variableName, out _) || ReservedVariables.Contains(variableName))
            {
                return;
            }
            if (!_variables.ContainsKey(variableName))
            {
                _variables[variableName] = associatedTrait.UniqueID;
            }
            else
            {
                throw new Exception("Tried to register an already existing variable: " + variableName);
            }
        }

        public void RegisterSubTrait(string mainTrait, Trait subTrait)
        {
            if (_subTraitRegistry.TryGetValue(mainTrait, out var subTraits))
            {
                subTraits.Add(subTrait.UniqueID);
                return;
            }
            _subTraitRegistry[mainTrait] = [subTrait.UniqueID];
        }

        public string UniqueID { get; set; }

        private readonly SortedDictionary<int, Trait> _traits = [];

        private ReadOnlyDictionary<int, Trait>? _readonlyTraitsByID = null;
        public ReadOnlyDictionary<int, Trait> TraitsByID
        {
            get
            {
                return _readonlyTraitsByID ??= new(_traits);
            }
        }

        private ReadOnlyDictionary<string, ReadOnlyCollection<Trait>>? _readonlyTraitsByName = null;
        public ReadOnlyDictionary<string, ReadOnlyCollection<Trait>> TraitsByName
        {
            get
            {
                if(_readonlyTraitsByName == null)
                {
                    //this is honestly probably excessive, but we *are* giving access to private data here
                    Dictionary<string,List<Trait>> buildDictionary = new(_traits.Count);
                    foreach(Trait trait in _traits.Values)
                    {
                        if(buildDictionary.TryGetValue(trait.Name, out List<Trait>? traitsByName))
                        {
                            traitsByName.Add(trait);
                        }
                        else
                        {
                            buildDictionary[trait.Name] = [trait];
                        }
                    }

                    Dictionary<string, ReadOnlyCollection<Trait>> buildReadOnlyLists = new(buildDictionary.Keys.Count);

                    foreach(string name in buildDictionary.Keys)
                    {
                        buildReadOnlyLists[name] = new(buildDictionary[name]);
                    }

                    _readonlyTraitsByName = new(buildReadOnlyLists);
                }

                return _readonlyTraitsByName;
            }
        }

        public IEnumerable<Trait> TopTextTraits
        {
            get
            {
                //There's two ways to do this - the LINQ way and the foreach-if way - and I'm not sure which I like better or which performs better.
                //I decided to mix it up for the demo project, as much to demonstrate that I can do both as anything else.
                foreach (Trait trait in _traits.Values)
                {
                    if (trait.Category == TraitCategory.TopText)
                    {
                        yield return trait;
                    }
                }
            }
        }

        public IEnumerable<Trait> PhysicalAttributes
        {
            get
            {
                //This is definitely more compact.
                return
                    from trait in _traits.Values
                    where trait.Category == TraitCategory.Attribute
                        && trait.SubCategory == TraitSubCategory.Physical //If I'm reading the docs correctly, the underlying SortedDictionary eliminates the need for orderby.
                    select trait;
            }
        }

        public IEnumerable<Trait> SocialAttributes
        {
            get
            {
                //But I *think* this has less runtime overhead.
                //Might not make a real difference though. We might be talking performance gains on the order of microseconds,
                //which would only matter in very high-performance apps.
                //(But, you know, in that situation, we should absolutely do this - or an even faster method. I went even farther than this in IslandSanctuarySolver, after all.)
                foreach (Trait trait in _traits.Values)
                {
                    if (trait.Category == TraitCategory.Attribute && trait.SubCategory == TraitSubCategory.Social)
                    {
                        yield return trait;
                    }
                }
            }
        }

        public IEnumerable<Trait> MentalAttributes
        {
            get
            {
                //Going with this for now. It's easier to edit.
                return
                    from trait in _traits.Values
                    where trait.Category == TraitCategory.Attribute
                        && trait.SubCategory == TraitSubCategory.Mental
                    select trait;
            }
        }

        public IEnumerable<Trait> PhysicalSkills
        {
            get
            {
                return
                    from trait in _traits.Values
                    where trait.Category == TraitCategory.Skill
                        && trait.SubCategory == TraitSubCategory.Physical
                    select trait;
            }
        }

        public IEnumerable<Trait> SocialSkills
        {
            get
            {
                return
                    from trait in _traits.Values
                    where trait.Category == TraitCategory.Skill
                        && trait.SubCategory == TraitSubCategory.Social
                    select trait;
            }
        }

        public IEnumerable<Trait> MentalSkills
        {
            get
            {
                return
                    from trait in _traits.Values
                    where trait.Category == TraitCategory.Skill
                        && trait.SubCategory == TraitSubCategory.Mental
                    select trait;
            }
        }

        public IEnumerable<Trait> Powers
        {
            get
            {
                return
                    from trait in _traits.Values
                    where trait.Category == TraitCategory.Power
                    select trait;
            }
        }

        private object? GetReservedVariable(string variableName)
        {
            switch (variableName)
            {
                /* TODO: All of this - we might actually end up with real frontend dropdowns that interact with some of this 
                 * (stuff like "Lowest Generation Diablerized", "Advanced Backgrounds Allowed", "Heart-Hunts Completed", or "Golconda Achieved")
                 * and some of these might have functions
                 */
                case "TRAITMAX":
                    return 5;
                case "MAGICMAX":
                    return 5;
                case "BACKGROUNDMAX":
                    return 5;
                case "PATHMAX":
                    return 10;
                case "GENERATIONMAX":
                    return 5;
                case "EFFECTIVEHUMANITY":
                    return 10;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
