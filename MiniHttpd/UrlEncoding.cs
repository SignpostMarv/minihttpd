using System;
using System.Text;

namespace MiniHttpd
{
    /// <summary>
    /// Provides somewhat lenient URL encoding suited for Unicode Uris.
    /// </summary>
    public class UrlEncoding
    {
        private static readonly Decoder _decoder = new Decoder();
        private static readonly string[] _urlEncStrings = InitUrlStrings();

        private UrlEncoding()
        {
        }

        private static string[] InitUrlStrings()
        {
            string[] urlEncodingStrings = new string[256];
            for (int i = 0; i < 255; i++)
                urlEncodingStrings[i] = "%" + i.ToString("X2");
            return urlEncodingStrings;
        }

        /// <summary>
        /// Url encodes a string, but allow unicode characters to pass unencoded.
        /// </summary>
        /// <param name="value">The string to encode.</param>
        /// <returns>The Url encoded string.</returns>
        public static string Encode(string value)
        {
            if (value == null)
                return null;

            StringBuilder ret = new StringBuilder(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                char ch = value[i];
                if (ch == ' ')
                    ret.Append('+');
                else if (!IsSafe(ch))
                    ret.Append(_urlEncStrings[ch]);
                else
                    ret.Append(ch);
            }

            return ret.ToString();
        }

        /// <summary>
        /// Decodes a Url using System.Uri's Unescape method.
        /// </summary>
        /// <param name="value">The string to decode.</param>
        /// <returns>The Url decoded string.</returns>
        public static string Decode(string value)
        {
            if (value == null)
                return null;

            return _decoder.Decode(value);
        }

        private static bool IsSafe(char ch)
        {
            if (char.IsLetterOrDigit(ch))
                return true;

            switch (ch)
            {
                case '\'':
                case '(':
                case ')':
                case '[':
                case ']':
                case '*':
                case '-':
                case '.':
                case '!':
                case '_':
                    return true;
            }

            if (ch > 255)
                return true;

            return false;
        }

        #region Nested type: Decoder

        /// <summary>
        /// Provides a somewhat hackish alternative to System.Web.HttpUtility.UrlDecode by exposing System.Uri's protected Unescape method
        /// </summary>
        private class Decoder : Uri
        {
            public Decoder() : base("http://localhost")
            {
            }

            public string Decode(string str)
            {
                //was: Unscape
                return UnescapeDataString(str.Replace("+", "%20"));
            }
        }

        #endregion
    }
}