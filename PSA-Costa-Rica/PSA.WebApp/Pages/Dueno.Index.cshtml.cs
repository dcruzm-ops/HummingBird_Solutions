using Microsoft.AspNetCore.Mvc.RazorPages;
using PSA.WebApp.Filters;

namespace PSA.WebApp.Pages.Dueno
{
    [RoleAuthorize("DUENO")]
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
