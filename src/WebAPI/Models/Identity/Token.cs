namespace WebAPI.Models.Identity;

public class Token
{
    [JsonPropertyName("refreshToken")]
    [Required]
    public required string RefreshToken { get; set; }
}