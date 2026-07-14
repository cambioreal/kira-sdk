using System.Text.Json;
using System.Text.Json.Serialization;

namespace CambioReal.Contracts.Serialization;

/// <summary>
/// Convenções de JSON do contrato canônico (<see cref="Envelope{T}"/>).
/// </summary>
/// <remarks>
/// Deliberadamente separado do formato de fio de qualquer integração específica — ex.: o
/// <c>KiraJson</c> do kira-sdk é snake_case com várias convenções de enum (o formato de fio *da
/// Kira*); este é o contrato de saída *da plataforma*, camelCase, seguindo a convenção usual de
/// JSON/OpenAPI. Não confundir os dois.
/// </remarks>
public static class EnvelopeJson
{
    /// <summary>Opções de serialização do <see cref="Envelope{T}"/> e tipos relacionados.</summary>
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
