using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseEdit.Pages
{
   public class IndexModel : PageModel
   {
      public IndexModel(DatabaseConfig config)
      {
         this.Config = config;
      }

      public DatabaseConfig Config { get; }

      public void OnGet()
      {

      }
      public IActionResult OnPostGotoEdit()
      {
         Config.SetConnectionString(Request.Form["connectionstring"]);
         return RedirectToPage("DatabaseEdit");
      }
   }
}
