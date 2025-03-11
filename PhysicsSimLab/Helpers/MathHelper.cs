using System;
using System.Globalization;

namespace PhysicsSimLab.Helpers
{
    public static class MathHelper
    {
        public static bool TryParseInvariant(string text, out double result)
        {
            string normalizedText = text.Replace(',', '.');
            
            return double.TryParse(normalizedText, 
                                  NumberStyles.Any, 
                                  CultureInfo.InvariantCulture, 
                                  out result);
        }
        
        public static string FormatInvariant(double value, string format)
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}
