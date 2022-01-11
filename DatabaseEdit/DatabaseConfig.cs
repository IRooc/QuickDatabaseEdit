using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DatabaseEdit
{
    public class DatabaseConfig
    {
        public DataTable ConfigTable { get; set; }

        const string query = @"select a.TABLE_SCHEMA, a.TABLE_NAME, a.ORDINAL_POSITION, a.COLUMN_NAME, a.COLUMN_DEFAULT, a.IS_NULLABLE, a.DATA_TYPE, a.CHARACTER_MAXIMUM_LENGTH, b.CONSTRAINT_NAME, c.CONSTRAINT_TYPE, d.PK_TableSchema, d.PK_Table, d.PK_Column, ISNULL(d.PK_Show, d.PK_Column) as PK_Show, COLUMNPROPERTY(OBJECT_ID(a.TABLE_SCHEMA+'.'+a.TABLE_NAME),a.COLUMN_NAME,'IsComputed') IS_COMPUTED,COLUMNPROPERTY(OBJECT_ID(a.TABLE_SCHEMA+'.'+a.TABLE_NAME),a.COLUMN_NAME,'IsIdentity') IS_IDENTITY
from INFORMATION_SCHEMA.COLUMNS a
left outer JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE b on a.TABLE_CATALOG = b.TABLE_CATALOG AND a.TABLE_SCHEMA = b.TABLE_SCHEMA AND a.TABLE_NAME = b.TABLE_NAME AND a.COLUMN_NAME = b.COLUMN_NAME
left outer JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS c on b.CONSTRAINT_NAME = c.CONSTRAINT_NAME
left outer join (
SELECT FK_Table = FK.TABLE_NAME,  FK_Column = CU.COLUMN_NAME,  PK_Table = PK.TABLE_NAME,  PK_Column = PT.COLUMN_NAME, PK_TableSchema = PK.TABLE_SCHEMA,  Constraint_Name = C.CONSTRAINT_NAME, (select top 1 column_name from INFORMATION_SCHEMA.COLUMNS where table_name = PK.TABLE_NAME and TABLE_SCHEMA = PK.TABLE_SCHEMA and data_type in ('nvarchar','varchar','char','nchar')) as PK_Show
FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS C INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS FK  ON C.CONSTRAINT_NAME = FK.CONSTRAINT_NAME
INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS PK ON C.UNIQUE_CONSTRAINT_NAME = PK.CONSTRAINT_NAME INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE CU ON C.CONSTRAINT_NAME = CU.CONSTRAINT_NAME
INNER JOIN (SELECT i1.TABLE_NAME, i2.COLUMN_NAME, i1.CONSTRAINT_SCHEMA FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS i1 INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE i2 ON i1.CONSTRAINT_NAME = i2.CONSTRAINT_NAME WHERE i1.CONSTRAINT_TYPE = 'PRIMARY KEY' ) PT ON PT.TABLE_NAME = PK.TABLE_NAME AND PT.CONSTRAINT_SCHEMA = PK.CONSTRAINT_SCHEMA
) d on d.FK_Table = a.TABLE_NAME AND b.CONSTRAINT_NAME = d.Constraint_Name
where a.TABLE_CATALOG = DB_NAME()";
        private readonly IConfiguration configuration;

        public DatabaseConfig(IConfiguration configuration)
        {
            using (var connection = new SqlConnection(configuration.GetConnectionString("Database")))
            {
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        ConfigTable = new DataTable();
                        ConfigTable.Load(reader);
                    }
                    connection.Close();
                }
            }

            this.configuration = configuration;
        }

        public IEnumerable<string> GetTables()
        {
            return ConfigTable.Rows.Cast<DataRow>().Select(r => r[0].ToString() + "." + r[1].ToString()).Distinct().OrderBy(s => s);
        }

        public TableResult GetTable(string table, string sortColumn, bool ascending)
        {
            var result = new TableResult();
            //check if param is valid
            result.Config = ConfigTable.Rows.Cast<DataRow>().Where(r => r[0].ToString() + "." + r[1].ToString() == table);
            if (result.Config.Any())
            {
                result.ConnectionString = configuration.GetConnectionString("Database");
                result.Table = table;
                result.LoadConfiguration();
                result.LoadData(sortColumn, ascending);
                return result;
            }
            return null;
        }
    }

    public class TableResult
    {
        public IEnumerable<DataRow> Config { get; set; }
        public DataTable Data { get; set; }
        public List<string> PrimaryKeys { get; set; } = new List<string>();
        public Dictionary<int, DataTable> ForeignKeys { get; set; } = new Dictionary<int, DataTable>();
        public string ConnectionString { get; set; }
        public string Table { get; set; }

        private string _sortColumn = null;
        private bool _sortAscending = true;

        public void LoadData(string sortColumn, bool sortAscending)
        {

            using (var connection = new SqlConnection(ConnectionString))
            {
                var query = "SELECT * FROM " + Table + " p ";
                var columnRow = Config.FirstOrDefault(r => r[3].ToString() == sortColumn);
                if (columnRow == null) sortColumn = null;

                if (!string.IsNullOrEmpty(sortColumn))
                {
                    var sort = $"p.{sortColumn}";
                    if (columnRow[9].ToString() == "FOREIGN KEY")
                    {
                        sort = $"(SELECT {columnRow[13]} FROM {columnRow[10]}.{columnRow[11]} f WHERE f.{columnRow[12]} = p.{sortColumn})";
                    }

                    query += " order by " + sort + " " + (sortAscending ? "ASC" : "DESC");
                }
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        Data = new DataTable();
                        Data.Load(reader);
                    }
                    connection.Close();
                }
            }
            _sortColumn = sortColumn;
            _sortAscending = sortAscending;
        }

        public void LoadConfiguration()
        {
            if (string.IsNullOrEmpty(Table)) return;

            foreach (var row in Config.Where(r => r[9].ToString() == "PRIMARY KEY"))
            {
                PrimaryKeys.Add(row[3].ToString());
            }

            using (var connection = new SqlConnection(ConnectionString))
            {
                foreach (var row in Config.Where(r => r[9].ToString() == "FOREIGN KEY"))
                {
                    var fquery = $"SELECT {row[12]} as [key], {row[13]} as [value] FROM {row[10]}.{row[11]} ORDER BY {row[13]}";
                    using (var command = new SqlCommand(fquery, connection))
                    {
                        var index = row.Field<int>(2);
                        connection.Open();
                        using (var reader = command.ExecuteReader())
                        {
                            var ftable = new DataTable();
                            ftable.Load(reader);
                            ForeignKeys.Add(index - 1, ftable);
                        }
                        connection.Close();


                    }
                }
            }
        }

        public bool UpdateRow(int row, Dictionary<string, string> updated)
        {
            if (!PrimaryKeys.Any())
            {
                //can't update database because there is no primary key
                return false;
            }
            if (row == -1)
            {
                return Add(updated);
            }
            return Update(row, updated);
        }

        private bool Add(Dictionary<string, string> updated)
        {
            var columns = string.Join(',', updated.Select(k => k.Key).ToArray());
            var values = string.Join(',', updated.Select(k => "@value" + k.Key).ToArray());

            var query = $"INSERT INTO {Table} ({columns}) VALUES({values})";
            using (var connection = new SqlConnection(ConnectionString))
            {
                using (var command = new SqlCommand(query, connection))
                {
                    foreach (var update in updated)
                    {
                        command.Parameters.AddWithValue("value" + update.Key, update.Value);
                    }
                    connection.Open();
                    var result = command.ExecuteNonQuery();
                    connection.Close();

                    LoadData(_sortColumn, _sortAscending);
                    return result > 0;
                }
            }
        }

        private bool Update(int row, Dictionary<string, string> updated)
        {
            var current = Data.Rows[row];

            var where = string.Empty;
            foreach (var key in PrimaryKeys)
            {
                if (!string.IsNullOrEmpty(where))
                {
                    where += " AND ";
                }
                where += $" {key} = @key{key} ";
            }
            var set = string.Empty;
            foreach (var update in updated)
            {
                if (!string.IsNullOrEmpty(set))
                {
                    set += " , ";
                }
                set += $" {update.Key} = @update{update.Key} ";
            }

            var query = $"UPDATE {Table} SET {set} WHERE {where}";
            using (var connection = new SqlConnection(ConnectionString))
            {
                using (var command = new SqlCommand(query, connection))
                {
                    foreach (var key in PrimaryKeys)
                    {
                        command.Parameters.AddWithValue("key" + key, current[key]);
                    }
                    foreach (var update in updated)
                    {
                        command.Parameters.AddWithValue("update" + update.Key, update.Value);
                    }
                    connection.Open();
                    var result = command.ExecuteNonQuery();
                    connection.Close();

                    LoadData(_sortColumn, _sortAscending);
                    return result > 0;
                }
            }
        }
    }
}
