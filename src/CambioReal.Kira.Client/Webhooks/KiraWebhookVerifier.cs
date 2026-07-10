using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace CambioReal.Kira.Webhooks;

/// <summary>
/// Verifica a assinatura HMAC SHA-256 que a Kira envia no header <c>x-signature-sha256</c>.
/// </summary>
/// <remarks>
/// A documentação da Kira mostra o cálculo em Node (<c>crypto.createHmac("sha256", secret)</c>) mas
/// não declara a codificação do digest. Aceitamos hex e base64, ambos comparados em tempo constante.
/// O prefixo opcional <c>sha256=</c> também é tolerado. Confirmar contra o sandbox antes de
/// tratar isto como definitivo.
/// </remarks>
public static class KiraWebhookVerifier
{
    private const string SchemePrefix = "sha256=";

    /// <summary>
    /// Confere a assinatura sobre o corpo bruto do webhook.
    /// </summary>
    /// <param name="payload">Bytes exatos do corpo recebido. Não reserialize o JSON: qualquer
    /// diferença de espaçamento ou ordem de chaves invalida o HMAC.</param>
    /// <param name="signatureHeader">Valor de <c>x-signature-sha256</c>.</param>
    /// <param name="secret">Segredo definido no registro do webhook.</param>
    /// <returns><see langword="true"/> se a assinatura confere.</returns>
    public static bool IsValid(ReadOnlySpan<byte> payload, string? signatureHeader, string secret)
    {
        ArgumentException.ThrowIfNullOrEmpty(secret);

        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        var candidate = signatureHeader.AsSpan().Trim();

        if (candidate.StartsWith(SchemePrefix, StringComparison.OrdinalIgnoreCase))
        {
            candidate = candidate[SchemePrefix.Length..];
        }

        Span<byte> expected = stackalloc byte[HMACSHA256.HashSizeInBytes];
        HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), payload, expected);

        Span<byte> provided = stackalloc byte[HMACSHA256.HashSizeInBytes];

        return TryDecode(candidate, provided) && CryptographicOperations.FixedTimeEquals(expected, provided);
    }

    /// <summary>Sobrecarga por conveniência para um corpo já materializado.</summary>
    public static bool IsValid(byte[] payload, string? signatureHeader, string secret)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return IsValid(payload.AsSpan(), signatureHeader, secret);
    }

    private static bool TryDecode(ReadOnlySpan<char> value, Span<byte> destination)
    {
        // Um HMAC-SHA256 em hex tem exatamente 64 caracteres; em base64, 44 (com padding).
        if (value.Length == HMACSHA256.HashSizeInBytes * 2)
        {
            return Convert.FromHexString(value, destination, out _, out var written) == OperationStatus.Done
                && written == destination.Length;
        }

        return Convert.TryFromBase64Chars(value, destination, out var decoded) && decoded == destination.Length;
    }
}
