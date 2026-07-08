using System.Globalization;
using System.Text;

namespace FellrnrHeatMonitor.Services;

internal static class MacAddressHelper
{
    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var chars = input.Where(Uri.IsHexDigit).Select(char.ToUpperInvariant).ToArray();
        return new string(chars);
    }

    public static bool TryNormalize(string? input, out string normalized)
    {
        normalized = Normalize(input);
        return normalized.Length == 12 && normalized.All(Uri.IsHexDigit);
    }

    public static string FormatWithColons(string input)
    {
        var normalized = Normalize(input);
        if (normalized.Length != 12)
        {
            return input.Trim();
        }

        var builder = new StringBuilder(17);
        for (var i = 0; i < normalized.Length; i += 2)
        {
            if (builder.Length > 0)
            {
                builder.Append(':');
            }

            builder.Append(normalized, i, 2);
        }

        return builder.ToString();
    }

    public static string FormatBluetoothAddress(ulong bluetoothAddress)
    {
        return string.Join(":", Enumerable.Range(0, 6)
            .Select(i => ((bluetoothAddress >> (8 * (5 - i))) & 0xff).ToString("X2", CultureInfo.InvariantCulture)));
    }

    public static string FormatBytes(byte[] bytes, int offset, bool reverse)
    {
        if (offset < 0 || bytes.Length - offset < 6)
        {
            return string.Empty;
        }

        var parts = new string[6];
        for (var i = 0; i < 6; i++)
        {
            var index = reverse ? offset + 5 - i : offset + i;
            parts[i] = bytes[index].ToString("X2", CultureInfo.InvariantCulture);
        }

        return string.Join(":", parts);
    }
}
