using System;
using System.Diagnostics;
using System.Globalization;
namespace NuGet.Services /* Namespace changed from original to avoid requring using statements */
{
    internal static class Guard
    {
        [DebuggerNonUserCode]
        public static void NotNullOrEmpty(string value, string parameterName)
        {
            if (String.IsNullOrEmpty(value))
            {
                throw new ArgumentException(
                    GuardStrings.StringNotNullOrEmpty,
                    parameterName);
            }
        }

        [DebuggerNonUserCode]
        public static void NotNull(object value, string parameterName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }
        
        [DebuggerNonUserCode]
        public static void NonNegative(int value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    GuardStrings.IntegerMustBeNonNegative,
                    parameterName);
            }
        }
    }
}