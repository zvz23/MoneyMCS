using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MoneyMCS.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace MoneyMCS.Pages.Member.Agents
{
    [Authorize(Policy = "MemberAccessPolicy")]
    public class AddModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EntitiesContext _context;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<AddModel> _logger;

        public AddModel(
            UserManager<ApplicationUser> userManager,
            EntitiesContext context,
            IUserStore<ApplicationUser> userStore,
            ILogger<AddModel> logger)
        {
            _userManager = userManager;
            _context = context;
            _userStore = userStore;
            _logger = logger;
            _emailStore = GetEmailStore();
        }

        [BindProperty]
        public InputModel Input { get; set; }


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
            await LoadFormDefaultData();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/Member/Agents/Index");
            if (ModelState.IsValid)
            {
                var user = CreateUser();
                user.FirstName = Input.FirstName;
                user.LastName = Input.LastName;
                user.CreationDate = DateTime.Now;
                user.UserType = UserType.AGENT;


                await _userStore.SetUserNameAsync(user, Input.UserName, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                if (Input.ReferrerId != null)
                {
                    ApplicationUser referrerUser = await _userManager.FindByIdAsync(Input.ReferrerId);
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

                List<Claim> claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Role, "Agent"),
                    new Claim("AgentId", user.Id),
                    new Claim("FullName", $"{user.FirstName} {user.LastName}"),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("Username", user.UserName)
                };

                IdentityResult result;

                while (true)

                {
                    user.ReferralCode = GenerateReferralCode(6);
                    try
                    {
                        user.Wallet = new Wallet();
                        result = await _userManager.CreateAsync(user, Input.Password);
                        break;
                    }
                    catch (DbUpdateException ex)
                    {
                        if (ex.InnerException != null)
                        {
                            var sqlException = (SqlException)ex.InnerException;
                            if (sqlException.Number == 2601)
                            {
                                continue;
                            }
                            return BadRequest("There was a problem in registring your info");
                        }
                    }

                }

                if (result.Succeeded)
                {

                    await _userManager.AddClaimsAsync(user, claims);
                    _logger.LogInformation("User created a new account with password.");
                    return RedirectToPage(returnUrl);
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            await LoadFormDefaultData();

            return Page();
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }

        private async Task LoadFormDefaultData()
        {
            await _userManager.Users
                .Where(au => au.UserType == UserType.AGENT)
                .ForEachAsync(agent =>
            {
                SelectAgents.Add(new SelectListItem()
                {
                    Text = agent.UserName,
                    Value = agent.Id
                });
            });
        }

        private string GenerateReferralCode(int sample)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var result = new string(
                Enumerable.Repeat(chars, sample)
                          .Select(s => s[random.Next(s.Length)])
                          .ToArray());
            return result;
        }




    }
}
