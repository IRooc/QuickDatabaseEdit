using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DatabaseEdit
{
    public class DatabaseConfig
    {
        public DataTable ConfigTable { get; set; }

        const string sqlServerQuery = @"select a.TABLE_SCHEMA, a.TABLE_NAME, a.ORDINAL_POSITION, a.COLUMN_NAME, a.COLUMN_DEFAULT, a.IS_NULLABLE, a.DATA_TYPE, a.CHARACTER_MAXIMUM_LENGTH, b.CONSTRAINT_NAME, c.CONSTRAINT_TYPE, d.PK_TableSchema, d.PK_Table, d.PK_Column, ISNULL(d.PK_Show, d.PK_Column) as PK_Show, COLUMNPROPERTY(OBJECT_ID(a.TABLE_SCHEMA+'.'+a.TABLE_NAME),a.COLUMN_NAME,'IsComputed') IS_COMPUTED,COLUMNPROPERTY(OBJECT_ID(a.TABLE_SCHEMA+'.'+a.TABLE_NAME),a.COLUMN_NAME,'IsIdentity') IS_IDENTITY
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

        // Preserve the metadata layout used by the existing editor. PostgreSQL also needs
        // the exact SQL type for parameter casts and a separate flag for PK/FK columns.
        const string postgresQuery = @"
SELECT n.nspname AS ""TABLE_SCHEMA"", t.relname AS ""TABLE_NAME"",
       a.attnum::integer AS ""ORDINAL_POSITION"", a.attname AS ""COLUMN_NAME"",
       pg_catalog.pg_get_expr(ad.adbin, ad.adrelid) AS ""COLUMN_DEFAULT"",
       c.is_nullable AS ""IS_NULLABLE"", c.data_type AS ""DATA_TYPE"",
       c.character_maximum_length AS ""CHARACTER_MAXIMUM_LENGTH"",
       COALESCE(fk.conname, pk.conname) AS ""CONSTRAINT_NAME"",
       CASE WHEN fk.conname IS NOT NULL THEN 'FOREIGN KEY'
            WHEN pk.conname IS NOT NULL THEN 'PRIMARY KEY' END AS ""CONSTRAINT_TYPE"",
       fk.table_schema AS ""PK_TableSchema"", fk.table_name AS ""PK_Table"",
       fk.column_name AS ""PK_Column"", fk.show_column AS ""PK_Show"",
       a.attgenerated <> '' AS ""IS_COMPUTED"",
       (a.attidentity <> '' OR COALESCE(pg_catalog.pg_get_expr(ad.adbin, ad.adrelid) LIKE 'nextval(%', false)) AS ""IS_IDENTITY"",
       pg_catalog.format_type(a.atttypid, NULL) AS ""SQL_TYPE"",
       pk.conname IS NOT NULL AS ""IS_PRIMARY_KEY""
FROM pg_catalog.pg_class t
JOIN pg_catalog.pg_namespace n ON n.oid = t.relnamespace
JOIN pg_catalog.pg_attribute a ON a.attrelid = t.oid AND a.attnum > 0 AND NOT a.attisdropped
JOIN information_schema.columns c ON c.table_schema = n.nspname AND c.table_name = t.relname AND c.column_name = a.attname
LEFT JOIN pg_catalog.pg_attrdef ad ON ad.adrelid = t.oid AND ad.adnum = a.attnum
LEFT JOIN pg_catalog.pg_constraint pk ON pk.conrelid = t.oid AND pk.contype = 'p' AND a.attnum = ANY(pk.conkey)
LEFT JOIN LATERAL (
    SELECT con.conname, rn.nspname AS table_schema, rt.relname AS table_name,
           ra.attname AS column_name,
           COALESCE((SELECT display.attname FROM pg_catalog.pg_attribute display
                     JOIN pg_catalog.pg_type dt ON dt.oid = display.atttypid
                     WHERE display.attrelid = rt.oid AND display.attnum > 0
                       AND NOT display.attisdropped AND dt.typcategory = 'S'
                     ORDER BY display.attnum LIMIT 1), ra.attname) AS show_column
    FROM pg_catalog.pg_constraint con
    JOIN pg_catalog.pg_class rt ON rt.oid = con.confrelid
    JOIN pg_catalog.pg_namespace rn ON rn.oid = rt.relnamespace
    JOIN pg_catalog.pg_attribute ra ON ra.attrelid = rt.oid
         AND ra.attnum = con.confkey[array_position(con.conkey, a.attnum)]
    WHERE con.conrelid = t.oid AND con.contype = 'f' AND a.attnum = ANY(con.conkey)
          AND cardinality(con.conkey) = 1
    ORDER BY con.oid LIMIT 1
) fk ON true
WHERE t.relkind IN ('r', 'p') AND NOT t.relispartition
      AND n.nspname NOT IN ('pg_catalog', 'information_schema') AND n.nspname NOT LIKE 'pg_toast%'
ORDER BY n.nspname, t.relname, a.attnum";

        public string ConnectionString { get; set; }
        public string ServerName { get; set; }
        public DatabaseType DatabaseType { get; set; } = DatabaseType.MSSQL;

        public DatabaseConfig()
        {

        }

        public void SetConnectionString(string connectionstring, DatabaseType databaseType = DatabaseType.MSSQL)
        {
            var serverName = DatabaseProvider.GetServerName(databaseType, connectionstring);
            var configTable = new DataTable();
            using (var connection = DatabaseProvider.CreateConnection(databaseType, connectionstring))
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = databaseType == DatabaseType.Postgres ? postgresQuery : sqlServerQuery;
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        configTable.Load(reader);
                    }
                    connection.Close();
                }
            }
            // Only switch the active database once the new connection succeeds.
            ConnectionString = connectionstring;
            DatabaseType = databaseType;
            ConfigTable = configTable;
            ServerName = serverName;
        }

        public IEnumerable<string> GetTables()
        {
            return ConfigTable?.Rows.Cast<DataRow>().Select(r => r[0].ToString() + "." + r[1].ToString()).Distinct().OrderBy(s => s) ?? Enumerable.Empty<string>();
        }

        public TableResult GetTable(string table, string sortColumn, bool ascending)
        {
            var result = new TableResult();
            //check if param is valid
            result.Config = ConfigTable?.Rows.Cast<DataRow>().Where(r => r[0].ToString() + "." + r[1].ToString() == table)
                ?? Enumerable.Empty<DataRow>();
            if (result.Config.Any())
            {
                result.ConnectionString = ConnectionString;
                result.DatabaseType = DatabaseType;
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
        public DatabaseType DatabaseType { get; set; } = DatabaseType.MSSQL;

        private string Quote(string identifier) => DatabaseProvider.QuoteIdentifier(DatabaseType, identifier);
        private string QualifiedTable => Quote(Config.First()[0].ToString()) + "." + Quote(Config.First()[1].ToString());

        private string _sortColumn = null;
        private bool _sortAscending = true;

        public void LoadData(string sortColumn, bool sortAscending)
        {

            using (var connection = DatabaseProvider.CreateConnection(DatabaseType, ConnectionString))
            {
                var query = "SELECT * FROM " + QualifiedTable + " p ";

                //check if sortColumn exists
                var columnRow = Config.FirstOrDefault(r => r[3].ToString() == sortColumn);
                if (columnRow == null) sortColumn = null;

                if (!string.IsNullOrEmpty(sortColumn))
                {
                    var sort = $"p.{Quote(sortColumn)}";
                    if (columnRow[9].ToString() == "FOREIGN KEY")
                    {
                        sort = $"(SELECT {Quote(columnRow[13].ToString())} FROM {Quote(columnRow[10].ToString())}.{Quote(columnRow[11].ToString())} f WHERE f.{Quote(columnRow[12].ToString())} = p.{Quote(sortColumn)})";
                    }

                    query += " order by " + sort + " " + (sortAscending ? "ASC" : "DESC");
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    DatabaseProvider.ReadValuesAsText(command);
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        Data = DatabaseProvider.LoadValues(reader, DatabaseType);
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

            PrimaryKeys.Clear();
            ForeignKeys.Clear();
            foreach (var row in Config.Where(r => r[9].ToString() == "PRIMARY KEY"
                || (r.Table.Columns.Contains("IS_PRIMARY_KEY") && Convert.ToBoolean(r["IS_PRIMARY_KEY"]))))
            {
                if (!PrimaryKeys.Contains(row[3].ToString())) PrimaryKeys.Add(row[3].ToString());
            }

            // A column can have multiple constraints; render it only once, in SELECT * order.
            Config = Config.GroupBy(r => r[3].ToString())
                .Select(g => g.OrderByDescending(r => r[9].ToString() == "FOREIGN KEY").First())
                .OrderBy(r => Convert.ToInt32(r[2])).ToList();

            using (var connection = DatabaseProvider.CreateConnection(DatabaseType, ConnectionString))
            {
                foreach (var row in Config.Where(r => r[9].ToString() == "FOREIGN KEY"))
                {
                    var fquery = $"SELECT {Quote(row[12].ToString())} as {Quote("key")}, {Quote(row[13].ToString())} as {Quote("value")} FROM {Quote(row[10].ToString())}.{Quote(row[11].ToString())} ORDER BY {Quote(row[13].ToString())}";
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = fquery;
                        DatabaseProvider.ReadValuesAsText(command);
                        var index = Config.ToList().IndexOf(row);
                        connection.Open();
                        using (var reader = command.ExecuteReader())
                        {
                            var ftable = DatabaseProvider.LoadValues(reader, DatabaseType);
                            ForeignKeys.Add(index, ftable);
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
            if (updated.Keys.Any(key => !Config.Any(c => c[3].ToString() == key && !IsGenerated(c))))
            {
                throw new ArgumentException("Updates must reference editable columns in this table.", nameof(updated));
            }
            if (row == -1)
            {
                return Add(updated);
            }
            return updated.Count > 0 && Update(row, updated);
        }

        public bool IsGenerated(DataRow column)
        {
            return (!column.IsNull(14) && Convert.ToBoolean(column[14]))
                || (!column.IsNull(15) && Convert.ToBoolean(column[15]));
        }

        private string ValueExpression(string column, string parameter)
        {
            if (DatabaseType != DatabaseType.Postgres) return "@" + parameter;
            var config = Config.First(c => c[3].ToString() == column);
            // PostgreSQL does not implicitly convert text parameters to numeric, UUID,
            // timestamp, enum, etc. The SQL type comes from the database's own catalog,
            // without length modifiers (varchar(n) casts would silently truncate text).
            return $"CAST(@{parameter} AS {config["SQL_TYPE"]})";
        }

        private object EditValue(string column, string value)
        {
            var config = Config.First(c => c[3].ToString() == column);
            if (DatabaseType == DatabaseType.Postgres && string.IsNullOrEmpty(value)
                && config[5].ToString() == "YES") return DBNull.Value;
            return value;
        }

        private bool Add(Dictionary<string, string> updated)
        {
            var columns = string.Join(',', updated.Keys.Select(Quote));
            var values = string.Join(',', updated.Keys.Select((key, index) => ValueExpression(key, "value" + index)));
            var query = updated.Count == 0 ? $"INSERT INTO {QualifiedTable} DEFAULT VALUES"
                : $"INSERT INTO {QualifiedTable} ({columns}) VALUES({values})";
            using (var connection = DatabaseProvider.CreateConnection(DatabaseType, ConnectionString))
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    var index = 0;
                    foreach (var update in updated)
                    {
                        DatabaseProvider.AddParameter(command, "value" + index++, EditValue(update.Key, update.Value));
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

            var where = string.Join(" AND ", PrimaryKeys.Select((key, index) => $"{Quote(key)} = {ValueExpression(key, "key" + index)}"));
            var set = string.Join(", ", updated.Keys.Select((key, index) => $"{Quote(key)} = {ValueExpression(key, "update" + index)}"));
            var query = $"UPDATE {QualifiedTable} SET {set} WHERE {where}";
            using (var connection = DatabaseProvider.CreateConnection(DatabaseType, ConnectionString))
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    var index = 0;
                    foreach (var key in PrimaryKeys)
                    {
                        DatabaseProvider.AddParameter(command, "key" + index++, current[key]);
                    }
                    index = 0;
                    foreach (var update in updated)
                    {
                        DatabaseProvider.AddParameter(command, "update" + index++, EditValue(update.Key, update.Value));
                    }
                    connection.Open();
                    var result = command.ExecuteNonQuery();
                    connection.Close();

                    LoadData(_sortColumn, _sortAscending);
                    return result > 0;
                }
            }
        }

        internal bool DeleteRow(int row)
        {
            if (!PrimaryKeys.Any()) return false;
            var current = Data.Rows[row];

            var where = string.Join(" AND ", PrimaryKeys.Select((key, index) => $"{Quote(key)} = {ValueExpression(key, "key" + index)}"));
            var query = $"DELETE FROM {QualifiedTable} WHERE {where}";
            using (var connection = DatabaseProvider.CreateConnection(DatabaseType, ConnectionString))
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    var index = 0;
                    foreach (var key in PrimaryKeys)
                    {
                        DatabaseProvider.AddParameter(command, "key" + index++, current[key]);
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
