using CambioReal.Kira.Contracts;
using Shouldly;
using Xunit;

namespace CambioReal.Kira.Tests.Contracts;

public sealed class EnvelopeTests
{
    private sealed record Probe(string Id);

    [Fact]
    public void OkBuildsASuccessEnvelope()
    {
        var envelope = Envelope.Ok(new Probe("p-1"), "PROBE_FETCHED", "Probe encontrado.");

        envelope.Success.ShouldBeTrue();
        envelope.Code.ShouldBe("PROBE_FETCHED");
        envelope.Data!.Id.ShouldBe("p-1");
        envelope.Errors.ShouldBeEmpty();
        envelope.Warnings.ShouldBeEmpty();
    }

    /// <summary>Ausência de conteúdo é <see langword="null"/>, nunca um objeto/array/string vazios.</summary>
    [Fact]
    public void OkWithoutDataLeavesDataNull()
    {
        var envelope = Envelope.Ok<Probe>(null, "PROBE_DELETED", "Probe removido.");

        envelope.Data.ShouldBeNull();
    }

    [Fact]
    public void FailBuildsAFailureEnvelopeWithAtLeastOneError()
    {
        var error = new ProblemDetail
        {
            Type = new Uri("https://errors.cambioreal.dev/probe-not-found"),
            Status = 404,
            Code = "PROBE_NOT_FOUND",
            Title = "Probe não encontrado",
            Retryable = false,
            Severity = ErrorSeverity.Error,
        };

        var envelope = Envelope.Fail<Probe>([error], "PROBE_NOT_FOUND", "Probe não encontrado.");

        envelope.Success.ShouldBeFalse();
        envelope.Data.ShouldBeNull();
        envelope.Errors.Count.ShouldBe(1);
        envelope.Errors[0].Code.ShouldBe("PROBE_NOT_FOUND");
    }

    /// <summary>Uma resposta de falha sem nenhum erro é uma contradição de contrato — falha cedo.</summary>
    [Fact]
    public void FailRequiresAtLeastOneError()
    {
        Should.Throw<ArgumentException>(() => Envelope.Fail<Probe>([], "X", "x"));
    }

    [Fact]
    public void PaginationLivesInMetadataNotInData()
    {
        var pagination = new PagedMetadata { Page = 2, PageSize = 10, TotalItems = 25, TotalPages = 3, HasNext = true, HasPrevious = true };
        var metadata = new ResponseMetadata { Timestamp = DateTimeOffset.UtcNow, Pagination = pagination };

        var envelope = Envelope.Ok(new List<Probe> { new("p-1") }, "PROBES_LISTED", "Ok.", metadata);

        envelope.Metadata.Pagination.ShouldBe(pagination);
        envelope.Data.ShouldBeOfType<List<Probe>>();
    }
}
