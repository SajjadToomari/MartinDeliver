namespace WebAPI.Models.Identity;

public class UserSeed
{
    public required string UsernameDelivery { get; set; }
    public required string PasswordDelivery { get; set; }
    public required string UsernameB2B { get; set; }
    public required string PasswordB2B { get; set; }
}