using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MoneyMCS.Areas.Identity.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MoneyMCS.Pages.Member
{
    public class AddAgentModel : PageModel
    {
        private readonly UserManager<AgentUser> _userManager;
        private readonly IUserStore<AgentUser> _userStore;
        private readonly IUserEmailStore<AgentUser> _emailStore;
        private readonly ILogger<AddAgentModel> _logger;

        public AddAgentModel(
            UserManager<AgentUser> userManager,
            IUserStore<AgentUser> userStore,
            ILogger<AddAgentModel> logger)
        {
            _userManager = userManager;
            _userStore = userStore;
            _logger = logger;
            _emailStore = GetEmailStore();
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public List<SelectListItem> SelectAgentType = new List<SelectListItem>() {
            new SelectListItem() { Value = "BASIC", Text = "BASIC" , Selected = true},
            new SelectListItem() { Value = "VIP", Text = "VIP"},
            new SelectListItem() { Value = "DIY", Text = "DIY" },
        };

        public List<SelectListItem> SelectAgents = new List<SelectListItem>();

        public string ReturnUrl { get; set; }

        public class InputModel
        {


            [Required]
            [Display(Name = "Username")]
            public string UserName { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [Display(Name = "First Name")]
            public string FirstName { get; set; }

            [Required]
            [Display(Name = "Last Name")]
            public string LastName { get; set; }

            [Phone]
            
            [Display(Name = "PhoneNumber")]
            public string? PhoneNumber { get; set; }

            [Display(Name = "Referer")]
            public string? ReferrerId { get; set; }

            [Required]
            [Display(Name = "Agent Type")]
            public string AgentType { get; set; }



            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }


            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }


        }
        public async Task<IActionResult> OnGet()
        {
            await _userManager.Users.ForEachAsync(agent =>
            {
                SelectAgents.Add(new SelectListItem()
                {
                    Text = agent.UserName,
                    Value = agent.Id
                });
            });
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/Member/Agents");
            if (ModelState.IsValid)
            {
                var user = CreateUser();
                user.UserName = Input.UserName;
                user.FirstName = Input.FirstName;
                user.LastName = Input.LastName;
                user.AgentType = Input.AgentType;
                if (Input.ReferrerId != null)
                {
                    AgentUser referrerUser = await _userManager.FindByIdAsync(Input.ReferrerId);
                    if (referrerUser == null)
                    {
                        return NotFound($"Agent with id: {Input.ReferrerId}");
                    }
                    user.Referrer = referrerUser;

                }
                if (Input.PhoneNumber != null)
                {
                    user.PhoneNumber = Input.PhoneNumber;
                }
                
                await _userStore.SetUserNameAsync(user, Input.UserName, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    var userId = await _userManager.GetUserIdAsync(user);
                    
                    return RedirectToPage(returnUrl);
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        private AgentUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<AgentUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(AgentUser)}'. " +
                    $"Ensure that '{nameof(AgentUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<AgentUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<AgentUser>)_userStore;
        }




    }
}
