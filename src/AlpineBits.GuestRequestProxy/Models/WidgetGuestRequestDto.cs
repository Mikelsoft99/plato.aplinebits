using System.ComponentModel.DataAnnotations;

namespace AlpineBits.GuestRequestProxy.Models;

public sealed class WidgetGuestRequestDto
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    [Required]
    public DateOnly ArrivalDate { get; set; }

    [Required]
    public DateOnly DepartureDate { get; set; }

    public string? RoomCategory { get; set; }

    [Required]
    [MinLength(1)]
    public int[] PersonAges { get; set; } = [];

    public string? Remarks { get; set; }

    public bool MarketingConsent { get; set; }
}
