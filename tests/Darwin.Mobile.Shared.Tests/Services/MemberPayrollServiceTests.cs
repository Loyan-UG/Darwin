using Darwin.Contracts.Common;
using Darwin.Contracts.HumanResources;
using Darwin.Mobile.Shared.Api;
using Darwin.Mobile.Shared.Services.Payroll;
using Darwin.Shared.Results;
using FluentAssertions;

namespace Darwin.Mobile.Shared.Tests.Services;

public sealed class MemberPayrollServiceTests
{
    [Fact]
    public async Task GetMyPayslipsAsync_Should_Fail_WhenPageIsInvalid()
    {
        var service = new MemberPayrollService(new FakeApiClient());

        var result = await service.GetMyPayslipsAsync(0, 20, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Page must be a positive integer.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public async Task GetMyPayslipsAsync_Should_Fail_WhenPageSizeIsInvalid(int pageSize)
    {
        var service = new MemberPayrollService(new FakeApiClient());

        var result = await service.GetMyPayslipsAsync(1, pageSize, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("PageSize must be between 1 and 200.");
    }

    [Fact]
    public async Task GetMyPayslipsAsync_Should_CallCanonicalRoute()
    {
        var response = new PagedResponse<MemberPayslipSummary>
        {
            Total = 1,
            Items =
            [
                new MemberPayslipSummary
                {
                    Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    PayslipNumber = "PS-1",
                    PaymentStatus = "Paid"
                }
            ],
            Request = new PagedRequest { Page = 2, PageSize = 10 }
        };
        var api = new FakeApiClient
        {
            OnGetResultAsync = route =>
            {
                route.Should().Be($"{ApiRoutes.Payroll.GetMyPayslips}?page=2&pageSize=10");
                return Result<PagedResponse<MemberPayslipSummary>>.Ok(response);
            }
        };
        var service = new MemberPayrollService(api);

        var result = await service.GetMyPayslipsAsync(2, 10, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().BeSameAs(response);
    }

    [Fact]
    public async Task DownloadPayslipDocumentAsync_Should_CallCanonicalPdfRoute()
    {
        var payslipId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var file = new ApiFileResult
        {
            Content = [0x25, 0x50, 0x44, 0x46],
            ContentType = "application/pdf",
            FileName = "payslip.pdf",
            ContentLength = 4
        };
        var api = new FakeApiClient
        {
            OnGetFileResultAsync = route =>
            {
                route.Should().Be(ApiRoutes.Payroll.DownloadPayslipDocument(payslipId));
                return Result<ApiFileResult>.Ok(file);
            }
        };
        var service = new MemberPayrollService(api);

        var result = await service.DownloadPayslipDocumentAsync(payslipId, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().BeSameAs(file);
        result.Value!.ContentType.Should().Be("application/pdf");
    }

    private sealed class FakeApiClient : IApiClient
    {
        public Func<string, object?>? OnGetResultAsync { get; init; }
        public Func<string, object?>? OnGetFileResultAsync { get; init; }

        public void SetBearerToken(string? accessToken)
        {
        }

        public Task<Result<TResponse>> GetResultAsync<TResponse>(string route, CancellationToken ct)
        {
            if (OnGetResultAsync is null)
            {
                return Task.FromResult(Result<TResponse>.Fail("No GET handler configured."));
            }

            var response = OnGetResultAsync(route);
            return Task.FromResult(response as Result<TResponse> ?? Result<TResponse>.Fail("Unexpected GET result type."));
        }

        public Task<Result<string>> GetStringResultAsync(string route, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<ApiFileResult>> GetFileResultAsync(string route, CancellationToken ct)
        {
            if (OnGetFileResultAsync is null)
            {
                return Task.FromResult(Result<ApiFileResult>.Fail("No file GET handler configured."));
            }

            var response = OnGetFileResultAsync(route);
            return Task.FromResult(response as Result<ApiFileResult> ?? Result<ApiFileResult>.Fail("Unexpected file GET result type."));
        }

        public Task<Result<TResponse>> PostResultAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<TResponse>> PostFileResultAsync<TResponse>(string route, Stream fileStream, string formFieldName, string fileName, string contentType, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<TResponse>> GetEnvelopeResultAsync<TResponse>(string route, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<TResponse>> PostEnvelopeResultAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<TResponse?> GetAsync<TResponse>(string route, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<TResponse?> PostAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result<TResponse>> PutResultAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<TResponse?> PutAsync<TRequest, TResponse>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result> PutNoContentAsync<TRequest>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Result> PostNoContentAsync<TRequest>(string route, TRequest request, CancellationToken ct)
            => throw new NotSupportedException();
    }
}
