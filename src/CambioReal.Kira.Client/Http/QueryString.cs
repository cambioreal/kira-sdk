using System.Globalization;
using System.Text;

namespace CambioReal.Kira.Http;

/// <summary>Monta query strings preservando o path relativo.</summary>
internal static class QueryString
{
    /// <summary>Anexa os parâmetros não nulos ao path. Devolve o path intacto se todos forem nulos.</summary>
    public static string Append(string path, params ReadOnlySpan<(string Key, string? Value)> parameters)
    {
        var builder = new StringBuilder(path);
        var first = true;

        foreach (var (key, value) in parameters)
        {
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            builder.Append(first ? '?' : '&');
            builder.Append(Uri.EscapeDataString(key));
            builder.Append('=');
            builder.Append(Uri.EscapeDataString(value));
            first = false;
        }

        return builder.ToString();
    }

    /// <summary>Formata um instante em ISO 8601 UTC.</summary>
    public static string? Format(DateTimeOffset? value) =>
        value?.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);

    /// <summary>Formata um inteiro de forma invariante.</summary>
    public static string? Format(int? value) =>
        value?.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Serializa um enum usando exatamente o conversor JSON que ele declara, para que a query
    /// string fale o mesmo vocabulário do corpo (<c>INSTANT_PAY</c>, não <c>InstantPay</c>).
    /// </summary>
    public static string? Format<TEnum>(TEnum? value)
        where TEnum : struct, Enum
    {
        if (value is null)
        {
            return null;
        }

        // JsonSerializer.Serialize devolve o valor entre aspas; removê-las é mais barato e mais
        // seguro do que reimplementar as três convenções de casing da Kira.
        var json = System.Text.Json.JsonSerializer.Serialize(value.Value, Serialization.KiraJson.Options);
        return json.Trim('"');
    }
}
