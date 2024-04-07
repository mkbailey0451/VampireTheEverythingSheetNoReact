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
        public Character(int uniqueID, IEnumerable<TemplateKey>? templates = null, Dictionary<int,string> traitValues = null)
        {
            UniqueID = uniqueID;

            if (templates == null || !templates.Any())
            {
                templates = [TemplateKey.Mortal]; //TODO: Factor this out
            }

            foreach (TemplateKey template in templates)
            {
                AddTemplate(template);
            }

            if (traitValues != null)
            {
                foreach(int traitID in traitValues.Keys)
                {
                    if(_traits.TryGetValue(traitID, out Trait? trait))
                    {
                        trait.TryAssign(traitValues[traitID]);
                    }
                }
            }

            foreach (Trait trait in _traits.Values)
            {
                switch(trait.Name)
                {
                    case "Name":
                        _nameTraitID = trait.TraitID;
                        break;
                    case "Virtues":
                        _pathVirtuesTraitID = trait.TraitID;
                        break;
                    case "Path Score":
                        _pathScoreTraitID = trait.TraitID;
                        break;
                }
            }
        }

        public Character(int uniqueID, Character character) : this(uniqueID, character._templateKeys) { }

        public string GameTitle
        {
            get
            {
                if(_templateKeys.Count != 1)
                {
                    return "Vampire: The Everything";
                }

                return _templateKeys.First() switch
                {
                    TemplateKey.Mortal => "Vampire: The Everything",
                    TemplateKey.Kindred => "Vampire: The Masquerade",
                    TemplateKey.Kalebite => "Werewolf: The Hunt",
                    TemplateKey.Fae => "Changeling: The Journey",
                    TemplateKey.Mage => "Mage: The Illumination",
                    _ => "Vampire: The Everything",
                };
            }
        }

        public string Name
        {
            get
            {
                return GetTraitValue(_nameTraitID, "");
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
            _traits[traitID] = new Trait(this, TraitTemplate.AllTraitTemplates[traitID]);
        }

        public void RemoveTrait(int traitID)
        {
            if (!_traits.ContainsKey(traitID))
            {
                return;
            }

            //We have to remove the trait from the main list of traits, the variable registry, and the subtrait registry

            _traits.Remove(traitID);

            foreach (string key in _variables.Keys)
            {
                if (_variables[key] == traitID)
                {
                    _variables.Remove(key);
                }
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
                //TODO: Try to make this a regular prop again after we fix the weird problem
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

        public object? GetTraitValue(int? traitID)
        {
            if(traitID == null || !_traits.TryGetValue((int)traitID, out Trait? trait))
            {
                return null;
            }

            return trait.Value;
        }

        public T GetTraitValue<T>(int? traitID, T defaultValue)
        {
            if (GetTraitValue(traitID) is T t)
            {
                return t;
            }
            return defaultValue;
        }

        public bool TryGetTraitValue<T>(int? traitID, out T? value)
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

        private readonly int? _nameTraitID;
        private readonly int? _pathVirtuesTraitID;
        private readonly int? _pathScoreTraitID;

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
            "RESOLVEPENALTY",
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

        public int GetMaxSubTrait(IEnumerable<int> subTraitIDs)
        {
            return
            (
                from traitID in subTraitIDs
                select Utils.TryGetInt(_traits[traitID].Value) ?? int.MinValue
            ).Max();
        }

        public int CountSubTraits(IEnumerable<int> subtraitIDs)
        {
            int count = 0;

            //we only want to return the count of selected subtraits (or in other words, traits with a "truthy" kind of value)
            foreach (int traitID in subtraitIDs)
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
                _variables[variableName] = associatedTrait.TraitID;
            }
            else
            {
                throw new Exception("Tried to register an already existing variable: " + variableName);
            }
        }

        public int UniqueID { get; set; }

        //TODO: We may just want to expose Traits at this point.
        public Trait? GetTrait(int? traitID)
        {
            if (traitID == null || !_traits.TryGetValue((int)traitID, out Trait? trait))
            {
                return null;
            }

            return trait;
        }

        //TODO: Get rid of ever trying to reference traits purely by name

        /// <summary>
        /// Returns an IEnumerable containing all Traits belonging to the specified TraitCategory.
        /// </summary>
        public IEnumerable<Trait> GetTraits(TraitCategory category)
        {
            //There's two ways to do this - the LINQ way and the foreach-if way - and I'm not sure which I like better or which performs better.
            //I decided to mix it up for the demo project, as much to demonstrate that I can do both as anything else.
            //The LINQ way is definitely more compact, though.
            foreach (Trait trait in _traits.Values)
            {
                if (trait.Category == category)
                {
                    yield return trait;
                }
            }
        }

        /// <summary>
        /// Returns an IEnumerable containing all Traits belonging to the specified TraitCategory, and having the correct visibility.
        /// </summary>
        public IEnumerable<Trait> GetTraits(TraitCategory category, TraitVisibility visible)
        {
            foreach (Trait trait in _traits.Values)
            {
                if (trait.Category == category && trait.Visible == visible)
                {
                    yield return trait;
                }
            }
        }

        /// <summary>
        /// Returns an IEnumerable containing all Traits belonging to the specified TraitCategory and TraitSubCategory.
        /// </summary>
        public IEnumerable<Trait> GetTraits(TraitCategory category, TraitSubCategory sub)
        {
            //We can't memoize these, unless we want to take responsibility for updating the memoized data every time the templates change, which kind of defeats the point.
            return
                from trait in _traits.Values
                where trait.Category == category
                    && trait.SubCategory == sub //If I'm reading the docs correctly, the underlying SortedDictionary eliminates the need for orderby.
                select trait;
        }

        /// <summary>
        /// Returns an IEnumerable containing all Traits belonging to the specified TraitCategory and TraitSubCategory.
        /// </summary>
        public IEnumerable<Trait> GetTraits(TraitCategory category, TraitSubCategory sub, TraitVisibility visible)
        {
            return
                from trait in _traits.Values
                where trait.Category == category
                    && trait.SubCategory == sub
                    && trait.Visible == visible
                select trait;
        }

        private readonly SortedDictionary<int, Trait> _traits = [];

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
                case "RESOLVEPENALTY":
                    int penalty = 0;
                    string pathVirtues = GetTraitValue(_pathVirtuesTraitID, "").ToLower();
                    if (!pathVirtues.Contains("conscience"))
                    {
                        penalty--;
                    }
                    if (!pathVirtues.Contains("self-control"))
                    {
                        penalty--;
                    }
                    return penalty;
                case "EFFECTIVEHUMANITY":
                    int pathScore = GetTraitValue(_pathScoreTraitID, 0);
                    return GetReservedVariable("RESOLVEPENALTY") switch
                    {
                        0 => pathScore,
                        -1 => (int)Math.Ceiling(pathScore / 2.0),
                        _ => (object)(pathScore > 0 ? 1 : 0),
                    };
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
