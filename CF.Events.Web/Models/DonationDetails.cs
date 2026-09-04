namespace CF.Events.Web.Models;

public class DonationDetails
{
    public required string EventName { get; set; }
    public bool AllowPhysicalGifts { get; set; }
    public string? Iban { get; set; }
    public string? Link { get; set; }
    public string? Reference { get; set; }
}
