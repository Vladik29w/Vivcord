using ErrorOr;
using System.Net;
using System.Text.Json;
using Vivcord.Server.DTO;

namespace Vivcord.Server.Infastructure.Api
{
    public interface IKlipyService
    {
        Task<ErrorOr<PagedResultDto<GifDTO>>> GetTrendingGifs(CancellationToken cancellationToken = default);
        Task<ErrorOr<PagedResultDto<GifDTO>>> SearchGifs(string query, CancellationToken cancellationToken = default);
    }
    public class KlipyService(HttpClient httpClient, IConfiguration config) : IKlipyService
    {
        private const string url = "https://api.klipy.com/api/v1";
        private readonly string _apiKey = config["Klipy:ApiKey"] ?? throw new InvalidOperationException("Null API key");

        public async Task<ErrorOr<PagedResultDto<GifDTO>>> GetTrendingGifs(CancellationToken cancellationToken = default)
        {
            var response = await httpClient.GetAsync($"{url}/{_apiKey}/gifs/trending?page=1&per_page=20", cancellationToken);//TOOD: add pagination with lazy loading in angular

            if (!response.IsSuccessStatusCode)
                return Error.Failure("Failed to fetch trending gifs from Klipy API.");

            var json = await response.Content.ReadFromJsonAsync<KlipyApiResponse>(cancellationToken: cancellationToken);

            if (json == null)
                return Error.Failure("Failed to deserialize Klipy API response.");

            return MapKlipyDataToGifDto(json);
        }
        public async Task<ErrorOr<PagedResultDto<GifDTO>>> SearchGifs(string query, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Error.Validation("Search query cannot be empty.");
                
            var encodedQuery = Uri.EscapeDataString(query);
            var response = await httpClient.GetAsync($"{url}/{_apiKey}/gifs/search?page=1&per_page=20&q={encodedQuery}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return Error.NotFound("Not found any GIFS for " + query);
            if (response.StatusCode == HttpStatusCode.InternalServerError)
                return Error.Failure("Klipy API internal error");
            if (!response.IsSuccessStatusCode)
                return Error.Unexpected("Failed to fetch gifs.");

            var json = await response.Content.ReadFromJsonAsync<KlipyApiResponse>(cancellationToken: cancellationToken);

            if (json == null)
                return Error.Failure("Failed to deserialize Klipy API response.");

            return MapKlipyDataToGifDto(json);
        }

        private PagedResultDto<GifDTO> MapKlipyDataToGifDto(KlipyApiResponse json)
        {
            return new PagedResultDto<GifDTO>
            {
                Page = json.Data.CurrentPage,
                HasNext = json.Data.HasNext,
                Items = json.Data.Data.Select(item =>
                {
                    var original = item.File?.Hd?.Gif ?? item.File?.Md?.Gif;
                    var preview = item.File?.Sm?.Gif ?? item.File?.Xs?.Gif;

                    var previewUrl = preview?.Url ?? item.BlurPreview ?? string.Empty;
                    var originalUrl = original?.Url ?? string.Empty;
                    var width = original?.Width ?? 0;
                    var height = original?.Height ?? 0;

                    return new GifDTO
                    {
                        Id = item.Id.ToString(),
                        Name = item.Title,
                        PreviewUrl = previewUrl,
                        OriginalUrl = originalUrl,
                        Width = width,
                        Height = height
                    };
                }).ToList()
            };
        }
    }
}
