using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using KastamonuMeetingRoom.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using KastamonuMeetingRoom.Helpers;
using Microsoft.AspNetCore.Http;

namespace KastamonuMeetingRoom.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly ReCaptchaService _captcha;

        public LoginModel(SignInManager<ApplicationUser> signInManager,
                          ILogger<LoginModel> logger,
                          ReCaptchaService captcha)
        {
            _signInManager = signInManager;
            _logger = logger;
            _captcha = captcha;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }
        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required, EmailAddress]
            public string Email { get; set; }

            [Required, DataType(DataType.Password)]
            public string Password { get; set; }

            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
                ModelState.AddModelError("", ErrorMessage);

            returnUrl ??= Url.Content("~/");

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager
                .GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;

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
                var result = await _signInManager.PasswordSignInAsync(
                    Input.Email, Input.Password, Input.RememberMe, false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Kullanıcı giriş yaptı.");
                    return LocalRedirect(returnUrl);
                }

                ModelState.AddModelError("", "Geçersiz giriş denemesi.");
            }

            return Page();
        }
    }
}






