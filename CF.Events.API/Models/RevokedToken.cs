using System.ComponentModel.DataAnnotations;

namespace CF.Events.API.Models;

public class RevokedToken
{
    [Key]
    [StringLength(4000)]
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiryDate { get; init; }
}
