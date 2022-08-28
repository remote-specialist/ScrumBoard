using System.Security.Cryptography;
using System.Text;

namespace Extensions
{
    public static class StringExtension
    {
        public static string GetColor(this string str)
        {
            if (str == "ivan.filippov")
            {
                return "#ff8000";
            }
            if (str == "marina.skiba")
            {
                return "#B2EBF2";
            }
            if (str == "evgeny.shvets")
            {
                return "#0033cc";
            }
            
            using MD5 md5 = MD5.Create();
            return $"#{BitConverter.ToString(md5.ComputeHash(Encoding.ASCII.GetBytes(str))).Replace("-", string.Empty).ToLowerInvariant()[0..6]}";
        }

        public static string FillTemplate(this string template, string data)
        {
            return template.Replace("INSERTDATAHERE", data);
        }
    }
}
