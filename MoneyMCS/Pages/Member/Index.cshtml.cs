using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MoneyMCS.Pages.Member
{

    [Authorize(Policy = "MemberAccessPolicy")]
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
