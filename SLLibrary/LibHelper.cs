using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SLLibrary
{
    public static class LibHelper
    {
        private static readonly CultureInfo cultureInfo = new CultureInfo("en-US");
        private static readonly NumberStyles numberStyleSeparator = NumberStyles.Float;
        public static readonly JsonSerializerOptions JsonSerializerWeb = CreateSerializerOptions();
        public static string NewKey => $"{Guid.NewGuid()}{Guid.NewGuid()}".Replace("-", string.Empty);
        private static readonly Encoding utf8 = Encoding.UTF8;

        private static JsonSerializerOptions CreateSerializerOptions()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            options.Converters.Add(new JsonModel.Converters.JsonSpecialTypeDateTimeConverter("dd.MM.yyyy HH:mm"));
            options.Converters.Add(new JsonModel.Converters.JsonSpecialTypeStringConverter());
            
            options.WriteIndented = true;
            options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

            return options;
        }

        public static T ChangeStringType<T>(string str)
        {
            return (T)ChangeStringType(str, typeof(T));
        }

        public static object ChangeStringType(string str, Type type)
        {

            if (type == typeof(string))
            {
                return str;
            }
            else if (type == typeof(int))
            {
                if (int.TryParse(str, out int newint))
                    return newint;
            }
            else if (type == typeof(long))
            {
                if (long.TryParse(str, out long newlong))
                    return newlong;
            }
            else if (type == typeof(decimal))
            {
                if (str.TryParseValue(out decimal newdec))
                    return newdec;
            }
            else if (type == typeof(float))
            {
                if (str.TryParseValue(out float newfloat))
                    return newfloat;
            }
            else if (type == typeof(DateTime))
            {
                if (DateTime.TryParse(str, out DateTime newdt))
                    return newdt;
            }

            return null;
        }


        public static string Crypt(string str)
        {
            using (var md5 = MD5.Create())
                return Convert.ToBase64String(md5.ComputeHash(Encoding.UTF8.GetBytes(str)));
        }


        public static string GetUtf8String(Stream input)
        {
            using (var reader = new StreamReader(input, true))
            {
                var result = reader.ReadToEnd();
                if (string.IsNullOrEmpty(result))
                    return string.Empty;
                var encoding = reader.CurrentEncoding;
                if (encoding != utf8)
                    return utf8.GetString(encoding.GetBytes(result));
                return result;
            }
        }

        public static string StrNorm(string val, Encoding encoding = null)
        {
            val = Regex.Replace(val ?? string.Empty, @"(?i)\\[uU]([0-9a-f]{4})", delegate (Match m) { return ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString(); });

            if ((encoding ?? utf8) != utf8)
                val = utf8.GetString(Encoding.Convert(encoding, utf8, encoding.GetBytes(val)));
            
            return val;
        }


        public static bool ContainsIgnoreCase(this IEnumerable<string> source, string value)
        {
            foreach (var str in source)
                if(str.Equals(value, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }


        public static bool TryParseValue(this string val, out decimal outVal)
        {
            return decimal.TryParse(val, numberStyleSeparator, cultureInfo, out outVal);
        }

        public static bool TryParseValue(this string val, out float outVal)
        {
            return float.TryParse(val, numberStyleSeparator, cultureInfo, out outVal);
        }

        public static bool TryParseValue(this string val, out double outVal)
        {
            return double.TryParse(val, numberStyleSeparator, cultureInfo, out outVal);
        }

        public static bool TryParseValue(this string val, out long outVal)
        {
            return long.TryParse(val, numberStyleSeparator, cultureInfo, out outVal);
        }

        public static bool TryParseValue(this string val, out int outVal)
        {
            return int.TryParse(val, numberStyleSeparator, cultureInfo, out outVal);
        }
    }
}
