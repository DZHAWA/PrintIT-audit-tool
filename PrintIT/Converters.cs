using System.Globalization;

namespace PrintIT;

/// <summary>
/// Русское склонение числительных: 1 кабинет, 3 кабинета, 5 кабинетов
/// </summary>
public static class Plural
{
    public static string Choose(int n, string one, string few, string many)
    {
        int n100 = Math.Abs(n) % 100;
        int n10 = n100 % 10;

        if (n100 is >= 11 and <= 14) return many;
        if (n10 == 1) return one;
        if (n10 is >= 2 and <= 4) return few;
        return many;
    }

    public static string Format(int n, string one, string few, string many) => $"{n} {Choose(n, one, few, many)}";
}

/// <summary>
/// Число + слово в правильной форме. Параметр: "кабинет|кабинета|кабинетов"
/// </summary>
public class PluralConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        int n = value is int i ? i : 0;
        var forms = (parameter as string ?? string.Empty).Split('|');
        return forms.Length == 3 ? Plural.Format(n, forms[0], forms[1], forms[2]) : n.ToString(culture);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class IsPositiveConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int i && i > 0;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class IsZeroConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not int i || i == 0;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
