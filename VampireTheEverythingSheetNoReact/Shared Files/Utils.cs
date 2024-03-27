using VampireTheEverythingSheetNoReact.Models;

namespace VampireTheEverythingSheetNoReact.Shared_Files
{
    public static class Utils
    {
        public const string ColumnPaddingClass = "p-5";

        /// <summary>
        /// Finds the maximum of more than two values, hopefuilly without allocating a new object on the heap.
        /// </summary>
        public static int Max(params int[] values)
        {
            return values.Max();
        }

        public static int? TryGetInt(object? input)
        {
            if (input == null || input == DBNull.Value)
            {
                return null;
            }
            if (
                (input is int intVal) ||
                (input is string stringVal && int.TryParse(stringVal, out intVal)) ||
                int.TryParse(input.ToString(), out intVal)
              )
            {
                return intVal;
            }
            return null;
        }

        public static int TryGetInt(object? input, int defaultValue)
        {
            return TryGetInt(input) ?? defaultValue;
        }

        public static bool TryGetInt(object? input, out int result)
        {
            int? trueResult = TryGetInt(input);
            result = trueResult ?? 0;
            return trueResult != null;
        }

        public static string? TryGetString(object? input)
        {
            if (input == null || input == DBNull.Value)
            {
                return null;
            }
            if (input is string strVal)
            {
                return strVal;
            }
            return input.ToString();
        }

        public static string TryGetString(object? input, string defaultValue)
        {
            return TryGetString(input) ?? defaultValue;
        }

        public static bool TryGetString(object? input, out string result)
        {
            string? trueResult = TryGetString(input);
            result = trueResult ?? "";
            return trueResult != null;
        }

        public static Trait[] VisibleOnly(IEnumerable<Trait> traits)
        {
            return (from trait in traits
                   where trait.Visible
                   select trait).ToArray();
        }

        public static Trait[] InvisibleOnly(IEnumerable<Trait> traits)
        {
            return (from trait in traits
                    where !trait.Visible
                    select trait).ToArray();
        }
    }
}
