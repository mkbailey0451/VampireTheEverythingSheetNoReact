﻿using System.Collections.ObjectModel;
using System.Data;
using VampireTheEverythingSheetNoReact.Models;
using static VampireTheEverythingSheetNoReact.Shared_Files.VtEConstants;

namespace VampireTheEverythingSheetNoReact.Data_Access_Layer
{
    //TODO: Replace with a real database using LocalDB
    /// <summary>
    /// Normally, an application of this kind would interface to a SQL backend via a class much like this one.
    /// However, this project is designed as a demonstration of the author's ability to develop code in React, and
    /// to be able to run on a local machine with no connection to a database for demo purposes. As such, this
    /// fake database layer with hardcoded values is provided. An interface has been created to allow for its easy replacement
    /// in the case that such is desirable.
    /// </summary>
    public class FakeDatabase : IDatabaseAccessLayer
    {
        #region Public members

        /// <summary>
        /// Returns the singleton instance of this database.
        /// </summary>
        /// <returns></returns>
        public static IDatabaseAccessLayer GetDatabase()
        {
            return _db;
        }

        public DataTable GetTraitData()
        {
            //Technically, we should do a deep copy here (and elsewhere in this class) to avoid mutability concerns, but this *is* a fake database anyway
            return _traits;
        }

        public DataTable GetCharacterTemplateData()
        {
            //TODO: Refactor because we're doing the trait template thing now
            //TODO: Revise this to reflect the DATA field below
            /*
             * This would normally be the result of a JOIN between the following tables:
             * TEMPLATES - would contain primary keys matching the Constants.TemplateKey enum, the name of the template, and probably nothing else
             * TEMPLATE_X_TRAIT - crosswalk table matching templates to their owned traits (primary key to primary key via foreign keys, with the TEMPLATE_X_TRAIT table having a PK of both keys)
             * TRAITS - table listing the ID, name, type (integer, free text, etc as enum values), category (top text, attribute, specific power, etc as enum values), subcategory (if applicable - 
             * things like physical/mental/social or Discipline/Lore/Tapestry - also as enum values), and data (whose meaning varies according to the other variables) of the trait.
             * 
             * The tables in this class are mainly meant to simulate that functionality.
             * */
            //TODO: Note archetypes and such above - probably from a function or stored proc
            DataTable output = new()
            {
                Columns =
                {
                    new DataColumn("TEMPLATE_ID", typeof(int)),
                    new DataColumn("TEMPLATE_NAME", typeof(string)),
                    new DataColumn("TRAIT_ID", typeof(int)),
                }
            };

            var rows =
                from templateRow in _templates.Rows.Cast<DataRow>()
                join templateXTraitRow in _template_x_trait.Rows.Cast<DataRow>()
                    on templateRow["TEMPLATE_ID"] equals templateXTraitRow["TEMPLATE_ID"]
                orderby templateRow["TEMPLATE_ID"], templateXTraitRow["TRAIT_ID"]
                select new object[]
                {
                    templateRow["TEMPLATE_ID"],
                    templateRow["TEMPLATE_NAME"],
                    templateXTraitRow["TRAIT_ID"],
                };

            foreach (object[] row in rows)
            {
                output.Rows.Add(row);
            }

            return output;
        }

        /// <summary>
        /// Returns a DataTable representing the moral Paths a character may follow.
        /// </summary>
        public DataTable GetPathData()
        {
            return _paths;
        }

        public ReadOnlyDictionary<string, List<int>> GetTraitIDsByName()
        {
            return _traitIDsByName;
        }

        #endregion

        #region Private members

        /// <summary>
        /// The singleton instance of this database.
        /// </summary>
        private static readonly FakeDatabase _db = new();

        /// <summary>
        /// Since this database is a singleton, we naturally want its constructor to be private.
        /// </summary>
        private FakeDatabase() { }

        /// <summary>
        /// A DataTable emulating a table of templates.
        /// </summary>
        private static readonly DataTable _templates = BuildTemplateTable();
        private static DataTable BuildTemplateTable()
        {
            DataTable templates = new()
            {
                Columns =
                {
                    new DataColumn("TEMPLATE_ID", typeof(int)),
                    new DataColumn("TEMPLATE_NAME", typeof(string)),
                }
            };

            foreach (TemplateKey key in Enum.GetValues(typeof(TemplateKey)))
            {
                templates.Rows.Add(new object[]
                {
                    (int)key,
                    key.ToString()
                });
            }

            return templates;
        }

        /// <summary>
        /// A DataTable emulating a table of traits.
        /// </summary>
        private static readonly DataTable _traits = BuildTraitTable();
        private static DataTable BuildTraitTable()
        {
            DataTable traits = new()
            {
                Columns =
                {
                    new DataColumn("TRAIT_ID", typeof(int)),
                    new DataColumn("TRAIT_NAME", typeof(string)),
                    new DataColumn("TRAIT_TYPE", typeof(string)),
                    new DataColumn("TRAIT_CATEGORY", typeof(int)),
                    new DataColumn("TRAIT_SUBCATEGORY", typeof(int)),
                    new DataColumn("TRAIT_DATA", typeof(string)),
                }
            };

            #region Build traits
            int traitID = 0;
            //this is not exactly the best way to do this, but again, this is a fake database and not really how we'd do any of this anyway
            object?[][] rawTraits =
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
                    $"{VtEKeywords.PossibleValues}|" + string.Join("|", GetAllArchetypes())
                ],
                [
                    traitID++,
                    "Demeanor", //name
                    (int)TraitType.DropdownTrait,
                    (int)TraitCategory.TopText,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.PossibleValues}|" + string.Join("|", GetAllArchetypes())
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
                    $"{VtEKeywords.PossibleValues}|" + string.Join("|", GetAllClans())
                ],
                [
                    traitID++,
                    "Generation", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.TopText,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.IsVar}|GENERATION"
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
                    $"{VtEKeywords.IsVar}|BROOD\n{VtEKeywords.PossibleValues}|" + string.Join("|", GetAllBroods())
                ],
                [
                    traitID++,
                    "Breed", //name
                    (int)TraitType.DropdownTrait,
                    (int)TraitCategory.TopText,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.DerivedOption}|[animal]|BROOD|" + string.Join("|", GetBroodBreedSwitch()) + "\n{VtEKeywords.PossibleValues}|" + string.Join("|", GetAllBreeds())
                ],
                [
                    traitID++,
                    "Tribe", //name
                    (int)TraitType.DropdownTrait,
                    (int)TraitCategory.TopText,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.PossibleValues}|" + string.Join("|", GetAllTribes())
                ],
                [
                    traitID++,
                    "Auspice", //name
                    (int)TraitType.DropdownTrait,
                    (int)TraitCategory.TopText,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.PossibleValues}|" + string.Join("|", GetAllAuspices())
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
                    $"{VtEKeywords.MinMax}|1|TRAITMAX" //min, max
                ],
                [
                    traitID++,
                    "Dexterity", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Attribute,
                    (int)TraitSubCategory.Physical,
                    $"{VtEKeywords.MinMax}|1|TRAITMAX"
                ],
                [
                    traitID++,
                    "Stamina", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Attribute,
                    (int)TraitSubCategory.Physical,
                    $"{VtEKeywords.MinMax}|1|TRAITMAX"
                ],

                [
                    traitID++,
                    "Charisma", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Attribute,
                    (int)TraitSubCategory.Social,
                    $"{VtEKeywords.MinMax}|1|TRAITMAX"
                ],
                [
                    traitID++,
                    "Manipulation", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Attribute,
                    (int)TraitSubCategory.Social,
                    $"{VtEKeywords.MinMax}|1|TRAITMAX"
                ],
                [
                    traitID++,
                    "Composure", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Attribute,
                    (int)TraitSubCategory.Social,
                    $"{VtEKeywords.MinMax}|1|TRAITMAX"
                ],

                [
                    traitID++,
                    "Intelligence", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Attribute,
                    (int)TraitSubCategory.Mental,
                    $"{VtEKeywords.MinMax}|1|TRAITMAX"
                ],
                [
                    traitID++,
                    "Wits", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Attribute,
                    (int)TraitSubCategory.Mental,
                    $"{VtEKeywords.MinMax}|1|TRAITMAX"
                ],
                [
                    traitID++,
                    "Resolve", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Attribute,
                    (int)TraitSubCategory.Mental,
                    $"{VtEKeywords.MinMax}|1|TRAITMAX"
                ],
                #endregion

                #region Skills
                [
                    traitID++,
                    "Athletics", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Physical,
                    $"{VtEKeywords.MinMax}|0|TRAITMAX"
                ],
                [
                    traitID++,
                    "Brawl", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Physical,
                    $"{VtEKeywords.MinMax}|0|TRAITMAX"
                ],
                [
                    traitID++,
                    "Drive", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Physical,
                    $"{VtEKeywords.MinMax}|0|TRAITMAX"
                ],
                [
                    traitID++,
                    "Firearms", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Physical,
                    $"{VtEKeywords.MinMax}|0|TRAITMAX"
                ],
                [
                    traitID++,
                    "Larceny", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Physical,
                    $"{VtEKeywords.MinMax}|0|TRAITMAX"
                ],
                [
                    traitID++,
                    "Stealth", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Physical,
                    $"{VtEKeywords.MinMax}|0|TRAITMAX"
                ],
                [
                    traitID++,
                    "Survival", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Physical,
                    $"{VtEKeywords.MinMax}|0|TRAITMAX"
                ],
                [
                    traitID++,
                    "Weaponry", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Physical,
                    $"{VtEKeywords.MinMax}|0|TRAITMAX"
                ],

                [
                    traitID++,
                    "Animal Ken", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Social,
                    $"{VtEKeywords.MinMax}|0|TRAITMAX"
                ],
                [
                    traitID++,
                    "Empathy", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Social,
                    $"{VtEKeywords.MinMax}|0|TRAITMAX"
                ],
                [
                    traitID++,
                    "Expression", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Social,
                    $"{VtEKeywords.MinMax}|0|TRAITMAX"
                ],
                [
                    traitID++,
                    "Intimidation", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Social,
                    $"{VtEKeywords.MinMax}|0|TRAITMAX"
                ],
                [
                    traitID++,
                    "Persuasion", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Social,
                    $"{VtEKeywords.MinMax}|0|TRAITMAX"
                ],
                [
                    traitID++,
                    "Socialize", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Social,
                    $"{VtEKeywords.MinMax}|0|TRAITMAX"
                ],
                [
                    traitID++,
                    "Streetwise", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Social,
                    $"{VtEKeywords.MinMax}|0|TRAITMAX"
                ],
                [
                    traitID++,
                    "Subterfuge", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Social,
                    $"{VtEKeywords.MinMax}|0|TRAITMAX"
                ],

                [
                    traitID++,
                    "Academics", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Mental,
                    $"{VtEKeywords.MinMax}|0|TRAITMAX"
                ],
                [
                    traitID++,
                    "Computer", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Mental,
                    $"{VtEKeywords.MinMax}|0|TRAITMAX"
                ],
                [
                    traitID++,
                    "Crafts", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Mental,
                    $"{VtEKeywords.MinMax}|0|TRAITMAX"
                ],
                [
                    traitID++,
                    "Investigation", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Mental,
                    $"{VtEKeywords.MinMax}|0|TRAITMAX"
                ],
                [
                    traitID++,
                    "Medicine", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Mental,
                    $"{VtEKeywords.MinMax}|0|TRAITMAX"
                ],
                [
                    traitID++,
                    "Occult", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Mental,
                    $"{VtEKeywords.MinMax}|0|TRAITMAX"
                ],
                [
                    traitID++,
                    "Politics", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Mental,
                    $"{VtEKeywords.MinMax}|0|TRAITMAX"
                ],
                [
                    traitID++,
                    "Science", //name
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Skill,
                    (int)TraitSubCategory.Mental,
                    $"{VtEKeywords.MinMax}|0|TRAITMAX"
                ],
                #endregion

                [
                    traitID++,
                    "True Faith", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Faith,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|5"
                ],

                //TODO: Physical Disciplines/etc need to have specific powers implemented a special way if we want to have all derived ratings - maybe don't and have a MINUSCOUNT rule or something?
                #region Disciplines
                [
                    traitID++,
                    "Animalism", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Auspex", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Celerity", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Chimerstry", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Dementation", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Dominate", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Fortitude", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Necromancy", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.MainTraitMax}"
                ],
                [
                    traitID++,
                    "The Ash Path", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.SubTrait}|Necromancy"
                ],
                [
                    traitID++,
                    "The Bone Path", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.SubTrait}|Necromancy"
                ],
                [
                    traitID++,
                    "The Cenotaph Path", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.SubTrait}|Necromancy"
                ],
                [
                    traitID++,
                    "The Corpse in the Monster", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.SubTrait}|Necromancy"
                ],
                [
                    traitID++,
                    "The Grave’s Decay", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.SubTrait}|Necromancy"
                ],
                [
                    traitID++,
                    "The Path of the Four Humors", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.SubTrait}|Necromancy"
                ],
                [
                    traitID++,
                    "The Sepulchre Path", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.SubTrait}|Necromancy"
                ],
                [
                    traitID++,
                    "The Vitreous Path", //name
                    (int)TraitType.DerivedTrait, //TODO: Might beome IntegerTrait
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.SubTrait}|Necromancy"
                ],
                [
                    traitID++,
                    "Obeah", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Obfuscate", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Obtenebration", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Potence", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Presence", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Protean", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Quietus", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Serpentis", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Thaumaturgy", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.MainTraitMax}"
                ],
                [
                    traitID++,
                    "Elemental Mastery", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.SubTrait}|Thaumaturgy"
                ],
                [
                    traitID++,
                    "The Green Path", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.SubTrait}|Thaumaturgy"
                ],
                [
                    traitID++,
                    "Hands of Destruction", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.SubTrait}|Thaumaturgy"
                ],
                [
                    traitID++,
                    "Movement of the Mind", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.SubTrait}|Thaumaturgy"
                ],
                [
                    traitID++,
                    "Neptune’s Might", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.SubTrait}|Thaumaturgy"
                ],
                [
                    traitID++,
                    "The Lure of Flames", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.SubTrait}|Thaumaturgy"
                ],
                [
                    traitID++,
                    "The Path of Blood", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.SubTrait}|Thaumaturgy"
                ],
                [
                    traitID++,
                    "The Path of Conjuring", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.SubTrait}|Thaumaturgy"
                ],
                [
                    traitID++,
                    "The Path of Corruption", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.SubTrait}|Thaumaturgy"
                ],
                [
                    traitID++,
                    "The Path of Mars", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.SubTrait}|Thaumaturgy"
                ],
                [
                    traitID++,
                    "The Path of Technomancy", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.SubTrait}|Thaumaturgy"
                ],
                [
                    traitID++,
                    "The Path of the Father’s Vengeance", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.SubTrait}|Thaumaturgy"
                ],
                [
                    traitID++,
                    "Weather Control", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.SubTrait}|Thaumaturgy"
                ],
                [
                    traitID++,
                    "Vicissitude", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.MainTraitCount}"
                ],

                //Branch Disciplines
                [
                    traitID++,
                    "Ogham", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Temporis", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Valeren", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.MainTraitCount}"
                ],


                [
                    traitID++,
                    "Assamite Sorcery", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.MainTraitMax}"
                ],
                [
                    traitID++,
                    "Awakening of the Steel", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.SubTrait}|Assamite Sorcery\n{VtEKeywords.MainTraitCount}|A:Awakening of the Steel"
                ],
                [
                    traitID++,
                    "Hands of Destruction", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.SubTrait}|Assamite Sorcery\n{VtEKeywords.MainTraitCount}|A:Hands of Destruction"
                ],
                [
                    traitID++,
                    "Movement of the Mind", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.SubTrait}|Assamite Sorcery\n{VtEKeywords.MainTraitCount}|A:Movement of the Mind"
                ],
                [
                    traitID++,
                    "The Lure of Flames", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.SubTrait}|Assamite Sorcery\n{VtEKeywords.MainTraitCount}|A:The Lure of Flames"
                ],
                [
                    traitID++,
                    "The Path of Blood", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.SubTrait}|Assamite Sorcery\n{VtEKeywords.MainTraitCount}|A:The Path of Blood"
                ],
                [
                    traitID++,
                    "The Path of Conjuring", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.SubTrait}|Assamite Sorcery\n{VtEKeywords.MainTraitCount}|A:The Path of Conjuring"
                ],

                [
                    traitID++,
                    "Bardo", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Countermagic", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Daimoinon", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Flight", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Koldunic Sorcery", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.MainTraitMax}"
                ],
                [
                    traitID++,
                    "The Way of Earth", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.SubTrait}|Koldunic Sorcery"
                ],
                [
                    traitID++,
                    "The Way of Fire", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.SubTrait}|Koldunic Sorcery"
                ],
                [
                    traitID++,
                    "The Way of Water", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.SubTrait}|Koldunic Sorcery"
                ],
                [
                    traitID++,
                    "The Way of Wind", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|MAGICMAX\n{VtEKeywords.SubTrait}|Koldunic Sorcery"
                ],
                [
                    traitID++,
                    "Melpominee", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Mytherceria", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Sanguinus", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.MainTraitCount}"
                ],
                [
                    traitID++,
                    "Visceratika", //name
                    (int)TraitType.DerivedTrait,
                    (int)TraitCategory.Power,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.MainTraitCount}"
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
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Animalism\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Beckoning (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Animalism\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Animal Succulence (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Animalism\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Quell the Beast (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Animalism\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Species Speech (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Animalism\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Subsume the Spirit (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Animalism\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Drawing Out the Beast (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Animalism\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shared Soul (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Animalism\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Heart of the Pack (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Animalism\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Conquer the Beast (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Animalism\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Nourish the Savage Beast (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Animalism\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Subsume the Pack (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Animalism\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Taunt The Caged Beast (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Animalism\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Heart of the Wild (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Animalism\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Unchain the Beast (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Animalism\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Animalism Supremacy (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Animalism\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Heightened Senses (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Auspex\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Aura Perception (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Auspex\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Spirit’s Touch (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Auspex\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Ever-Watchful Eye (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Auspex\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Telepathy (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Auspex\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Breach The Mind’s Sanctum (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Auspex\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Mind to Mind (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Auspex\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Karmic Sight (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Auspex\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Through Another’s Eyes (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Auspex\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Into Another’s Heart (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Auspex\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Eyes of the Grave (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Auspex\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "False Slumber (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Auspex\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Auspex Supremacy (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Auspex\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Enrich the Spirit (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Bardo\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Quell the Ravening Serpent (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Bardo\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Vows Unbroken (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Bardo\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Gift of Apis (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Bardo\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Whisper of Dawn (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Bardo\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Boon of Anubis (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Bardo\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Bring Forth the Dawn (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Bardo\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Pillar of Osiris (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Bardo\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Mummification (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Bardo\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Ra’s Blessing (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Bardo\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Celerity Supremacy (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Celerity\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Ignis Fatuus (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Chimerstry\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Fata Morgana (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Chimerstry\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Apparition (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Chimerstry\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Permanency (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Chimerstry\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Horrid Reality (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Chimerstry\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "False Resonance (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Chimerstry\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Fatuus Mastery (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Chimerstry\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shared Nightmare (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Chimerstry\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Far Fatuus (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Chimerstry\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Suspension of Disbelief (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Chimerstry\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Figment (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Chimerstry\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Through The Cracks (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Chimerstry\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Chimerstry Supremacy (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Chimerstry\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Sense The Sin (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Daimoinon\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Fear of the Void Below (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Daimoinon\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Conflagration (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Daimoinon\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Psychomachia (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Daimoinon\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Beastly Pact (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Daimoinon\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Concordance (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Daimoinon\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Herald of Topheth (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Daimoinon\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Contagion (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Daimoinon\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Call the Great Beast (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Daimoinon\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Passion (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Daimoinon\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Fracture (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Daimoinon\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Eyes of Chaos (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Daimoinon\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Voice of Insanity (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Daimoinon\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Total Madness (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Daimoinon\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Babble (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Daimoinon\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Sibyl’s Tongue (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Daimoinon\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Weaving the Tapestry (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Daimoinon\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shattered Mirror (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Daimoinon\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Speak To The Stars (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Daimoinon\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Father’s Blood (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Daimoinon\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Command (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Dominate\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Mesmerize (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Dominate\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Forgetful Mind (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Dominate\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Conditioning (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Dominate\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Obedience (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Dominate\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Possession (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Dominate\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Chain the Psyche (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Dominate\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Loyalty (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Dominate\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Mass Manipulation (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Dominate\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Still the Mortal Flesh (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Dominate\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Far Mastery (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Dominate\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Speak Through the Blood (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Dominate\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Dominate Supremacy (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Dominate\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Personal Armor (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Fortitude\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shared Strength (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Fortitude\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Fortitude Supremacy (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Fortitude\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Missing Voice (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Melpominee\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Phantom Speaker (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Melpominee\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Madrigal (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Melpominee\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Virtuosa (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Melpominee\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Siren’s Beckoning (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Melpominee\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Persistent Echo (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Melpominee\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shattering Crescendo (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Melpominee\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Riddle (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Mytherceria\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Fae Sight (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Mytherceria\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Oath of Iron (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Mytherceria\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Walk In Dreaming (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Mytherceria\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Fae Words (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Mytherceria\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Iron In The Mind (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Mytherceria\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Elysian Glade (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Mytherceria\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Geas (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Mytherceria\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Wyrd (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Mytherceria\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Sense Vitality (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obeah\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Anesthetic Touch (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obeah\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Corpore Sano (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obeah\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Mens Sana (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obeah\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Truce (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obeah\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Blood Of My Blood (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obeah\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Flesh Of My Flesh (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obeah\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Beating Heart (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obeah\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Safe Passage (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obeah\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Unburdening the Bestial Soul (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obeah\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Lifesense (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obeah\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Renewed Vigor (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obeah\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Life Through Death (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obeah\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Keeper Of The Flock (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obeah\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Obeah Supremacy (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obeah\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Cloak of Shadows (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obfuscate\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Unseen Presence (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obfuscate\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Mask of a Thousand Faces (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obfuscate\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Vanish from the Mind’s Eye (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obfuscate\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Cloak the Gathering (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obfuscate\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Conceal (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obfuscate\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Soul Mask (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obfuscate\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Cache (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obfuscate\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Veil of Blissful Ignorance (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obfuscate\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Old Friend (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obfuscate\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Avalonian Mist (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obfuscate\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Create Name (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obfuscate\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Obfuscate Supremacy (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obfuscate\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shadow Play (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obtenebration\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shroud of Night (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obtenebration\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Arms of the Abyss (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obtenebration\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Black Metamorphosis (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obtenebration\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Tenebrous Form (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obtenebration\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Tenebrous Mastery (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obtenebration\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Darkness Within (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obtenebration\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shadowstep (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obtenebration\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shadow Twin (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obtenebration\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Witness in Darkness (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obtenebration\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Oubliette (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obtenebration\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Ahriman’s Demesne (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obtenebration\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Keeper of the Shadowlands (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obtenebration\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Obtenebration Supremacy (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Obtenebration\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Consecrate the Grove (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Ogham\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Crimson Woad (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Ogham\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Inscribe the Curse (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Ogham\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Aspect of the Beast (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Ogham\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Moon and Sun (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Ogham\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Drink Dry the Earth (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Ogham\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Earthshock (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Potence\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Flick (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Potence\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Potence Supremacy (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Potence\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Awe (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Presence\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Dread Gaze (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Presence\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Entrancement (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Presence\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Summon (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Presence\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Majesty (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Presence\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Love (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Presence\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Paralyzing Glance (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Presence\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Spark of Rage (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Presence\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Cooperation (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Presence\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Ironclad Command (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Presence\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Pulse of the City (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Presence\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Presence Supremacy (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Presence\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Eyes of the Beast (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Protean\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Feral Claws (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Protean\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Earth Meld (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Protean\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Mist Form (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Protean\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shape of the Beast (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Protean\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Restore the Mortal Visage (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Protean\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Earth Control (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Protean\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Flesh of Marble (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Protean\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shape of the Beast’s Wrath (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Protean\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Spectral Body (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Protean\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Purify the Impaled Beast (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Protean\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Inward Focus (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Protean\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Protean Supremacy (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Protean\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Silence of Death (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Quietus\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Scorpion’s Touch (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Quietus\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Dagon’s Call (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Quietus\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Baal’s Caress (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Quietus\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Blood Burn (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Quietus\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Taste of Death (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Quietus\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Purification (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Quietus\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Ripples of the Heart (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Quietus\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Selective Silence (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Quietus\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Baal’s Bloody Talons (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Quietus\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Poison the Well of Life (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Quietus\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Songs of Distant Vitae (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Quietus\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Condemn the Sins of  the Father (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Quietus\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Quietus Supremacy (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Quietus\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Brother’s Blood (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Sanguinus\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Octopod (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Sanguinus\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Gestalt (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Sanguinus\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Walk of Caine (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Sanguinus\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Coagulated Entity (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Sanguinus\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Eyes of the Serpent (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Serpentis\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Tongue of the Asp (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Serpentis\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Skin of the Adder (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Serpentis\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Form of the Cobra (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Serpentis\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Heart of Darkness (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Serpentis\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Cobra Fangs (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Serpentis\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Divine Image (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Serpentis\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Heart Thief (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Serpentis\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shadow of Apep (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Serpentis\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Serpentis Supremacy (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Serpentis\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Hourglass of the Mind (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Temporis\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Recurring Contemplation (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Temporis\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Leaden Moment (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Temporis\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Patience of the Norns (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Temporis\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Clotho’s Gift (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Temporis\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Kiss of Lachesis (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Temporis\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "See Between Moments (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Temporis\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Clio’s Kiss (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Temporis\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Cheat the Fates (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Temporis\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Sense Infirmity (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Valeren\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Seek the Hated Foe (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Valeren\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Touch of Abaddon (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Valeren\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Armor of Caine’s Fury (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Valeren\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Sword of Michael (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Valeren\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Malleable Visage (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Vicissitude\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Fleshcraft (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Vicissitude\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Bonecraft (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Vicissitude\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Horrid Form (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Vicissitude\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Bloodform (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Vicissitude\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Chiropteran Marauder (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Vicissitude\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Cocoon (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Vicissitude\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Breath of the Dragon (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Vicissitude\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Earth’s Vast Haven (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Vicissitude\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Zahhak (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Vicissitude\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Vicissitude Supremacy (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Vicissitude\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Stoneskin (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Claws of Stone (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Scry the Hearthstone (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Humble As The Earth (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Reshape the Fortress (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Sand Form (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Flesh to Stone (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Golem (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Heightened Senses (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Aura Perception (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Spirit’s Touch (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Ever-Watchful Eye (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Telepathy (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Breach The Mind’s Sanctum (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Mask of a Thousand Faces (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Vanish from the Mind’s Eye (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Cloak the Gathering (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Eyes of the Beast (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Earth Meld (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Mist Form (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shape of the Beast (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Restore the Mortal Visage (••••• •)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Mind to Mind (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Conceal (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Soul Mask (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Earth Control (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Flesh of Marble (••••• ••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Through Another’s Eyes (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Cache (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Veil of Blissful Ignorance (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shape of the Beast’s Wrath (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Spectral Body (••••• •••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Into Another’s Heart (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Old Friend (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Purify the Impaled Beast (••••• ••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Eyes of the Grave (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "False Slumber (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Avalonian Mist (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Create Name (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Inward Focus (••••• •••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Visceratika\n{VtEKeywords.PowerLevel}"
                ],


                //Assamite Sorcery
                [
                    traitID++,
                    "Confer with the Blade (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|A:Awakening of the Steel\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Grasp of the Mountain (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|A:Awakening of the Steel\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Pierce Steel’s Skin (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|A:Awakening of the Steel\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Razor’s Shield (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|A:Awakening of the Steel\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Spirit of Zulfiqar (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|A:Awakening of the Steel\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Decay (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|A:Hands of Destruction\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Gnarl Wood (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|A:Hands of Destruction\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Acidic Touch (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|A:Hands of Destruction\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Atrophy (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|A:Hands of Destruction\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Turn to Dust (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|A:Hands of Destruction\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Movement of the Mind (1)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|A:Movement of the Mind\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Movement of the Mind (2)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|A:Movement of the Mind\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Movement of the Mind (3)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|A:Movement of the Mind\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Movement of the Mind (4)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|A:Movement of the Mind\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Movement of the Mind (5)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|A:Movement of the Mind\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Lure of Flames (1)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|A:The Lure of Flames\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Lure of Flames (2)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|A:The Lure of Flames\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Lure of Flames (3)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|A:The Lure of Flames\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Lure of Flames (4)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|A:The Lure of Flames\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Lure of Flames (5)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|A:The Lure of Flames\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "A Taste for Blood (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|A:The Path of Blood\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Blood Rage (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|A:The Path of Blood\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Blood of Potency (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|A:The Path of Blood\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Theft of Vitae (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|A:The Path of Blood\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Cauldron of Blood (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|A:The Path of Blood\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Summon the Simple Form (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|A:The Path of Conjuring\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Permanency (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|A:The Path of Conjuring\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Magic of the Smith (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|A:The Path of Conjuring\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Reverse Conjuration (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|A:The Path of Conjuring\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Power Over Life (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|A:The Path of Conjuring\n{VtEKeywords.PowerLevel}"
                ],


                //Koldunic Sorcery
                [
                    traitID++,
                    "Grasping Soil (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Way of Earth\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Endurance of Stone (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Way of Earth\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Hungry Earth (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Way of Earth\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Root of Vitality (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Way of Earth\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Kupala’s Fury (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Way of Earth\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Fiery Courage (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Way of Fire\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Combust (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Way of Fire\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Wall of Magma (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Way of Fire\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Heat Wave (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Way of Fire\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Volcanic Blast (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Way of Fire\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Pool of Lies (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Way of Water\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Watery Haven (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Way of Water\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Fog Over Sea (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Way of Water\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Minions of the Deep (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Way of Water\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Dessicate (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Way of Water\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Doom Tide (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Way of Water\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Breath of Whispers (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Way of Wind\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Biting Gale (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Way of Wind\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Breeze of Lethargy (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Way of Wind\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Ride the Tempest (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Way of Wind\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Tempest (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Way of Wind\n{VtEKeywords.PowerLevel}"
                ],


                //Necromancy
                [
                    traitID++,
                    "Shroudsight (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Ash Path\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Lifeless Tongues (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Ash Path\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Dead Hand (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Ash Path\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Ex Nihilo (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Ash Path\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shroud Mastery (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Ash Path\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Tremens (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Bone Path\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Apprentice’s Brooms (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Bone Path\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Shambling Hordes (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Bone Path\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Soul Stealing (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Bone Path\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Daemonic Possession (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Bone Path\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "A Touch of Death (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Cenotaph Path\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Reveal the Catene (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Cenotaph Path\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Tread Upon the Grave (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Cenotaph Path\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Death Knell (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Cenotaph Path\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Ephemeral Binding (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Cenotaph Path\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Masque of Death (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Corpse in the Monster\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Cold of the Grave (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Corpse in the Monster\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Curse of Life (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Corpse in the Monster\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Gift of the Corpse (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Corpse in the Monster\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Gift of Life (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Corpse in the Monster\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Destroy the Husk (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Grave’s Decay\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Rigor Mortis (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Grave’s Decay\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Wither (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Grave’s Decay\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Corrupt the Undead Flesh (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Grave’s Decay\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Dissolve the Flesh (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Grave’s Decay\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Whispers to the Soul (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of the Four Humors\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Kiss of the Dark Mother (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of the Four Humors\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Dark Humors (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of the Four Humors\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Clutching the Shroud (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of the Four Humors\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Black Breath (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of the Four Humors\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Witness of Death (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Sepulchre Path\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Summon Soul (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Sepulchre Path\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Compel Soul (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Sepulchre Path\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Haunting (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Sepulchre Path\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Torment (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Sepulchre Path\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Eyes of the Dead (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Vitreous Path\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Aura of Decay (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Vitreous Path\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Soul Feast (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Vitreous Path\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Breath of Thanatos (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Vitreous Path\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Night Cry (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Vitreous Path\n{VtEKeywords.PowerLevel}"
                ],


                //Thaumaturgy
                [
                    traitID++,
                    "Elemental Strength (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Elemental Mastery\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Wooden Tongues (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Elemental Mastery\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Animate the Unmoving (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Elemental Mastery\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Elemental Form (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Elemental Mastery\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Summon Elemental (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Elemental Mastery\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Herbal Wisdom (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Green Path\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Speed the Season’s Passing (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Green Path\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Dance of Vines (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Green Path\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Verdant Haven (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Green Path\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Awaken the Forest Giants (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Green Path\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Decay (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Hands of Destruction\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Gnarl Wood (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Hands of Destruction\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Acidic Touch (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Hands of Destruction\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Atrophy (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Hands of Destruction\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Turn to Dust (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Hands of Destruction\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Movement of the Mind (1)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Movement of the Mind\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Movement of the Mind (2)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Movement of the Mind\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Movement of the Mind (3)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Movement of the Mind\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Movement of the Mind (4)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Movement of the Mind\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Movement of the Mind (5)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Movement of the Mind\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Eyes of the Sea (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Neptune’s Might\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Prison of Water (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Neptune’s Might\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Blood to Water (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Neptune’s Might\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Flowing Wall (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Neptune’s Might\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Dehydrate (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Neptune’s Might\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Lure of Flames (1)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Lure of Flames\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Lure of Flames (2)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Lure of Flames\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Lure of Flames (3)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Lure of Flames\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Lure of Flames (4)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Lure of Flames\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Lure of Flames (5)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Lure of Flames\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "A Taste for Blood (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of Blood\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Blood Rage (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of Blood\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Blood of Potency (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of Blood\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Theft of Vitae (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of Blood\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Cauldron of Blood (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of Blood\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Summon the Simple Form (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of Conjuring\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Permanency (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of Conjuring\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Magic of the Smith (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of Conjuring\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Reverse Conjuration (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of Conjuring\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Power Over Life (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of Conjuring\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Contradict (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of Corruption\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Subvert (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of Corruption\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Dissociate (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of Corruption\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Addiction (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of Corruption\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Dependence (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of Corruption\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "War Cry (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of Mars\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Strike True (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of Mars\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Wind Dance (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of Mars\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Fearless Heart (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of Mars\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Comrades at Arms (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of Mars\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Analyze (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of Technomancy\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Burnout (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of Technomancy\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Encrypt/Decrypt (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of Technomancy\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Remote Access (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of Technomancy\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Telecommute (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of Technomancy\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Zillah’s Litany (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of the Father’s Vengeance\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "The Crone’s Pride (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of the Father’s Vengeance\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Feast of Ashes (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of the Father’s Vengeance\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Uriel’s Disfavor (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of the Father’s Vengeance\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Valediction (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|The Path of the Father’s Vengeance\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Weather Control (1)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Weather Control\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Weather Control (2)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Weather Control\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Weather Control (3)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Weather Control\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Weather Control (4)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Weather Control\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Weather Control (5)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|Weather Control\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Turning (•)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|True Faith\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Scourging (••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|True Faith\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Laying on Hands (•••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|True Faith\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Sanctification (••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|True Faith\n{VtEKeywords.PowerLevel}"
                ],
                [
                    traitID++,
                    "Fear Not (•••••)", //name
                    (int)TraitType.SelectableTrait,
                    (int)TraitCategory.SpecificPower,
                    (int)TraitSubCategory.Discipline,
                    $"{VtEKeywords.AutoHide}\n{VtEKeywords.MinMax}|0|TRAITMAX\n{VtEKeywords.SubTrait}|True Faith\n{VtEKeywords.PowerLevel}"
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
                    $"{VtEKeywords.MinMax}|0|BACKGROUNDMAX"
                ],
                [
                    traitID++,
                    "Alternate Identity",
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Background,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.MinMax}|0|BACKGROUNDMAX"
                ],
                [
                    traitID++,
                    "Contacts",
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Background,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.MinMax}|0|BACKGROUNDMAX"
                ],
                [
                    traitID++,
                    "Domain",
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Background,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.MinMax}|0|BACKGROUNDMAX"
                ],
                [
                    traitID++,
                    "Fame",
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Background,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.MinMax}|0|BACKGROUNDMAX"
                ],
                [
                    traitID++,
                    "Generation",
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Background,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.MinMax}|0|GENERATIONMAX"
                ],
                [
                    traitID++,
                    "Herd",
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Background,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.MinMax}|0|BACKGROUNDMAX"
                ],
                [
                    traitID++,
                    "Influence",
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Background,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.MinMax}|0|BACKGROUNDMAX"
                ],
                [
                    traitID++,
                    "Mentor",
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Background,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.MinMax}|0|BACKGROUNDMAX"
                ],
                [
                    traitID++,
                    "Resources",
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Background,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.MinMax}|0|BACKGROUNDMAX"
                ],
                [
                    traitID++,
                    "Retainers",
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Background,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.MinMax}|0|BACKGROUNDMAX"
                ],
                [
                    traitID++,
                    "Status",
                    (int)TraitType.IntegerTrait,
                    (int)TraitCategory.Background,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.MinMax}|0|BACKGROUNDMAX"
                ],
                #endregion

                [
                    traitID++,
                    "Path",
                    (int)TraitType.PathTrait,
                    (int)TraitCategory.MoralPath,
                    (int)TraitSubCategory.None,
                    $"{VtEKeywords.MinMax}|0|PATHMAX"
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
            #endregion

            //template for the above:
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

            //build the actual datatable rows
            foreach (object?[] row in rawTraits)
            {
                traits.Rows.Add(row);
            }

            return traits;
        }

        /// <summary>
        /// A Dictionary mapping a trait name to all trait IDs which have that name.
        /// In most cases, this is a one-to-one mapping, but some traits share a name 
        /// (such as the Generation Background and its derived top trait, or magic paths belonging to multiple main Disciplines).
        /// </summary>
        private static readonly ReadOnlyDictionary<string, List<int>> _traitIDsByName = BuildTraitIDsByName();
        private static ReadOnlyDictionary<string, List<int>> BuildTraitIDsByName()
        {
            Dictionary<string, List<int>> output = new(_traits.Rows.Count);

            foreach (DataRow row in _traits.Rows)
            {
                //we don't really have a way to log problems with this, but it should also never happen. Again, in a real project, we'd try/catch and log the error to a log file or the database,
                //but since we're essentially operating on dummy data, there's no real need.
                //Even so, I'm giving myself a spot to put a breakpoint in case something odd happens.
                if (row[1] == null || row[0] == null)
                {

                }
                string key = row[1].ToString() ?? "BAD TRAIT NAME";
                int id = int.Parse(row[0].ToString() ?? "-1");
                if (output.TryGetValue(key, out List<int>? traitsOfThisName))
                {
                    traitsOfThisName.Add(id);
                }
                else
                {
                    output[key] = [id];
                }
            }

            return new(output);
        }

        /// <summary>
        /// A DataTable emulating a crosswalk table mapping template IDs to the IDs of their respective traits.
        /// </summary>
        private static readonly DataTable _template_x_trait = BuildTemplateXTrait();

        private static DataTable BuildTemplateXTrait()
        {
            DataTable template_x_trait = new()
            {
                Columns =
                {
                    new DataColumn("TEMPLATE_ID", typeof(int)),
                    new DataColumn("TRAIT_ID", typeof(int)),
                }
            };

            #region Build template_x_trait
            string[] mortalTraitNames = [

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

                "Path"

                //TODO: Weapons, Physical Description
            ];

            foreach (string traitName in mortalTraitNames)
            {
                foreach (int id in _traitIDsByName[traitName])
                {
                    template_x_trait.Rows.Add((int)TemplateKey.Mortal, id);
                }
            }

            #endregion

            return template_x_trait;
        }

        private static readonly DataTable _paths = BuildPathTable();
        private static DataTable BuildPathTable()
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
                    string.Join('\n',
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
                    string.Join('\n',
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
                    string.Join('\n',
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
                    string.Join('\n',
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
                    string.Join('\n',
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
                    string.Join('\n',
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
                    string.Join('\n',
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
                    string.Join('\n',
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

        #region Pseudo reference tables
        private static IEnumerable<string> GetAllArchetypes()
        {
            return [
                "Architect",
                "Autocrat",
                "Bon Vivant",
                "Bravo",
                "Capitalist",
                "Caregiver",
                "Celebrant",
                "Chameleon",
                "Child",
                "Competitor",
                "Conformist",
                "Conniver",
                "Creep Show",
                "Curmudgeon",
                "Dabbler",
                "Deviant",
                "Director",
                "Enigma",
                "Eye of the Storm",
                "Fanatic",
                "Gallant",
                "Guru",
                "Idealist",
                "Judge",
                "Loner",
                "Martyr",
                "Masochist",
                "Monster",
                "Pedagogue",
                "Penitent",
                "Perfectionist",
                "Rebel",
                "Rogue",
                "Sadist",
                "Scientist",
                "Soldier",
                "Survivor",
                "Thrill-Seeker",
                "Traditionalist",
                "Trickster",
                "Visionary"
            ];
        }

        private static IEnumerable<string> GetAllClans()
        {
            return [
                "Assamite (Hunter)",
                "Assamite (Vizier)",
                "Baali",
                "Blood Brothers",
                "Brujah",
                "Caitiff",
                "Cappadocian (Scholar)",
                "Cappadocian (Templar)",
                "Daughters of Cacophony",
                "Followers of Set",
                "Followers of Set (Warrior)",
                "Gangrel (City)",
                "Gangrel (Country)",
                "Gangrel (Pagan)",
                "Gargoyles",
                "Giovanni",
                "Harbingers of Skulls",
                "Kiasyd",
                "Lasombra",
                "Malkavian",
                "Nosferatu",
                "Ravnos",
                "Ravnos (Brahman)",
                "Salubri (Healer)",
                "Salubri (Warrior)",
                "Toreador",
                "Tremere",
                "True Brujah",
                "Tzimisce",
                "Ventrue"
            ];
        }

        private static IEnumerable<string> GetAllBroods()
        {
            return [
                "Kalebite",
                "Aragite",
                "Arumite",
                "Chatul (Tiger)",
                "Chatul (Lion)",
                "Chatul (Panther/Jaguar)",
                "Chatul (Cheetah)",
                "Chatul (Housecat)",
                "Chatul (Bobcat)",
                "Dabberite",
                "Devoroth",
                "Kohen",
                "Nychterid",
                "Tashlimite",
                "Tekton",
                "Tsayadite",
                "Zakarite",
                "Rishon",
                "Chargol"
            ];
        }

        private static IEnumerable<string> GetBroodBreedSwitch()
        {
            return [
                "Kalebite",
                "Lycanth",
                "Aragite",
                "Arachnes",
                "Arumite",
                "Alopex",
                "Chatul (Tiger)",
                "Ailouros",
                "Chatul (Lion)",
                "Ailouros",
                "Chatul (Panther/Jaguar)",
                "Ailouros",
                "Chatul (Cheetah)",
                "Ailouros",
                "Chatul (Housecat)",
                "Ailouros",
                "Chatul (Bobcat)",
                "Ailouros",
                "Dabberite",
                "Koraki",
                "Devoroth",
                "Melisses",
                "Kohen",
                "Arctos",
                "Nychterid",
                "Chiroptera",
                "Tashlimite",
                "Kouneli",
                "Tekton",
                "Kastoras",
                "Tsayadite",
                "Aetos",
                "Zakarite",
                "Loxodon",
                "Rishon",
                "[animal]",
                "Chargol",
                "Skathari",
            ];
        }

        private static IEnumerable<string> GetAllBreeds()
        {
            return [
                "Anthrope",
                "Yvrid",
                "[animal]"
            ];
        }

        private static IEnumerable<string> GetAllTribes()
        {
            return [
                "Ash Walkers",
                "Daughters of Zevah",
                "Firstborn",
                "Hell's Wardens",
                "Midnight Runners",
                "Shadows of Death",
                "Souls of Harmony",
                "Swords of Kaleb",
                "Undefeated",
                "Wolves of Tefar",
                "Blood Jackals",
                "Golden Moons",
                "Peerless Hunters",
                "Sanguine Kings",
                "Shadow-Hunters",
                "Velvet Whispers"
            ];
        }

        private static IEnumerable<string> GetAllAuspices()
        {
            return [
                "Scurra",
                "Logios",
                "Iudex",
                "Legatus",
                "Myrmidon",
            ];
        }
        #endregion

        #endregion
    }
}
