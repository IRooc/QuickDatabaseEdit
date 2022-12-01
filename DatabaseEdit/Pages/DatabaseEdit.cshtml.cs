using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

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
                var type = config[6].ToString();
                var key = config[3].ToString();
                var oldValue = row[key];
                if (type == "datetime" || type == "datetime2")
                {
                    oldValue = GetDateTimeString(oldValue);
                }
                var newValue = Request.Form[key].ToString();
                if (oldValue.ToString()?.Replace("\r\n", "\n") != newValue?.Replace("\r\n", "\n"))
                {
                    updated.Add(key, newValue);
                }
            }
            if (updated.Count == 0)
            {
                this.Message = "Nothing changed";
            }
            else
            {
                this.Message = "Saved<br/>";
                bool success = TableConfig.UpdateRow(Row, updated);
                if (!success) this.Message += "UPDATE FAILED!!!!!!!!!!!!!!!!!!"; //https://wiki.lspace.org/mediawiki/index.php/Multiple_exclamation_marks
                this.Message += string.Join("<br/>", updated.Select(k => { return k.Key + ": " + k.Value; }).ToArray());
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

        public async Task<ObjectResult> OnGetDownloadCSV()
        {
            Init();
            var builder = new StringBuilder();
            var started = false;
            foreach (DataColumn col in TableConfig.Data.Columns)
            {
                if (started)
                {
                    builder.Append(',');
                }
                started = true;
                builder.Append(col.ColumnName);
            }
            builder.Append('\n');

            for (int r = 0; r < TableConfig.Data.Rows.Count; r++)
            {
                var row = TableConfig.Data.Rows[r];
                for (int i = 0; i < row.ItemArray.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(',');
                    }
                    var item = row.ItemArray[i];
                    if (item is string ss && ss.Contains("\n"))
                    {
                        ss = ss.Replace("\n", "\t"); //todo what todo?
                        if (ss.Contains(","))
                        {
                            if (ss.Contains("\""))
                            {
                                ss = ss.Replace('"', '\'');
                            }
                            ss = $"\"{ss}\"";
                        }
                        item = ss;
                    }
                    builder.Append(item);
                }
                builder.Append('\n');
            }

            return new ObjectResult(builder.ToString());
        }

        public string GetDateTimeString(object value)
        {
            if (value == null) return string.Empty;
            if (value == DBNull.Value) return string.Empty;
            var date = (DateTime)value;
            return date.ToString("yyyy-MM-dd'T'HH:mm:ssZ");
        }

        public string GetQuery(string view)
        {
            var query = Request.QueryString.ToString();
            return Regex.Replace(query, "view=[^&]+", "view=" + view);
        }

        private void Init()
        {
            ViewData["Title"] = Table ?? "Tables";

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
