using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MailLib
{
    internal static class Utils
    {
        public static string DecodeEncodedLine(string text)
        {
            Regex regex = new Regex(@"\s*=\?(?<charset>.*?)\?(?<encoding>[qQbB])\?(?<value>.*?)\?=");
            string encoded = text;
            string decoded = string.Empty;

            while (encoded.Length > 0)
            {
                Match match = regex.Match(encoded);

                if (match.Success)
                {
                    decoded += encoded.Substring(0, match.Index);
                    string charset = match.Groups["charset"].Value;
                    string encoding = match.Groups["encoding"].Value.ToUpper();
                    string value = match.Groups["value"].Value;

                    if (encoding.Equals("B"))
                    {
                        var bytes = Convert.FromBase64String(value);
                        decoded += Encoding.GetEncoding(charset).GetString(bytes);
                    }
                    else if (encoding.Equals("Q"))
                    {

                        Regex reg = new Regex(@"(\=(?<byte>[0-9A-F][0-9A-F]))+", RegexOptions.IgnoreCase);
                        decoded += reg.Replace(value, new MatchEvaluator(m =>
                        {
                            byte[] bytes = m.Groups["byte"].Captures.Cast<Capture>().Select(c => (byte)Convert.ToInt32(c.Value, 16)).ToArray();
                            return Encoding.GetEncoding(charset).GetString(bytes);
                        })).Replace('_', ' ');
                    }
                    else
                    {
                        decoded += encoded;
                        break;
                    }

                    encoded = encoded.Substring(match.Index + match.Length);
                }
                else
                {
                    decoded += encoded;
                    break;
                }
            }
            return decoded;
        }
        public static string UTF7Decode(string text)
        {
            StringReader reader = new StringReader(text);
            StringBuilder builder = new StringBuilder();
            while (reader.Peek() != -1)
            {
                char c = (char)reader.Read();
                if (c == '&' && reader.Peek() != '-')
                {
                    // The character sequence needs to be decoded.
                    StringBuilder sequence = new StringBuilder();
                    while (reader.Peek() != -1)
                    {
                        if ((c = (char)reader.Read()) == '-')
                            break;
                        sequence.Append(c);
                    }
                    string encoded = sequence.ToString().Replace(',', '/');
                    int pad = encoded.Length % 4;
                    if (pad > 0)
                        encoded = encoded.PadRight(encoded.Length + (4 - pad), '=');
                    try
                    {
                        byte[] buffer = Convert.FromBase64String(encoded);
                        builder.Append(Encoding.BigEndianUnicode.GetString(buffer));
                    }
                    catch (Exception e)
                    {
                        //   throw new FormatException("The input string is not in the correct Format.", e);
                    }
                }
                else
                {
                    if (c == '&' && reader.Peek() == '-')
                        reader.Read();
                    builder.Append(c);
                }
            }
            return builder.ToString();
        }
        public static string Base64Encode(string toEncode)
        {
            byte[] toEncodeAsBytes = Encoding.ASCII.GetBytes(toEncode);
            return Convert.ToBase64String(toEncodeAsBytes);
        }
    }
}
