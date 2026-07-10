namespace CambioReal.Kira;

/// <summary>Configuração do <see cref="KiraClient"/>.</summary>
public sealed class KiraOptions
{
    /// <summary>Nome da seção de configuração sugerida.</summary>
    public const string SectionName = "Kira";

    /// <summary>Identificador (UUID) da aplicação, fornecido pela Kira.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Credencial secreta da aplicação. Deve vir do <c>pass</c> ou de um secret store — nunca do código.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Chave de API enviada no header <c>x-api-key</c> em toda requisição.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Ambiente alvo. O padrão é <see cref="KiraEnvironment.Sandbox"/>, deliberadamente.</summary>
    public KiraEnvironment Environment { get; set; } = KiraEnvironment.Sandbox;

    /// <summary>
    /// Sobrescreve o endereço base derivado de <see cref="Environment"/>. Precisa terminar em <c>/</c>
    /// — ver <see cref="KiraEnvironmentExtensions.GetBaseAddress"/>.
    /// </summary>
    public Uri? BaseAddress { get; set; }

    /// <summary>
    /// Margem de segurança para renovar o token antes do vencimento real. O token da Kira vive 3600 s
    /// e não existe refresh token: expirar significa reautenticar do zero.
    /// </summary>
    public TimeSpan TokenExpirationSkew { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>Timeout de cada requisição HTTP.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Endereço base efetivo.</summary>
    public Uri ResolveBaseAddress() => BaseAddress ?? Environment.GetBaseAddress();

    /// <summary>Valida a configuração e lança se estiver inconsistente.</summary>
    /// <exception cref="InvalidOperationException">Alguma credencial obrigatória está ausente ou o base address é inválido.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ClientId))
        {
            throw new InvalidOperationException($"{nameof(KiraOptions)}.{nameof(ClientId)} é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            throw new InvalidOperationException($"{nameof(KiraOptions)}.{nameof(Password)} é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException($"{nameof(KiraOptions)}.{nameof(ApiKey)} é obrigatório.");
        }

        var baseAddress = ResolveBaseAddress();

        if (!baseAddress.IsAbsoluteUri)
        {
            throw new InvalidOperationException($"{nameof(BaseAddress)} precisa ser absoluto.");
        }

        // Sem a barra final, o Uri descarta o último segmento ao resolver paths relativos —
        // e um cliente apontado para o sandbox passaria a chamar produção.
        if (!baseAddress.AbsolutePath.EndsWith('/'))
        {
            throw new InvalidOperationException(
                $"{nameof(BaseAddress)} precisa terminar em '/' (recebido: '{baseAddress}'). " +
                "Sem a barra final, o prefixo /sandbox é descartado na resolução de paths relativos.");
        }

        if (TokenExpirationSkew < TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{nameof(TokenExpirationSkew)} não pode ser negativo.");
        }

        if (Timeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{nameof(Timeout)} precisa ser positivo.");
        }
    }
}
