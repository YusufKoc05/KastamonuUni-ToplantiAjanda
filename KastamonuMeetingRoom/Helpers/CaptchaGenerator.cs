using System;
using System.Linq;

namespace KastamonuMeetingRoom.Helpers
{
    public static class CaptchaGenerator
    {
        public static string GenerateCode(int length = 5)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var random = new Random();

            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)])
                .ToArray());
        }
    }
}

