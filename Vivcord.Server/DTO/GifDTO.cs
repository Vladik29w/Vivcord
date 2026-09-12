namespace Vivcord.Server.DTO
{
    public record PagedResultDto<T>
    {
        public List<T> Items { get; init; } = [];
        public int Page { get; init; }
        public bool HasNext { get; init; }
    }

    public record GifDTO
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public string PreviewUrl { get; init; } = string.Empty;
        public string OriginalUrl { get; init; } = string.Empty;
        public int Width { get; init; } = 10;
        public int Height { get; init; } = 10;
    }
}
