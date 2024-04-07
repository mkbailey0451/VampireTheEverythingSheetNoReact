using System.Data;
using static VampireTheEverythingSheetNoReact.Shared_Files.VtEConstants;

namespace VampireTheEverythingSheetNoReact.Data_Access_Layer.FakeData
{
    public static class CharacterTemplateTable
    {
        public static DataTable Data {  get; private set; }

        static CharacterTemplateTable()
        {
            Data = BuildData();
        }

        private static DataTable BuildData()
        {
            DataTable templates = new()
            {
                TableName = "CHARACTER_TEMPLATES",
                Columns =
                {
                    new DataColumn("CHAR_TEMPLATE_ID", typeof(int)),
                    new DataColumn("CHAR_TEMPLATE_NAME", typeof(string)),
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
    }
}
