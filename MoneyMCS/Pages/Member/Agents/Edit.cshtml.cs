using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MoneyMCS.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;

namespace MoneyMCS.Pages.Member.Agents
{
    [Authorize(Policy = "MemberAccessPolicy")]
    public class EditModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<EditModel> _logger;

        public EditModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            ILogger<EditModel> logger)
        {
            _userManager = userManager;
            _userStore = userStore;
            _logger = logger;
            _emailStore = GetEmailStore();
        }

        public List<SelectListItem> SelectAgents = new List<SelectListItem>();

        public string ReturnUrl { get; set; }

        public ApplicationUser ToEditAgent { get; set; }

        public class ProfileInput
        {

            [Required]
            [Display(Name = "Username")]
            public string UserName { get; set; }

            [Required]
            [Display(Name = "First Name")]
            public string FirstName { get; set; }

            [Display(Name = "Last Name")]
            public string LastName { get; set; } = string.Empty;

            [Phone]
            [Display(Name = "Phone Number")]
            public string? PhoneNumber { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            public string? returnUrl { get; set; }
        }

        public class PasswordChangeInput
        {
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }


            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

        }

        public class AgentTypeInput
        {

            [Required]
            public string ReferrerId { get; set; }
        }

        public AgentTypeInput AgentInput { get; set; }
        public PasswordChangeInput PasswordInput { get; set; }

        public async Task<IActionResult> OnGet([FromRoute] string? id)
        {
            if (id == null)
            {
                return RedirectToPage("/Member/Agents");
            }

            ToEditAgent = await _userManager.FindByIdAsync(id);
            if (ToEditAgent == null)
            {
                return NotFound();
            }

            await _userManager.Users.ForEachAsync(agent =>
            {
                if (agent.Id != ToEditAgent.Id)
                {
                    SelectAgents.Add(new SelectListItem()
                    {
                        Text = agent.UserName,
                        Value = agent.Id,
                        Selected = ToEditAgent.ReferrerId != null && ToEditAgent.ReferrerId != null && ToEditAgent.ReferrerId == agent.Id
                    });
                }

            });
            return Page();
        }
        public async Task<IActionResult> OnPostProfile([FromRoute] string? id, ProfileInput Input)
        {
            if (id == null)
            {
                return BadRequest();
            }

            ToEditAgent = await _userManager.FindByIdAsync(id);
            if (ToEditAgent == null)
            {
                return NotFound($"User with the id: {id} is not found.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }



            ToEditAgent.UserName = Input.UserName;
            ToEditAgent.FirstName = Input.FirstName;
            ToEditAgent.LastName = Input.LastName;
            ToEditAgent.PhoneNumber = Input.PhoneNumber;
            ToEditAgent.Email = Input.Email;

            await _userManager.UpdateAsync(ToEditAgent);

            Input.returnUrl ??= Url.Content($"~/Member/EditAgent/{id}");
            return Redirect(Input.returnUrl);

        }
        //Continue change password
        //public async Task<IActionResult> OnPostChangePassword([FromRoute] string? Id, PasswordChangeInput Input)
        //{
        //    if (Id == null)
        //    {
        //        return BadRequest();
        //    }
        //    if (!ModelState.IsValid)
        //    {
        //        return Page();
        //    }

        //    ToEditAgent = await _userManager.FindByIdAsync(Id);

        //    if (ToEditAgent == null)
        //    {
        //        return NotFound();
        //    }

        //    var token = await _userManager.GeneratePasswordResetTokenAsync(ToEditAgent);
        //}

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }


    }
}
