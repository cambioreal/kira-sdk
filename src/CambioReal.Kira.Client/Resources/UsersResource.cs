using CambioReal.Kira.Http;
using CambioReal.Kira.Models;

namespace CambioReal.Kira.Resources;

/// <summary>Usuários, verificação e elegibilidade por produto.</summary>
public sealed class UsersResource
{
    private readonly KiraClient client;

    internal UsersResource(KiraClient client) => this.client = client;

    /// <summary>
    /// Cria um usuário. <c>POST /v1/users</c>.
    /// </summary>
    /// <remarks>
    /// A Kira avalia o usuário contra todos os produtos ativos e dispara a verificação assim que
    /// <em>ao menos um</em> deles tiver seus campos completos. Cheque
    /// <see cref="CreateUserResponse.VerificationTriggered"/>,
    /// <see cref="KiraUser.EligibleProducts"/> e <see cref="KiraUser.MissingFields"/> na resposta —
    /// um 201 não significa que a verificação começou.
    /// </remarks>
    /// <param name="request">Dados do usuário.</param>
    /// <param name="idempotencyKey">
    /// Sua chave de idempotência. Se omitida, uma é gerada — o que torna a chamada idempotente
    /// apenas dentro deste processo. Para sobreviver a um retry após crash, gere e persista a sua.
    /// </param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    public Task<CreateUserResponse> CreateAsync(
        CreateUserRequest request,
        Guid? idempotencyKey = null,
        CancellationToken cancellationToken = default) =>
        client.PostAsync<CreateUserRequest, CreateUserResponse>(
            KiraPaths.Users,
            request,
            Idempotent(idempotencyKey),
            cancellationToken);

    /// <summary>Busca um usuário. <c>GET /v1/users/{id}</c>.</summary>
    public Task<KiraUser> GetAsync(string userId, CancellationToken cancellationToken = default) =>
        client.GetAsync<KiraUser>(KiraPaths.User(userId), cancellationToken: cancellationToken);

    /// <summary>
    /// Atualiza um usuário. <c>PATCH /v1/users/{id}</c>.
    /// </summary>
    /// <remarks>
    /// Alterar um campo sensível (nome, nascimento, endereço, SSN, documento) dispara reverificação
    /// em background. A resposta informa isso em <see cref="UpdateUserResponse.RequiresReverification"/>.
    /// </remarks>
    public Task<UpdateUserResponse> UpdateAsync(
        string userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default) =>
        client.PatchAsync<UpdateUserRequest, UpdateUserResponse>(
            KiraPaths.User(userId),
            request,
            cancellationToken: cancellationToken);

    /// <summary>Lista usuários. <c>GET /v1/users</c>.</summary>
    public Task<KiraPage<KiraUser>> ListAsync(
        ListUsersRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        request ??= new ListUsersRequest();

        var path = QueryString.Append(
            KiraPaths.Users,
            ("status", request.Status),
            ("type", QueryString.Format(request.Type)),
            ("verification_status", QueryString.Format(request.VerificationStatus)),
            ("email", request.Email),
            ("created_from", QueryString.Format(request.CreatedFrom)),
            ("created_to", QueryString.Format(request.CreatedTo)),
            ("page", QueryString.Format(request.Page)),
            ("limit", QueryString.Format(request.Limit)));

        return client.GetAsync<KiraPage<KiraUser>>(path, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Dispara a verificação manualmente. <c>POST /v1/users/{id}/verifications</c>.
    /// </summary>
    /// <remarks>
    /// Fluxo legado. O caminho recomendado é enviar todos os campos exigidos em
    /// <see cref="CreateAsync"/>, que dispara a verificação automaticamente.
    /// </remarks>
    public Task<CreateVerificationResponse> CreateVerificationAsync(
        string userId,
        CreateVerificationRequest request,
        Guid? idempotencyKey = null,
        CancellationToken cancellationToken = default) =>
        client.PostAsync<CreateVerificationRequest, CreateVerificationResponse>(
            KiraPaths.UserVerifications(userId),
            request,
            Idempotent(idempotencyKey),
            cancellationToken);

    internal static KiraRequestContext Idempotent(Guid? key) =>
        new() { IdempotencyKey = key ?? Guid.NewGuid() };
}

/// <summary>Destinatários de payout.</summary>
public sealed class RecipientsResource
{
    private readonly KiraClient client;

    internal RecipientsResource(KiraClient client) => this.client = client;

    /// <summary>Cria um recipient. <c>POST /v1/recipients</c>.</summary>
    public Task<KiraRecipient> CreateAsync(
        CreateRecipientRequest request,
        Guid? idempotencyKey = null,
        CancellationToken cancellationToken = default) =>
        client.PostAsync<CreateRecipientRequest, KiraRecipient>(
            KiraPaths.Recipients,
            request,
            UsersResource.Idempotent(idempotencyKey),
            cancellationToken);

    /// <summary>Lista os recipients de um usuário. <c>GET /v1/recipients?user_id=</c>.</summary>
    /// <remarks>
    /// A doc não declara o nome do parâmetro de query; <c>user_id</c> é inferido. Confirmado
    /// contra o sandbox em 2026-07-14: a resposta é <c>{"recipients": [...], "total": N}</c>.
    /// </remarks>
    public async Task<IReadOnlyList<KiraRecipient>> ListByUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var path = QueryString.Append(KiraPaths.Recipients, ("user_id", userId));
        var envelope = await client.GetAsync<KiraRecipientsEnvelope>(path, cancellationToken: cancellationToken);
        return envelope.Recipients;
    }

    /// <summary>Busca um recipient. <c>GET /v1/recipients/{id}</c>.</summary>
    public Task<KiraRecipient> GetAsync(string recipientId, CancellationToken cancellationToken = default) =>
        client.GetAsync<KiraRecipient>(KiraPaths.Recipient(recipientId), cancellationToken: cancellationToken);
}
