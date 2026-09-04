namespace CF.Events.Web.Models;

public class DonationDetails
{
    public required string EventName { get; set; }
    public string? PhysicalGiftInfo { get; set; }
    public string? Iban { get; set; }
    public string? Link { get; set; }
    public string? Reference { get; set; }
}
