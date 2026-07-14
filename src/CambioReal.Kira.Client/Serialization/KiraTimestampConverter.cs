using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CambioReal.Kira.Serialization;

/// <summary>
/// Lê timestamps que não seguem o subconjunto estrito de ISO 8601 que o conversor padrão de
/// <see cref="DateTimeOffset"/> do <c>System.Text.Json</c> exige.
/// </summary>
/// <remarks>
/// Confirmado contra o sandbox em 2026-07-14: <c>KiraRecipient.CreatedAt</c>/<c>UpdatedAt</c>
/// (<c>created_ts</c>/<c>updated_ts</c>) vêm como <c>"2026-07-14 01:29:21.851995+00"</c> — espaço
/// em vez de <c>T</c>, e offset de 2 dígitos sem separador de minutos (<c>+00</c>, não
/// <c>+00:00</c>). O conversor padrão rejeita essa forma. Outros endpoints (ex.: <c>KiraUser</c>)
/// devolvem ISO 8601 padrão (<c>"2026-07-13T20:42:04.713Z"</c>), que este conversor também aceita
/// via <see cref="DateTimeOffset.TryParse(string?, IFormatProvider?, DateTimeStyles, out DateTimeOffset)"/>
/// antes de cair nos formatos explícitos abaixo.
/// </remarks>
internal sealed class KiraTimestampConverter : JsonConverter<DateTimeOffset?>
{
    private static readonly string[] ExplicitFormats =
    [
        "yyyy-MM-dd HH:mm:ss.FFFFFFzz",
        "yyyy-MM-dd HH:mm:ss.FFFFFFzzz",
        "yyyy-MM-dd HH:mm:sszz",
        "yyyy-MM-dd HH:mm:sszzz",
    ];

    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        var value = reader.GetString();

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }

        foreach (var format in ExplicitFormats)
        {
            if (DateTimeOffset.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
            {
                return parsed;
            }
        }

        throw new JsonException($"Não foi possível interpretar o timestamp da Kira: '{value}'.");
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value is { } dateTimeOffset)
        {
            writer.WriteStringValue(dateTimeOffset);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
