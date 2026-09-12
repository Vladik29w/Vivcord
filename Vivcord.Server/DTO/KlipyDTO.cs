namespace Vivcord.Server.DTO;

using System.Text.Json.Serialization;

public sealed record KlipyApiResponse
{
    [JsonPropertyName("result")]
    public bool Result { get; init; }

    [JsonPropertyName("data")]
    public KlipyDataContainer? Data { get; init; }
}

public sealed record KlipyDataContainer
{
    [JsonPropertyName("data")]
    public List<KlipyItem> Data { get; init; } = [];

    [JsonPropertyName("current_page")]
    public int CurrentPage { get; init; }

    [JsonPropertyName("per_page")]
    public int PerPage { get; init; }

    [JsonPropertyName("has_next")]
    public bool HasNext { get; init; }
}

public sealed record KlipyItem
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("slug")]
    public string Slug { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("file")]
    public KlipyMediaSizes? File { get; init; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; init; } = [];

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("blur_preview")]
    public string? BlurPreview { get; init; }
}

public sealed record KlipyMediaSizes
{
    [JsonPropertyName("hd")]
    public KlipyFormatGroup? Hd { get; init; }

    [JsonPropertyName("md")]
    public KlipyFormatGroup? Md { get; init; }

    [JsonPropertyName("sm")]
    public KlipyFormatGroup? Sm { get; init; }

    [JsonPropertyName("xs")]
    public KlipyFormatGroup? Xs { get; init; }
}

public sealed record KlipyFormatGroup
{
    [JsonPropertyName("gif")]
    public MediaFormat? Gif { get; init; }

    [JsonPropertyName("webp")]
    public MediaFormat? Webp { get; init; }

    [JsonPropertyName("mp4")]
    public MediaFormat? Mp4 { get; init; }

    [JsonPropertyName("webm")]
    public MediaFormat? Webm { get; init; }

    [JsonPropertyName("jpg")]
    public MediaFormat? Jpg { get; init; }
}

public sealed record MediaFormat
{
    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    [JsonPropertyName("width")]
    public int Width { get; init; }

    [JsonPropertyName("height")]
    public int Height { get; init; }

    [JsonPropertyName("size")]
    public long Size { get; init; }
}