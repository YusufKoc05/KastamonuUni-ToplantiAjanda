using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using KastamonuMeetingRoom.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using KastamonuMeetingRoom.Helpers;
using Microsoft.AspNetCore.Http;

namespace KastamonuMeetingRoom.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly ReCaptchaService _captcha;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            ReCaptchaService captcha)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _captcha = captcha;
        }

        [BindProperty]
        public InputModel Input { get; set; }
        public string ReturnUrl { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required, Display(Name = "Ad Soyad")]
            public string FullName { get; set; }

            [Required, EmailAddress, Display(Name = "Email")]
            public string Email { get; set; }

            [Required, StringLength(100, MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Compare("Password"), DataType(DataType.Password)]
            public string ConfirmPassword { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;

            ExternalLogins = (await _signInManager
                .GetExternalAuthenticationSchemesAsync()).ToList();

            // ⭐ MANUEL CAPTCHA ÜRET
            var code = CaptchaGenerator.GenerateCode();
            HttpContext.Session.SetString("CaptchaCode", code);
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager
                .GetExternalAuthenticationSchemesAsync()).ToList();

            // ⭐ GOOGLE reCAPTCHA
            var captchaToken = Request.Form["g-recaptcha-response"];
            if (!await _captcha.Verify(captchaToken))
            {
                ModelState.AddModelError("", "Lütfen robot olmadığınızı doğrulayın.");
                return Page();
            }

            // ⭐ MANUEL CAPTCHA
            var userCaptcha = Request.Form["manual-captcha"];
            var realCaptcha = HttpContext.Session.GetString("CaptchaCode");

            if (userCaptcha != realCaptcha)
            {
                ModelState.AddModelError("", "Güvenlik kodu hatalı.");
                return Page();
            }

            if (ModelState.IsValid)
            {
                var user = Activator.CreateInstance<ApplicationUser>();
                user.FullName = Input.FullName;

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Yeni kullanıcı oluşturuldu.");

                    await _signInManager.SignInAsync(user, false);
                    return LocalRedirect(returnUrl);
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
            }

            return Page();
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}


