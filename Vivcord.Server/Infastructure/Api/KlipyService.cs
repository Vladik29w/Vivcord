using ErrorOr;
using System.Net;
using System.Text.Json;
using Vivcord.Server.DTO;

namespace Vivcord.Server.Infastructure.Api
{
    public interface IKlipyService
    {
        Task<ErrorOr<PagedResultDto<GifDTO>>> GetTrendingGifs();
        Task<ErrorOr<PagedResultDto<GifDTO>>> SearchGifs(string query);
    }
    public class KlipyService(HttpClient httpClient, IConfiguration config) : IKlipyService
    {
        private const string url = "https://api.klipy.com/api/v1";
        private readonly string _apiKey = config["Klipy:ApiKey"] ?? throw new InvalidOperationException("Null API key");

        public async Task<ErrorOr<PagedResultDto<GifDTO>>> GetTrendingGifs()
        {
            var response = await httpClient.GetAsync($"{url}/{_apiKey}/gifs/trending?page={1}&per_page={20}");//TOOD: add pagination with lazy loading in angular

            if (!response.IsSuccessStatusCode)
                return Error.Failure("Failed to fetch trending gifs from Klipy API.");

            var jsonString = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<KlipyApiResponse>(jsonString);
            if (json == null)
                return Error.Failure("Failed to deserialize Klipy API response.");

            return MapKlipyDataToGifDto(json);
        }
        public async Task<ErrorOr<PagedResultDto<GifDTO>>> SearchGifs(string query)
        {
            query = Uri.EscapeDataString(query);
            var response = await httpClient.GetAsync($"{url}/{_apiKey}/gifs/search?page={1}&per_page={20}&q={query}");

            if (response.StatusCode == HttpStatusCode.NotFound)
                return Error.NotFound("Not found any GIFS for " + query);
            if (response.StatusCode == HttpStatusCode.InternalServerError)
                return Error.Failure("Klipy API internal error");
            if (!response.IsSuccessStatusCode)
                return Error.Unexpected("Failed to fetch gifs.");

            var json = await response.Content.ReadFromJsonAsync<KlipyApiResponse>();

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

                    var originalUrl = original?.Url ?? string.Empty;
                    var previewUrl = preview?.Url ?? item.BlurPreview ?? string.Empty;
                    var width = original?.Width ?? 0;
                    var height = original?.Height ?? 0;

                    return new GifDTO
                    {
                        Id = item.Id.ToString(),
                        Name = item.Title,
                        PreviewUrl = preview?.Url ?? item.BlurPreview ?? string.Empty,
                        OriginalUrl = original?.Url ?? string.Empty,
                        Width = preview?.Width ?? 0,
                        Height = preview?.Height ?? 0
                    };
                }).ToList()
            };
        }
    }
}
