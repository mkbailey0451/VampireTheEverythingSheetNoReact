namespace VampireTheEverythingSheetNoReact.Shared_Files
{
    public static class Utils
    {
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
    }
}
