using Microsoft.AspNetCore.Mvc.RazorPages;
using PSA.WebApp.Filters;

namespace PSA.WebApp.Pages.Ingeniero
{
    [RoleAuthorize("INGENIERO")]
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}