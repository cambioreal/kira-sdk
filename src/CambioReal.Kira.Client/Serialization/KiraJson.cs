using System.Text.Json;
using System.Text.Json.Serialization;

namespace CambioReal.Kira.Serialization;

/// <summary>Convenções de JSON da API Kira.</summary>
public static class KiraJson
{
    /// <summary>
    /// A Kira usa <c>snake_case</c> em todo o corpo de requisição e resposta
    /// (<c>access_token</c>, <c>expires_in</c>, <c>business_legal_name</c>, …).
    /// </summary>
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

            // A Kira devolve valores monetários como string ("1000.00", "23.00").
            // Ler número a partir de string evita um converter dedicado por campo.
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
        };

        // Deliberadamente sem um JsonStringEnumConverter global: a Kira mistura snake_case
        // (verification_link), SCREAMING_SNAKE (INSTANT_PAY), kebab-case (usa-virtual-accounts) e
        // PascalCase (Full) nos valores de enum. Um conversor na coleção Converters teria
        // precedência sobre o [JsonConverter] de cada enum e uniformizaria tudo — errado.
        // Cada enum declara seu próprio conversor. Ver EnumConverters.cs.

        // populateMissingResolver: true instala o resolver por reflexão. Sem ele, MakeReadOnly()
        // lança em runtime — e congelar as opções aqui evita a penalidade de warm-up a cada
        // primeira serialização de um tipo novo.
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
