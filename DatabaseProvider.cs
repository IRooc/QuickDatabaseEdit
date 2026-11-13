using Microsoft.Data.SqlClient;
using Npgsql;
using System;
using System.Data;
using System.Data.Common;

namespace DatabaseEdit
{
    public enum DatabaseType
    {
        MSSQL,
        Postgres
    }

    internal static class DatabaseProvider
    {
        public static DbConnection CreateConnection(DatabaseType databaseType, string connectionString)
        {
            return databaseType switch
            {
                DatabaseType.MSSQL => new SqlConnection(connectionString),
                DatabaseType.Postgres => new NpgsqlConnection(connectionString),
                _ => throw new ArgumentOutOfRangeException(nameof(databaseType))
            };
        }

        public static string GetServerName(DatabaseType databaseType, string connectionString)
        {
            if (databaseType == DatabaseType.Postgres)
            {
                var builder = new NpgsqlConnectionStringBuilder(connectionString);
                return $"{builder.Host}:{builder.Port}";
            }

            var server = new SqlConnectionStringBuilder(connectionString).DataSource;
            return server.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase) ? server.Substring(4) : server;
        }

        public static string QuoteIdentifier(DatabaseType databaseType, string identifier)
        {
            return databaseType == DatabaseType.Postgres
                ? "\"" + identifier.Replace("\"", "\"\"") + "\""
                : "[" + identifier.Replace("]", "]]") + "]";
        }

        public static void AddParameter(DbCommand command, string name, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        public static void ReadValuesAsText(DbCommand command)
        {
            // Keep PostgreSQL's editable text representation, including arrays, JSON and enums.
            // Metadata is still read with its native types.
            if (command is NpgsqlCommand postgresCommand)
            {
                postgresCommand.AllResultTypesAreUnknown = true;
            }
        }

        public static DataTable LoadValues(DbDataReader reader, DatabaseType databaseType)
        {
            var table = new DataTable();
            if (databaseType == DatabaseType.MSSQL)
            {
                table.Load(reader);
                return table;
            }

            // Npgsql's schema still reports the original SQL types when text results
            // are requested, so DataTable.Load would try to convert e.g. 't' to bool.
            for (var i = 0; i < reader.FieldCount; i++) table.Columns.Add(reader.GetName(i), typeof(string));
            while (reader.Read())
            {
                var values = new object[reader.FieldCount];
                reader.GetValues(values);
                table.Rows.Add(values);
            }
            return table;
        }
    }
}
