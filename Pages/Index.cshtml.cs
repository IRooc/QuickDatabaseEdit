using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;

namespace DatabaseEdit.Pages
{
   public class IndexModel : PageModel
   {
      public IndexModel(DatabaseConfig config)
      {
         this.Config = config;
      }

      public DatabaseConfig Config { get; }

      [BindProperty]
      [Required(ErrorMessage = "Enter a connection string.")]
      public string ConnectionString { get; set; }

      [BindProperty]
      public DatabaseType DatabaseType { get; set; } = DatabaseType.MSSQL;

      public void OnGet()
      {
         ConnectionString = Config.ConnectionString;
         DatabaseType = Config.DatabaseType;
      }
      public IActionResult OnPostGotoEdit()
      {
         if (!Enum.IsDefined(DatabaseType))
         {
            ModelState.AddModelError(nameof(DatabaseType), "Select MSSQL or Postgres.");
         }
         if (!ModelState.IsValid) return Page();

         try
         {
            Config.SetConnectionString(ConnectionString, DatabaseType);
         }
         catch (Exception exception) when (exception is DbException || exception is ArgumentException)
         {
            ModelState.AddModelError(string.Empty, "Unable to connect: " + exception.Message);
            return Page();
         }
         return RedirectToPage("DatabaseEdit");
      }
   }
}
