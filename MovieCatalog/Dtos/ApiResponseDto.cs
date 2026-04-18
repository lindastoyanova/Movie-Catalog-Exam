using System.Text.Json.Serialization;

namespace MovieCatalog.Dtos;

public class ApiResponseDto
{
    [JsonPropertyName("msg")]
    public string Msg { get; set; } = string.Empty;

    [JsonPropertyName("movie")]
    public MovieDto? Movie { get; set; }
}
