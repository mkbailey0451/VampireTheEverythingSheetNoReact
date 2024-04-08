using System.Data;
using System.Diagnostics;
using VampireTheEverythingSheetNoReact.Shared_Files;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;
using static VampireTheEverythingSheetNoReact.Shared_Files.VtEConstants;

namespace VampireTheEverythingSheetNoReact.Models
{
    public class Trait
    {
        #region Public interface

        /// <summary>
        /// Creates a copy of the supplied Trait but belonging to the supplied Character.
        /// For interface reasons, this constructor does NOT copy the Value field.
        /// </summary>
        public Trait(Character character, Trait trait)
        {
            _character = character;
            Template = trait.Template;

            //this may get ignored or overridden by ProcessTraitData and that's okay
            RawValue = Template.DefaultValue;

            //these will get overwritten in ProcessTraitData if needed as well
            _minValue = -1;
            _maxValue = -1;

            //it's easier to reprocess this each time, since it does things like register variables on the Character
            ProcessTraitData(Template.Data);
        }

        /// <summary>
        /// Creates a Trait belonging to the supplied Character based on the supplied TraitTemplate.
        /// </summary>
        public Trait(Character character, TraitTemplate template)
        {
            //there are a lot of possible exceptions here, but the correct thing to do in this case is throw them anyway
            _character = character;
            Template = template;

            //this may get ignored or overridden by ProcessTraitData and that's okay
            RawValue = Template.DefaultValue;

            //these will get overwritten in ProcessTraitData if needed as well
            _minValue = -1;
            _maxValue = -1;

            ProcessTraitData(Template.Data);
        }

        /// <summary>
        /// The trait ID of this Trait. Each different Trait has a unique ID corresponding to its Trait Template.
        /// (Different Characters can and will have Traits with the same ID.)
        /// </summary>
        public int TraitID { get { return Template.UniqueID; } }

        /// <summary>
        /// The name of the Trait, such as Strength, Path, or Generation. There are NOT guaranteed to be unique!
        /// </summary>
        public string Name { get { return Template.Name; } }

        /// <summary>
        /// The type of Trait, which determines its validation rules and how it is rendered on the front end.
        /// (This could have been implemented as subclasses, but the amount of duplicated code across different subclasses was becoming unreasonable.)
        /// </summary>
        public TraitType Type { get { return Template.Type; } }

        /// <summary>
        /// The category of the Trait, which helps determine where on the page it will be rendered.
        /// </summary>
        public TraitCategory Category { get { return Template.Category; } }

        /// <summary>
        /// The subcategory of the Trait, which helps determine where on the page it will be rendered and what character templates can use it.
        /// </summary>
        public TraitSubCategory SubCategory { get { return Template.SubCategory; } }

        /// <summary>
        /// Determines if this trait should be rendered on the UI. (Many traits, such as powers and Backgrounds, are not rendered on the UI unless specifically selected.)
        /// </summary>
        public TraitVisibility Visible
        {
            get
            {
                switch (Category)
                {
                    case TraitCategory.Power:
                        return (ExpandedValue is int pwrVal && pwrVal > 0)
                            ? TraitVisibility.Visible
                            : TraitVisibility.Hidden;
                    case TraitCategory.SpecificPower:
                        if(ExpandedValue is bool specificVal && specificVal )
                        {
                            return TraitVisibility.Visible;
                        }
                        if(
                            PowerLevel == null || 
                            (
                                _character.TryGetTraitValue(MainTrait, out int mainTraitVal) && 
                                mainTraitVal >= PowerLevel - 1
                            )
                          )
                        {
                            return TraitVisibility.Selectable;
                        }
                        return TraitVisibility.Hidden;
                    case TraitCategory.Background:
                        return (ExpandedValue is int backVal && backVal > 0)
                            ? TraitVisibility.Visible
                            : TraitVisibility.Selectable;
                    case TraitCategory.MeritFlaw:
                    case TraitCategory.Weapon:
                        return (ExpandedValue is bool boolVal && boolVal)
                            ? TraitVisibility.Visible
                            : TraitVisibility.Selectable;
                    default: return TraitVisibility.Visible;
                }
            }
        }

        /// <summary>
        /// Returns the "display" value of the object, which is used for dropdowns with derived values and things like that. Contrasted with Value, 
        /// which retains a state that can be referenced sensibly on the backend.
        /// </summary>
        public string DisplayValue
        {
            get
            {
                //get the value of, well, Value as a string - in most cases, this is also the display value
                string rawTraitVal = Utils.TryGetString(ExpandedValue, "");

                //TODO: If there don't end up being multiple cases in this switch, refactor it to a compound if condition
                switch (_valDerivation)
                {
                    //but if there are derived options, we might display something different
                    case TraitValueDerivation.DerivedOptions:

                        //if this is, in fact, a derived option and not a normal one (because there can be a mix)...
                        if (DerivedOptionsLookup.ContainsKey(rawTraitVal))
                        {
                            //we pull out the Dictionary that governs the mapping of character variable values to display values for this trait
                            Dictionary<string, string> deriveSwitch = DerivedOptionsSwitches[rawTraitVal];
                            //as well as the character value in question
                            string variableVal = Utils.TryGetString(_character.GetVariable(rawTraitVal), "");
                            //and then we actually map it - this should theoretically always work, but we fall back on returning the raw trait value if it doesn't work
                            //TODO: We should probably log the error, though
                            if (deriveSwitch.TryGetValue(variableVal, out string? derivedValue))
                            {
                                return derivedValue;
                            }
                        }
                        break;
                }
                return rawTraitVal;
            }
        }

        /// <summary>
        /// The variable-expanded value of the object. Be warned that setting this attribute can fail silently due to validations. To avoid this behavior, use TryAssign instead.
        /// </summary>
        public object ExpandedValue
        {
            get
            {
                object unclippedVal;
                switch (_valDerivation)
                {
                    case TraitValueDerivation.Standard:
                    case TraitValueDerivation.DerivedOptions: //DerivedOptions only affects display values
                        unclippedVal = _character.GetVariable(RawValue);
                        break;
                    case TraitValueDerivation.DerivedSwitch:
                        Dictionary<string, string> derivedSwitch = DerivedOptionsSwitches["Value"];
                        string derivingVal = Utils.TryGetString(_character.GetVariable(RawValue), "");
                        if(derivedSwitch.TryGetValue(derivingVal, out string? derivedValue))
                        {
                            unclippedVal = derivedValue;
                        }
                        else
                        {
                            unclippedVal = derivingVal;
                        }
                        break;
                    case TraitValueDerivation.DerivedInteger:
                        unclippedVal = EvaluateDerived(RawValue);
                        break;
                    case TraitValueDerivation.MainTraitMax:
                        unclippedVal = _character.GetMaxSubTrait(SubTraits);
                        break;
                    case TraitValueDerivation.MainTraitCount:
                        unclippedVal = _character.CountSubTraits(SubTraits);
                        break;
                    default:
                        throw new NotImplementedException();
                }
                if(unclippedVal is int intVal)
                {
                    int min = MinValue,
                        max = MaxValue;

                    if(min != -1 && max != -1)
                    {
                        intVal = Math.Max(intVal, min);
                        intVal = Math.Min(intVal, max);
                        return intVal;
                    }
                }
                return unclippedVal;
            }
        }

        public string RawValue { get; private set; }
        private TraitValueDerivation _valDerivation = TraitValueDerivation.Standard;

        /// <summary>
        /// Attempts to safely assign the new value of the Trait, validating the input for type and other restrictions. Returns true if the assignment was successful.
        /// </summary>
        public bool TryAssign(object newValue)
        {
            //Purely-derived traits don't use their own value field like normal traits do,
            //instead deriving it from TRAIT_DATA
            switch(_valDerivation)
            {
                case TraitValueDerivation.DerivedInteger:
                case TraitValueDerivation.DerivedSwitch:
                case TraitValueDerivation.MainTraitCount:
                case TraitValueDerivation.MainTraitMax:
                    return false;
            }

            //ironically enough, we don't need to validate all IntegerTraits this way,
            //since such traits can have derived values - we only need to validate pure ints
            int? intValue = Utils.TryGetInt(newValue);

            if(intValue != null)
            {
                int min = MinValue,
                    max = MaxValue;

                if (min != -1 && max != -1)
                {
                    if (intValue < min || intValue > max)
                    {
                        return false;
                    }
                }
            }

            string? strValue = Utils.TryGetString(newValue);

            if (strValue == null)
            {
                return false;
            }

            if (_possibleValues.Length != 0 && !_possibleValues.Contains(strValue))
            {
                return false;
            }

            RawValue = strValue;

            return true;
        }

        /// <summary>
        /// Minimum numerical value of the Trait (if applicable).
        /// </summary>
        public int MinValue
        {
            get
            {
                //Sadly, we can't memoize the value because these variables can change over time
                _character.TryGetVariable(_minValue, out int val, -1);
                return val;
            }
        }
        object _minValue;

        /// <summary>
        /// Maximum numerical value of the Trait (if applicable).
        /// </summary>
        public int MaxValue
        {
            get
            {
                //Sadly, we can't memoize the value because these variables can change over time
                _character.TryGetVariable(_maxValue, out int val, -1);
                return val;
            }
        }
        object _maxValue;

        public int? PowerLevel { get; private set; } = null; //TODO: Not sure if this is the best implementation

        /// <summary>
        /// A list of all possible values of the Trait (for dropdown lists and the like).
        /// </summary>
        public IEnumerable<string> PossibleValues
        {
            get
            {
                //we don't want to return the object itself because it's mutable
                foreach (string value in _possibleValues)
                {
                    yield return value;
                }
            }
        }
        private string[] _possibleValues = [];

        public bool AutoHide { get; private set; }

        //TODO: Document ALL THE THINGS

        #endregion

        #region Private members

        /// <summary>
        /// The Character to whom this Trait belongs.
        /// </summary>
        public readonly Character _character;

        public int? MainTrait { get; private set; }

        public SortedSet<int> SubTraits { get { return Template.SubTraits; } }

        /// <summary>
        /// Every key in this Dictionary is the "dummy" name of an option (stored in PossibleValues) whose actual value changes based on the 
        /// value of some variable stored on the Character. The associated value is the name of the variable. (For example, the Breed of an animal-form
        /// Kalebite is internally listed as "[animal]" in the database. This is associated with the BROOD variable, because the name of that breed varies
        /// according to the Brood of the Kalebite.)
        /// </summary>
        private Dictionary<string, string> DerivedOptionsLookup { get; set; } = [];

        /// <summary>
        /// Every key in this Dictionary is the "dummy" name of an option (stored in PossibleValues) whose actual value changes based on the 
        /// value of some variable stored on the Character. The associated value is a Dictionary of values of that variable, mapped to the 
        /// correct value to transform that option to. (For example, the Breed of an animal-form Kalebite is internally listed as "[animal]" 
        /// in the database. This is associated with the BROOD variable, because the name of that breed varies according to the Brood of the Kalebite. 
        /// In this situation, the key "[animal]" would retrieve a Dictionary with keys of broods - Kalebite, Aragite, etc. - and values of their associated animal
        /// Breed names - Lycanth, Arachnes, etc.
        /// </summary>
        private Dictionary<string, Dictionary<string, string>> DerivedOptionsSwitches { get; set; } = [];

        private TraitTemplate Template { get; set; }

        /// <summary>
        /// Parses the TRAIT_DATA field on a given row into more friendly tokens.
        /// </summary>
        private static IEnumerable<string[]> TokenizeData(string traitData)
        {
            if (string.IsNullOrEmpty(traitData))
            {
                yield break;
            }

            foreach (string bigToken in traitData.Split(Utils.ChunkSplitter))
            {
                yield return bigToken.Split(Utils.MiniChunkSplitter);
            }
        }

        /// <summary>
        /// Parses the TRAIT_DATA field on a given row and sets up the Trait accordingly.
        /// </summary>
        private void ProcessTraitData(string traitData)
        {
            //TODO: Create TraitProps
            foreach (string[] tokens in TokenizeData(traitData))
            {
                switch (tokens[0])
                {
                    case VtEKeywords.MinMax:
                        _minValue = tokens[1];
                        _maxValue = tokens[2];
                        break;
                    case VtEKeywords.PossibleValues:
                        _possibleValues = tokens.Skip(1).ToArray(); //TODO: use
                        if(string.IsNullOrEmpty(RawValue) && _possibleValues.Length > 0)
                        {
                            RawValue = _possibleValues[0];
                        }
                        break;
                    case VtEKeywords.AutoHide:
                        AutoHide = true;
                        break;
                    case VtEKeywords.IsVar:
                        _character.RegisterVariable(tokens[1], this);
                        //TODO: Probably some flags for updating the Character from the Trait, vice versa, incrememting the Trait...
                        break;
                    case VtEKeywords.DerivedInteger:
                        _valDerivation = TraitValueDerivation.DerivedInteger;
                        RawValue = string.Join(Utils.MiniChunkSplitter,tokens.Skip(1));
                        break;
                    case VtEKeywords.DerivedOption:
                        _valDerivation = TraitValueDerivation.DerivedOptions;
                        DerivedOptionsLookup[tokens[1]] = tokens[2];
                        Dictionary<string, string> derivedOptions = [];
                        for (int x = 3; x < tokens.Length - 1; x += 2)
                        {
                            derivedOptions[tokens[x]] = tokens[x + 1];
                        }
                        DerivedOptionsSwitches[tokens[1]] = derivedOptions;
                        break;
                    case VtEKeywords.DerivedSwitch:
                        _valDerivation = TraitValueDerivation.DerivedSwitch;
                        RawValue = tokens[1];
                        Dictionary<string, string> derivedSwitch = [];
                        for (int x = 2; x < tokens.Length - 1; x += 2)
                        {
                            derivedSwitch[tokens[x]] = tokens[x + 1];
                        }
                        DerivedOptionsSwitches["Value"] = derivedSwitch;
                        break;
                    case VtEKeywords.MainTraitMax:
                        _valDerivation = TraitValueDerivation.MainTraitMax;
                        //Most (all?) main traits will want their own name as the main trait
                        //TODO: Do the same for variables?
                        RawValue = tokens.Length > 1
                            ? tokens[1]
                            : Name;
                        break;
                    case VtEKeywords.MainTraitCount:
                        _valDerivation = TraitValueDerivation.MainTraitCount;
                        RawValue = tokens.Length > 1
                            ? tokens[1]
                            : Name;
                        break;
                    case VtEKeywords.SubTrait:
                        MainTrait = int.Parse(tokens[1]);
                        break;
                    case VtEKeywords.PowerLevel:
                        PowerLevel = tokens.Length > 1
                            ? Utils.TryGetInt(tokens[1])
                            : (from nameChar in Name
                               where nameChar == '•'
                               select nameChar).Count();
                        break;
                }
            }
        }

        private int EvaluateDerived(string operandString)
        {
            string[] operands = operandString.Split(Utils.MiniChunkSplitter);

            if (operands.Length == 0)
            {
                throw new ArgumentNullException("Could not evaluate derived value " + operandString.ToString());
            }

            if(operands.Length == 1)
            {
                _character.TryGetVariable(operands[0], out int? result);
                if(result == null)
                {
                    throw new ArgumentNullException("Could not evaluate variable " + operands[0].ToString());
                }
                return (int)result;
            }

            //oh yeah, it's RPN time, I haven't done this since high school let's GO
            Stack<string> stack = new(operands);

            return (int)Math.Round(EvaluateStack(stack));
        }

        private double EvaluateStack(Stack<string> stack)
        {
            string val = stack.Pop().Trim();

            switch(val)
            {
                case "+": return EvaluateStack(stack) + EvaluateStack(stack);
                case "-": return EvaluateStack(stack) - EvaluateStack(stack);
                case "*": return EvaluateStack(stack) * EvaluateStack(stack);
                //TODO: These two might be in the wrong order - may have to store in variables and do from there - if we even ever use them
                case "/": return EvaluateStack(stack) / EvaluateStack(stack);
                case "^": return Math.Pow(EvaluateStack(stack),EvaluateStack(stack));
            }

            _character.TryGetVariable(val, out int? result);
            if (result == null)
            {
                throw new ArgumentNullException("Could not evaluate variable " + (val ?? "null").ToString());
            }
            return (double)result;
        }

        #endregion
    }
}
