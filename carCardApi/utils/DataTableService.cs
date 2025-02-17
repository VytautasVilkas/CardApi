using System.Data;

namespace carCard
{
    public class DataTableService
    {
        // Method to convert DataTable to JSON-compatible List<Dictionary<string, object>>
        public List<Dictionary<string, object>> ConvertToJson(DataTable dataTable)
        {
            var jsonResult = new List<Dictionary<string, object>>();

            foreach (DataRow row in dataTable.Rows)
            {
                var rowDict = new Dictionary<string, object>();
                foreach (DataColumn column in dataTable.Columns)
                {
                    rowDict[column.ColumnName] = row[column];
                }
                jsonResult.Add(rowDict);
            }

            return jsonResult;
        }
    }
}
