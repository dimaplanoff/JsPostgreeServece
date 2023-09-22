using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SLLibrary.JsonModel.Converters
{

    public class JsonSpecialTypeDateTimeConverter : JsonConverter<DateTime>
    {
        private string DateFormat;


        public JsonSpecialTypeDateTimeConverter(string dateFormat)
        {
            DateFormat = dateFormat;
        }

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {

            string str = reader.GetString();
            str = str.ToUpper().Trim();
            int index = str.IndexOf(' ');
            if (index == -1)
                index = str.IndexOf('T');

            var ns = str;
            if (index != -1)
                ns = str.Substring(0, index);

            var regs = ns.Split(new char[] { '-', '.', ':', '/' });

            if (regs[0].Length == 4)
                Array.Reverse(regs);

            DateTime result = DateTime.Parse(string.Join("-", regs));

            if (index == -1)
                return result;

            ns = str.Substring(index);

            regs = ns.Split(new char[] { '+', '-' });

            var times = regs[0].Split(new char[] { ':', '.' }).Select(m => int.Parse(Regex.Replace(m, @"[^0-9]", ""))).ToArray();
            for (int i = 0; i < times.Length; i++)
            {
                switch (i)
                {
                    case 0:
                        result = result.AddHours(times[i]);
                        break;
                    case 1:
                        result = result.AddMinutes(times[i]);
                        break;
                    case 2:
                        result = result.AddSeconds(times[i]);
                        break;
                    case 3:
                        result = result.AddMilliseconds(times[i]);
                        break;
                    default:
                        break;
                }
            }

            if (regs.Length == 2 && int.TryParse(Regex.Replace(regs[1], @"[^0-9]", ""), out int gmt))
                if (ns.Contains('-'))
                    result = result.AddHours(-gmt);
                else
                    result = result.AddHours(gmt);

            return result;
            //if (DateTime.TryParseExact(str, DateFormat, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeLocal, out DateTime dateTime))
            //        return dateTime.ToLocalTime();
            //    else
            //        return DateTime.ParseExact(str, ShortDateFormat, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeLocal).ToLocalTime();
        }


        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToString(DateFormat));
    }
}
