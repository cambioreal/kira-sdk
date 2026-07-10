using System.Text.Json;
using System.Text.Json.Serialization;

namespace CambioReal.Kira.Serialization;

/// <summary>
/// Converte enums para <c>snake_case</c> minúsculo: <c>VerificationLink</c> → <c>verification_link</c>.
/// </summary>
/// <remarks>
/// A Kira não usa uma convenção única para valores de enum: <c>verification_link</c> é snake_case,
/// <c>INSTANT_PAY</c> é SCREAMING_SNAKE, <c>usa-virtual-accounts</c> é kebab-case e <c>Full</c> é
/// PascalCase. Por isso cada enum declara seu próprio conversor por atributo, em vez de haver uma
/// política global. Um <see cref="JsonStringEnumConverter"/> na coleção
/// <see cref="JsonSerializerOptions.Converters"/> teria precedência sobre esses atributos e
/// uniformizaria tudo — errado.
/// </remarks>
internal sealed class SnakeCaseLowerEnumConverter<TEnum> : JsonStringEnumConverter<TEnum>
    where TEnum : struct, Enum
{
    public SnakeCaseLowerEnumConverter()
        : base(JsonNamingPolicy.SnakeCaseLower)
    {
    }
}

/// <summary>Converte enums para <c>SCREAMING_SNAKE_CASE</c>: <c>InstantPay</c> → <c>INSTANT_PAY</c>, <c>Ach</c> → <c>ACH</c>.</summary>
internal sealed class UpperSnakeCaseEnumConverter<TEnum> : JsonStringEnumConverter<TEnum>
    where TEnum : struct, Enum
{
    public UpperSnakeCaseEnumConverter()
        : base(JsonNamingPolicy.SnakeCaseUpper)
    {
    }
}

/// <summary>Converte enums para <c>kebab-case</c>: <c>UsaVirtualAccountsAct</c> → <c>usa-virtual-accounts-act</c>.</summary>
internal sealed class KebabCaseLowerEnumConverter<TEnum> : JsonStringEnumConverter<TEnum>
    where TEnum : struct, Enum
{
    public KebabCaseLowerEnumConverter()
        : base(JsonNamingPolicy.KebabCaseLower)
    {
    }
}
