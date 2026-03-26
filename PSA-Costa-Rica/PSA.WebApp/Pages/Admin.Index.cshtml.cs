using Microsoft.AspNetCore.Mvc.RazorPages;
using PSA.WebApp.Filters;

namespace PSA.WebApp.Pages.Admin
{
    [RoleAuthorize("ADMIN")]
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}