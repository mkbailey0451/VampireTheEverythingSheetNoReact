namespace VampireTheEverythingSheetNoReact.Shared_Files
{
    public static class VtEConstants
    {
        /// <summary>
        /// This class contains a collection of database keywords used in the DATA field of the TRAITS table.
        /// These control the behavior of traits in various ways.
        /// </summary>
        public static class VtEKeywords
        {
            /// <summary>
            /// A trait with the MinMax keyword has a minimum and maximum numeric value.
            /// Such Traits should always have numeric values (such as by having a TraitType
            /// of IntegerTrait).
            /// </summary>
            public const string MinMax = "MINMAX";

            /// <summary>
            /// The AutoHide keyword indicates that, if the Trait has a value of zero or the empty
            /// string, it should not appear on the character sheet. This is used for Discipline scores
            /// and similar.
            /// </summary>
            public const string AutoHide = "AUTOHIDE";

            /// <summary>
            /// The PossibleValues keyword indicates that the Trait has a list of possible, generally
            /// non-numeric, values.
            /// </summary>
            public const string PossibleValues = "VALUES";

            /// <summary>
            /// The IsVar keyword indicates that this Trait is a variable used elsewhere in the character
            /// sheet. Such values are generally numeric.
            /// </summary>
            public const string IsVar = "IS_VARIABLE";

            /// <summary>
            /// The DerivedOption keyword indicates that one of the possible values of this Trait has a name
            /// which varies depending on some character-wide variable.
            /// </summary>
            public const string DerivedOption = "DERIVED_OPTION";

            /// <summary>
            /// The MainTraitMax keyword indicates that this trait takes the maximum value of any subtrait belonging
            /// to it as its value.
            /// </summary>
            public const string MainTraitMax = "MAINTRAIT_MAX";

            /// <summary>
            /// The MainTraitCount keyword indicates that this trait takes the count of selected subtraits belonging
            /// to it as its value.
            /// </summary>
            public const string MainTraitCount = "MAINTRAIT_COUNT";

            /// <summary>
            /// The SubTrait keyword indicates that a given trait is a subtrait. A subtrait is a component trait of a 
            /// main trait, which in some way derives its value from its component subtraits. A trait may be both a 
            /// subtrait and a main trait, though circular references will result in undefined behavior and must not
            /// be created.
            /// </summary>
            public const string SubTrait = "SUBTRAIT";

            /// <summary>
            /// The PowerLevel keyword indicates that a selectable subtrait is of a certain level, and thus that its main 
            /// trait must be greater than or equal to its level minus one to select it.
            /// </summary>
            public const string PowerLevel = "POWER_LEVEL"; //TODO: Implement business rules
        }

        /// <summary>
        /// This enum defines the different character templates that a character may have, which then define what traits
        /// the character has or may select.
        /// </summary>
        public enum TemplateKey
        {
            //This enum and others like it are given specific numbers to emulate the idea that these should be static and reflected in the database.
            Mortal = 0,
            Kindred = 1,
            Kalebite = 2,
            Fae = 3,
            Mage = 4,
        };

        /// <summary>
        /// This enum defines the types of different traits, which helps to control their appearance, behavior, and validation rules.
        /// </summary>
        public enum TraitType
        {
            /// <summary>
            /// A FreeTextTrait has a name and allows the user to associate any text with it.
            /// </summary>
            FreeTextTrait = 0,

            /// <summary>
            /// A DropdownTrait has a defined set of possible values and offers the user a dropdown list to select from these values.
            /// </summary>
            DropdownTrait = 1,

            /// <summary>
            /// An IntegerTrsit has a numeric value which the user can select by clicking on dots or through keyboard shortcuts.
            /// </summary>
            IntegerTrait = 2,

            /// <summary>
            /// A PathTrait reflects the character's moral Path, and allows the user to select both the Path and the rating thereof.
            /// The trait also displays certain information pertinent to the Path.
            /// </summary>
            PathTrait = 3, //TODO: Hierarchy of Sins?

            /// <summary>
            /// A WeaponTrait represents a weapon that the character can select to display in the Weapons section.
            /// The mere presence of one of these traits does not change the appearance of the UI except to add it to the
            /// dropdown menu for weapon selection.
            /// </summary>
            WeaponTrait = 4, //TODO: Conditional weapons like Arms of the Abyss

            /// <summary>
            /// A DerivedTrait represents a trait that is derived from other values, and therefore does not have a selectable
            /// or editable component.
            /// </summary>
            DerivedTrait = 5,

            MeritFlawTrait = 6, //TODO: Is this the same as SelectableTrait, or do we need SpecificPowerTrait, or what?
            SelectableTrait = 7, //TODO: Implement business rules
        };

        /// <summary>
        /// This enum defines the different categories of traits. These are used to determine where in the frontend page flow they should appear.
        /// </summary>
        public enum TraitCategory
        {
            /// <summary>
            /// A TopText trait is a field that goes at the top of the character sheet - Name, Chronicle, Clan, Breed, etc. Not all of these are actual
            /// text fields, but they belong to the "top text" of the character sheet.
            /// </summary>
            TopText = 0,

            Attribute = 1,
            Skill = 2,

            /// <summary>
            /// A Progression trait is used to measure a character's general power. Not all templates have one of these. Examples include Rage, Gnosis,
            /// Arete, and Wyrd.
            /// </summary>
            Progression = 3,

            /// <summary>
            /// Power traits represent the integer-value power traits, such as Auspex or Beastcraft. Specific powers of these traits, such as Aura Perception
            /// or Scent of Death, should be implemented as SpecificPower traits instead.
            /// </summary>
            Power = 4,

            /// <summary>
            /// SpecificPower traits represent specific powers belonging to Power traits - Power traits being Disciplines, Lores, and the like. Specific powers 
            /// thus include things like Aura Perception and Scent of Death.
            /// </summary>
            SpecificPower = 5,

            Background = 6,

            /// <summary>
            /// Vital Statistics are derived values describing the character's capabilities. These include Size, Speed, Health, Initiative, and so on.
            /// </summary>
            VitalStatistic = 7, //TODO: Break out Willpower into its own thing? Progression maybe? And add the "boxes below" dealie?

            MeritFlaw = 8,
            MoralPath = 9,
            Weapon = 10,

            /// <summary>
            /// Physical Description Bits are small aspects of the physical description of a character, such as their age, eye color, hair color, or fur color.
            /// </summary>
            PhysicalDescriptionBit = 11,

            /// <summary>
            /// Hidden traits do not appear on the UI at all.
            /// </summary>
            Hidden = 12, //TODO is this used?
        };

        /// <summary>
        /// The subcategory of a trait helps determine where in the page flow it should appear, as well as certain rules that may apply to it. Most of these are 
        /// self-explanatory as they are defined in the rules of the game.
        /// </summary>
        public enum TraitSubCategory
        {
            //TODO: trait ID may render this unnecessary for some areas
            None = -1,

            Physical = 0,
            Social = 1,
            Mental = 2,

            Faith = 3,
            Discipline = 4,
            Lore = 5,
            Tapestry = 6,
            Knit = 7,
            Arcanum = 8,

            /// <summary>
            /// Natural weapons are weapons that will automatically appear on the sheet when the character qualifies for them. They do not need to be selected.
            /// </summary>
            NaturalWeapon = 9, //TODO: Business rules on this - need some kind of keyword-condition thing

            /// <summary>
            /// Selectable weapons are weapons that may be selected to appear on the character sheet.
            /// </summary>
            SelectableWeapon = 10,
        };

        /// <summary>
        /// This enum describes the method by which a trait's value is determined.
        /// </summary>
        public enum TraitValueDerivation
        {
            /// <summary>
            /// The Standard derivation indicates that a Trait's value is simply stored and returned according to normal validation rules.
            /// </summary>
            Standard,

            /// <summary>
            /// The DerivedSum derivation indicates that a Trait's value is the sum of certain numbers and/or variables.
            /// </summary>
            DerivedSum,

            /// <summary>
            /// The DerivedOptions derivation indicates that certain values of the trait should be displayed to the user differently than they are 
            /// stored on the backend, according to certain internal rules. This only affects the display value, and not the actual value.
            /// </summary>
            DerivedOptions,

            /// <summary>
            /// The MainTraitMax derivation indicates that this is a main trait that takes the value of its highest subtrait. 
            /// See the Utils.Keywords.MainTraitMax, Utils.Keywords.MainTraitCount, and Utils.Keywords.SubTrait documentation for more details.
            /// </summary>
            MainTraitMax,

            /// <summary>
            /// The MainTraitCount derivation indicates that this is a main trait that takes the value of the count of its selected subtraits. 
            /// See the Utils.Keywords.MainTraitMax, Utils.Keywords.MainTraitCount, and Utils.Keywords.SubTrait documentation for more details.
            /// </summary>
            MainTraitCount
        }
    }
}
