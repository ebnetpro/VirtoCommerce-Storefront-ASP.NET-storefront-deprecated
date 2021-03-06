using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DotLiquid;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace VirtoCommerce.LiquidThemeEngine.Filters
{
    public class MathFilters
    {
        public static object Round(object input, int digits = 0)
        {
            if (input != null)
            {
                input =  Math.Round(Convert.ToDouble(input, CultureInfo.InvariantCulture), digits);
            }
            return input;
        }

        public static object Ceil(object input)
        {
            if (input != null)
            {
                input = Math.Ceiling(Convert.ToDouble(input, CultureInfo.InvariantCulture));
            }
            return input;
        }

        public static object Floor(object input)
        {
            if (input != null)
            {
                input = Math.Floor(Convert.ToDouble(input, CultureInfo.InvariantCulture));
            }
            return input;
        }

        public static object Abs(object input)
        {
            if (input != null)
            {
                input = Math.Abs(Convert.ToDouble(input, CultureInfo.InvariantCulture));
            }
            return input;
        }

        public static string Format(object input, string format)
        {
            if (input == null)
                return null;
            else if (string.IsNullOrWhiteSpace(format))
                return input.ToString();

            return string.Format("{0:" + format + "}", input);
        }

    }
}
