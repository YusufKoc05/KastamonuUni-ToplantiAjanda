using Microsoft.AspNetCore.Identity;

namespace KastamonuMeetingRoom.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
    }
}
