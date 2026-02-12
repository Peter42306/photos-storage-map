using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace PhotosStorageMap.Application.Common.Encoding
{
    public static class TokenEncoding
    {
        public static string Encode(string token)
        {
            return WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(token));
        }

        public static string Decode(string encodedToken)
        {
            return System.Text.Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));
        }
    }
}
