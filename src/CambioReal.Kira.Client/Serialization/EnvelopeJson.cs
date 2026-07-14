using System.Text.Json;
using System.Text.Json.Serialization;

namespace CambioReal.Kira.Serialization;

/// <summary>
/// Convenções de JSON do contrato canônico (<see cref="Contracts.Envelope{T}"/>).
/// </summary>
/// <remarks>
/// Deliberadamente separado de <see cref="KiraJson"/>: <see cref="KiraJson"/> é o formato de fio
/// *da Kira* (snake_case, várias convenções de enum) — este é o contrato de saída *da
/// plataforma*, camelCase, seguindo a convenção usual de JSON/OpenAPI. Não confundir os dois.
/// </remarks>
public static class EnvelopeJson
{
    /// <summary>Opções de serialização do <see cref="Contracts.Envelope{T}"/> e tipos relacionados.</summary>
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        };

        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
