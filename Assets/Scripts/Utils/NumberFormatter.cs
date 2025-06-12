using UnityEngine;

namespace Utils
{
    public static class NumberFormatter
    {
        public static string FormatRoundedAbbreviation(float value, int decimalPlaces = 0)
        {
            // Define a format string like "0.##" or "0.000"
            string format = "0" + (decimalPlaces > 0 ? "." + new string('#', decimalPlaces) : "");

            float roundedValue = Mathf.Round(value);
            if (roundedValue >= 1_000_000_000_000)
                return $"{(roundedValue / 1_000_000_000_000).ToString(format)}T";
            else if (roundedValue >= 1_000_000_000)
                return $"{(roundedValue / 1_000_000_000).ToString(format)}B";
            else if (roundedValue >= 1_000_000)
                return $"{(roundedValue / 1_000_000).ToString(format)}M";
            else if (roundedValue >= 1_000)
                return $"{(roundedValue / 1_000).ToString(format)}K";
            else
                return roundedValue.ToString(format);
        }
    }
}