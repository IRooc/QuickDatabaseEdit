using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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
        public int Row { get; set; }


        public TableResult TableConfig { get; set; }
        public IEnumerable<string> Tables { get; set; }

        public string Message { get; set; }

        public DatabaseEditModel(DatabaseConfig config)
        {
            this.Config = config;
        }


        public void OnGet()
        {
            Init();
        }

        public JsonResult OnGetGuid()
        {
            return new JsonResult(Guid.NewGuid());
        }

        public void OnPost()
        {
            Init();

            var row = Row == -1 ? TableConfig.Data.NewRow() : TableConfig.Data.Rows[Row];
            var updated = new Dictionary<string, string>();
            foreach (var config in TableConfig.Config)
            {
                var key = config[3].ToString();
                var oldValue = row[key];
                var newValue = Request.Form[key].ToString();
                if (oldValue.ToString()?.Replace("\r\n","\n") != newValue?.Replace("\r\n", "\n"))
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
        }

        private void Init()
        {
            ViewData["Title"] = Table ?? "Tables";

            if (!string.IsNullOrEmpty(Table))
            {
                TableConfig = Config.GetTable(Table);
                if (TableConfig == null)
                {
                    View = "tables";
                }
            }
        }
    }
}
