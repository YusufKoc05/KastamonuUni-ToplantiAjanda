using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using KastamonuMeetingRoom.Models;

namespace KastamonuMeetingRoom.Areas.Identity.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;

        public LogoutModel(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
        }

        public async Task<IActionResult> OnPost()
        {
            await _signInManager.SignOutAsync();

            // ✅ ÇIKIŞTAN SONRA LOGOUT SAYFASINI GÖSTER
            return RedirectToPage("/Account/Logout");
        }
    }
}
