using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DatabaseEdit.Pages
{
    public class DatabaseEditModel : PageModel
    {
        public readonly DatabaseConfig Config;

        [BindProperty(SupportsGet = true)]
        public string View { get; set; } = "tables";

        [BindProperty(SupportsGet = true)]
        public string Table { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortColumn { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortDirection { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Row { get; set; }


        public TableResult TableConfig { get; set; }
        public IEnumerable<string> Tables { get; set; }
        public string Message { get; set; }

        public DatabaseEditModel(DatabaseConfig config)
        {
            this.Config = config;
        }

        public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            Init();
            if (Config.ConfigTable == null) context.Result = RedirectToPage("Index");
        }


        public void OnGet()
        {
            this.Message = (string)TempData["Message"];
        }

        public JsonResult OnGetGuid()
        {
            return new JsonResult(Guid.NewGuid());
        }

        public IActionResult OnPost()
        {
            var row = Row == -1 ? TableConfig.Data.NewRow() : TableConfig.Data.Rows[Row];
            var updated = new Dictionary<string, string>();
            foreach (var config in TableConfig.Config)
            {
                var key = config[3].ToString();
                if (TableConfig.IsGenerated(config) || (Row >= 0 && TableConfig.PrimaryKeys.Contains(key))) continue;
                var oldValue = GetEditValue(config, row[key]);
                var newValue = Request.Form[key].ToString();
                // Leave blank defaulted fields out of INSERT so the database supplies them.
                if (Row == -1 && string.IsNullOrEmpty(newValue) && !config.IsNull(4)) continue;
                if (oldValue.Replace("\r\n", "\n") != newValue.Replace("\r\n", "\n")
                    || Row == -1 && config[5].ToString() == "NO")
                {
                    updated.Add(key, newValue);
                }
            }
            if (updated.Count == 0 && Row != -1)
            {
                this.Message = "Nothing changed";
            }
            else
            {
                this.Message = "Saved<br/>";
                bool success = TableConfig.UpdateRow(Row, updated);
                if (!success) this.Message += "UPDATE FAILED!!!!!!!!!!!!!!!!!!"; //https://wiki.lspace.org/mediawiki/index.php/Multiple_exclamation_marks
                this.Message += string.Join("<br/>", updated.Select(k => System.Net.WebUtility.HtmlEncode(k.Key + ": " + k.Value)).ToArray());
            }
            TempData["Message"] = this.Message;
            return RedirectToPage(new { view = "table", table = Table });
        }
        public IActionResult OnPostDelete()
        {
            this.View = "table";
            if (TableConfig.DeleteRow(Row))
            {
                this.Message = "Deleted Row " + this.Row;
            }
            else
            {
                this.Message = "Failed to deleted row " + this.Row;

            }
            TempData["Message"] = this.Message;
            return RedirectToPage(new { view = "table", table = Table });
        }

        public ObjectResult OnGetDownloadCSV()
        {
            Init();
            var builder = new StringBuilder();
            builder.AppendLine(string.Join(',', TableConfig.Data.Columns.Cast<DataColumn>().Select(c => EscapeCsv(c.ColumnName))));
            foreach (DataRow row in TableConfig.Data.Rows)
            {
                builder.AppendLine(string.Join(',', row.ItemArray.Select(value => EscapeCsv(value.ToString()))));
            }
            return new ObjectResult(builder.ToString());
        }

        private static string EscapeCsv(string value)
        {
            return value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0
                ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
        }

        public string GetDateTimeString(object value)
        {
            if (value == null) return string.Empty;
            if (value == DBNull.Value) return string.Empty;
            var date = (DateTime)value;
            return date.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture);
        }

        public bool IsDateTimeType(string type)
        {
            return type == "datetime" || type == "datetime2" || type == "date"
                || Config.DatabaseType == DatabaseType.Postgres && type.StartsWith("timestamp");
        }

        public string GetEditValue(DataRow column, object value)
        {
            if (value == null || value == DBNull.Value) return string.Empty;
            var type = column[6].ToString();
            if (Config.DatabaseType == DatabaseType.Postgres)
            {
                if (type == "boolean") return value.ToString() == "t" ? "True" : "False";
                if (IsDateTimeType(type)) return value.ToString().Replace(' ', 'T');
            }
            else if (IsDateTimeType(type))
            {
                return type == "date" ? ((DateTime)value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : GetDateTimeString(value);
            }
            return value.ToString();
        }

        public string GetQuery(string view)
        {
            var query = Request.QueryString.ToString();
            return Regex.Replace(query, "view=[^&]+", "view=" + view);
        }

        private void Init()
        {
            ViewData["Title"] = $"{Table ?? "Tables"} - {Config.ServerName}";

            if (!string.IsNullOrEmpty(Table))
            {
                TableConfig = Config.GetTable(Table, SortColumn, SortDirection?.Equals("asc", StringComparison.InvariantCultureIgnoreCase) ?? true);
                if (TableConfig == null)
                {
                    View = "tables";
                }
            }
        }
    }
}
