
using Pos.SalesService.Application.DTOS.InventoryClient;
using Pos.SalesService.Application.Interfaces.Clients;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Pos.SalesService.Infrastructure.Shared.Clients
{
    public class InventoryClient : IInventoryClient
    {
        private readonly HttpClient _httpClient;
        private readonly ICurrentUserService _currentUserService;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public InventoryClient(HttpClient httpClient,ICurrentUserService currentUserService)
        {
            _httpClient = httpClient;
            _currentUserService = currentUserService;
        }

        public async Task<Result<Guid>> CreateStockReservationAsync(
            CreateStockReservationRequest request,
            CancellationToken cancellationToken)
        {
            var result = await SendInventoryRequestAsync<Guid>(
                HttpMethod.Post,
                "api/v1/StockReservations",
                request,
                cancellationToken);

            if (result.IsFailure)
                return result;

            if (result.Value == Guid.Empty)
                return Result<Guid>.Failure(
                    "Inventory returned an invalid reservation ID. " +
                    "The reservation outcome could not be confirmed.");

            return result;
        }
        public async Task<Result> ConsumeStockReservationAsync(
    Guid reservationId,
    CancellationToken cancellationToken)
        {
            var result = await SendInventoryRequestAsync<Guid>(
                HttpMethod.Post,
                "api/v1/StockReservations/consume",
                new { ReservationId = reservationId },
                cancellationToken);

            if (result.IsFailure)
                return Result.Failure(result.Errors.ToArray());

            if (result.Value == Guid.Empty)
                return Result.Failure(
                    "Inventory returned an invalid response. " +
                    "Consumption could not be confirmed.");

            return Result.Success();
        }

        public async Task<Result> ReleaseStockReservationAsync(
            Guid reservationId,
            CancellationToken cancellationToken)
        {
            // The existing release endpoint returns Response<string>.
            var result = await SendInventoryRequestAsync<string>(
                HttpMethod.Post,
                "api/v1/StockReservations/release",
                new { ReservationId = reservationId },
                cancellationToken);

            if (result.IsFailure)
                return Result.Failure(result.Errors.ToArray());

            return Result.Success();
        }

        public async Task<Result<StockReservationDetailsDto>>
            GetStockReservationByIdAsync(
                Guid reservationId,
                CancellationToken cancellationToken)
        {
            var result = await SendInventoryRequestAsync<StockReservationDetailsDto>(
                HttpMethod.Get,
                $"api/v1/StockReservations/{reservationId}",
                null,
                cancellationToken);

            if (result.IsFailure)
                return result;

            if (result.Value == null ||
                result.Value.Id != reservationId)
            {
                return Result<StockReservationDetailsDto>.Failure(
                    "Inventory returned invalid reservation details.");
            }

            return result;
        }

        public async Task<Result<StockReservationDetailsDto>>
            GetStockReservationByReferenceIdAsync(
                Guid saleId,
                CancellationToken cancellationToken)
        {
            var result = await SendInventoryRequestAsync<StockReservationDetailsDto>(
                HttpMethod.Get,
                $"api/v1/StockReservations/by-reference/{saleId}",
                null,
                cancellationToken);

            if (result.IsFailure)
                return result;

            if (result.Value == null ||
                result.Value.Id == Guid.Empty ||
                result.Value.ReferenceId != saleId)
            {
                return Result<StockReservationDetailsDto>.Failure(
                    "Inventory returned invalid reservation details.");
            }

            return result;
        }

        private async Task<Result<T>> SendInventoryRequestAsync<T>(
             HttpMethod method,
             string endpoint,
             object? body,
             CancellationToken cancellationToken)
        {
            if (_httpClient.BaseAddress == null)
                return Result<T>.Failure("Inventory service address is not configured.");

            var accessToken = _currentUserService.AccessToken;

            if (string.IsNullOrWhiteSpace(accessToken))
                return Result<T>.Failure("Access token is missing.");

            using var httpRequest = new HttpRequestMessage(method, endpoint);

            httpRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            if (body != null)
                httpRequest.Content = JsonContent.Create(body);

            try
            {
                using var response = await _httpClient.SendAsync(
                    httpRequest,
                    cancellationToken);

                var responseBody = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                var apiResponse = DeserializeResponse<T>(responseBody);

                if (!response.IsSuccessStatusCode)
                {
                    var errors = ExtractErrors(
                        apiResponse,
                        $"Inventory request failed. StatusCode: {(int)response.StatusCode}.");

                    return Result<T>.Failure(errors);
                }

                if (apiResponse == null)
                    return Result<T>.Failure(
                        "Inventory returned an empty response. " +
                        "The operation could not be confirmed.");

                if (!apiResponse.Succeeded)
                {
                    var errors = ExtractErrors(
                        apiResponse,
                        "Inventory rejected the request.");

                    return Result<T>.Failure(errors);
                }

                if (apiResponse.Data is null)
                    return Result<T>.Failure(
                        "Inventory returned no response data. " +
                        "The operation could not be confirmed.");

                return Result<T>.Success(apiResponse.Data);
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return Result<T>.Failure(
                    "Inventory request timed out. " +
                    "The operation outcome could not be confirmed.");
            }
            catch (HttpRequestException)
            {
                return Result<T>.Failure(
                    "Could not communicate with Inventory. " +
                    "The operation outcome could not be confirmed.");
            }
            catch (JsonException)
            {
                return Result<T>.Failure(
                    "Inventory returned an invalid response. " +
                    "The operation outcome could not be confirmed.");
            }
        }

        private static Response<T>? DeserializeResponse<T>(string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
                return null;

            return JsonSerializer.Deserialize<Response<T>>(
                responseBody,
                JsonOptions);
        }

        private static string[] ExtractErrors<T>(
            Response<T>? response,
            string fallbackMessage)
        {
            if (response?.Errors != null && response.Errors.Any())
                return response.Errors.ToArray();

            if (!string.IsNullOrWhiteSpace(response?.Message))
                return new[] { response.Message };

            return new[] { fallbackMessage };
        }
    }
}
