namespace WebAPI.Dto;

public class DeliveryRequestDto
{
    public string? ProviderName { get; set; }
    public string? ProviderMobile { get; set; }
    public string? RecipientName { get; set; }
    public string? RecipientMobile { get; set; }
    public string? PickupLocation { get; set; }
    public string? DestinationLocation { get; set; }
    public double PickupLongitude { get; set; }
    public double PickupLatitude { get; set; }
    public double DestinationLongitude { get; set; }
    public double DestinationLatitude { get; set; }
    public bool IsDeliveryDone { get; set; }
}
