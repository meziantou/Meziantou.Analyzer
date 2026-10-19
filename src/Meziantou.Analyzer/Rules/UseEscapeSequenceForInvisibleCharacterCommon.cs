using System.Globalization;

namespace Meziantou.Analyzer.Rules;

internal static class UseEscapeSequenceForInvisibleCharacterCommon
{
    // The list is explicit instead of using CharUnicodeInfo, as the Unicode data depends on the runtime (.NET Framework in Visual Studio, .NET with the SDK).
    // CR and LF are not reported (MA0101). The characters used by emoji sequences are not reported: ZWJ (U+200D), variation selectors and tag characters.
    public static bool IsInvisibleCharacter(int codePoint) => codePoint switch
    {
        '\r' or '\n' => false,
        < 0x20 => true,
        < 0x7F => false,
        <= 0x9F => true,
        0x00A0 or 0x00AD or 0x034F or 0x061C or 0x115F or 0x1160 or 0x1680 or 0x17B4 or 0x17B5 or 0x180E => true,
        >= 0x2000 and <= 0x200C => true,
        0x200E or 0x200F => true,
        >= 0x2028 and <= 0x202F => true,
        >= 0x205F and <= 0x2064 => true,
        >= 0x2066 and <= 0x206F => true,
        0x2800 or 0x3000 or 0x3164 or 0xFEFF or 0xFFA0 => true,
        >= 0xFFF9 and <= 0xFFFB => true,
        >= 0x1BCA0 and <= 0x1BCA3 => true,
        >= 0x1D173 and <= 0x1D17A => true,
        0xE0001 => true,
        _ => false,
    };

    public static int GetCodePoint(string text, int index, out int length)
    {
        if (char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
        {
            length = 2;
            return char.ConvertToUtf32(text[index], text[index + 1]);
        }

        length = 1;
        return text[index];
    }

    public static string GetCodePointName(int codePoint) => "U+" + codePoint.ToString(codePoint <= 0xFFFF ? "X4" : "X5", CultureInfo.InvariantCulture);

    // The \e escape sequence is supported since C# 13
    public static string GetEscapeSequence(int codePoint, bool supportsEscapeCharacterEscapeSequence) => codePoint switch
    {
        '\0' => "\\0",
        '\a' => "\\a",
        '\b' => "\\b",
        '\f' => "\\f",
        '\t' => "\\t",
        '\v' => "\\v",
        0x1B when supportsEscapeCharacterEscapeSequence => "\\e",
        <= 0xFFFF => "\\u" + codePoint.ToString("X4", CultureInfo.InvariantCulture),
        _ => "\\U" + codePoint.ToString("X8", CultureInfo.InvariantCulture),
    };
}
