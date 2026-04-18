using System.Text.Json.Serialization;

namespace MovieCatalog.Dtos;

public class MovieDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("posterUrl")]
    public string? PosterUrl { get; set; }

    [JsonPropertyName("trailerLink")]
    public string? TrailerLink { get; set; }

    [JsonPropertyName("isWatched")]
    public bool? IsWatched { get; set; }
}
