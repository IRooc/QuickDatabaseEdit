# Quick editor for SQL Server and PostgreSQL databases

Only works on smaller databases because foreign keys will be loaded in dropdowns

## Settings

Select **MSSQL** (the default) or **Postgres** on the first page and enter a connection string. The table browser, row editor, foreign-key dropdowns, sorting and CSV download work the same way for both databases.

SQL Server example:

```text
Server=localhost;Database=mydatabase;Integrated Security=True;TrustServerCertificate=True
```

PostgreSQL example:

```text
Host=localhost;Port=5432;Database=mydatabase;Username=postgres;Password=yourpassword
```

Saved connections remember the database type. Existing saved connections are treated as MSSQL.

Identity/serial and computed/generated columns are supplied by the database, and blank fields with defaults are omitted when adding a row. PostgreSQL nullable fields can be cleared by leaving them blank. Rows need a primary key to be added, updated or deleted; composite primary keys are supported. PostgreSQL composite foreign keys are edited as individual fields rather than dropdowns.


## Note

This is a demo project and should not be used in production environments.

Saved connection strings (including passwords) are stored in the browser's local storage, as in the original setup.
