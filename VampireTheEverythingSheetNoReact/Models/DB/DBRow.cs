using System.Data;

namespace VampireTheEverythingSheetNoReact.Models.DB
{
    /// <summary>
    /// Generically implements an interface that satisfies both DataRow and SqlDataReader.
    /// This is used to wrap FakeDatabase and VtEDatabaseAccessLayer, and also prevents us 
    /// from passing the actual reader from VtEDatabaseAccessLayer to other data layers.
    /// </summary>
    public class DBRow
    {
        public DBRow(DataRow row)
        {
            IEnumerable<KeyValuePair<string, object>> pairs =
                from DataColumn col in row.Table.Columns
                select new KeyValuePair<string, object>
                (
                    col.ColumnName,
                    row[col.ColumnName]
                );

            _dict = new(pairs);
            _list = (from KeyValuePair<string, object> pair in pairs select pair.Value).ToArray();
        }

        public DBRow(IDataRecord record)
        {
            _dict = new(record.FieldCount);
            _list = new object[record.FieldCount];

            for (int x = 0; x < record.FieldCount; x++)
            {
                object val = record[x];
                string key = record.GetName(x);
                _list[x] = val;
                _dict[key] = val;
            }
        }

        public object this[string index]
        {
            get
            {
                return _dict[index];
            }
        }
        public object this[int index]
        {
            get
            {
                return _list[index];
            }
        }

        private readonly Dictionary<string, object> _dict;
        private readonly object[] _list;
    }
}
