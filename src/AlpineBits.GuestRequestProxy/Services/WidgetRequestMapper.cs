using AlpineBits.GuestRequestProxy.Models;

namespace AlpineBits.GuestRequestProxy.Services;

public interface IWidgetRequestMapper
{
    OtaHotelGuestRequestsNotifRq Map(WidgetGuestRequestDto request, string hotelCode);
}

public sealed class WidgetRequestMapper : IWidgetRequestMapper
{
    public OtaHotelGuestRequestsNotifRq Map(WidgetGuestRequestDto request, string hotelCode)
    {
        var uniqueId = Guid.NewGuid().ToString("N");
        var adults = request.PersonAges.Count(x => x >= 18);
        var childrenAges = request.PersonAges.Where(x => x < 18).ToArray();

        var details = $"Email: {request.Email}; Phone: {request.Phone ?? "-"}; Arrival: {request.ArrivalDate:yyyy-MM-dd}; Departure: {request.DepartureDate:yyyy-MM-dd}; " +
            $"RoomCategory: {request.RoomCategory ?? "-"}; Adults: {adults}; ChildrenAges: [{string.Join(',', childrenAges)}]; " +
            $"MarketingConsent: {request.MarketingConsent}; Remarks: {request.Remarks ?? "-"}";

        return new OtaHotelGuestRequestsNotifRq
        {
            EchoToken = uniqueId,
            TimeStamp = DateTimeOffset.UtcNow.ToString("O"),
            Version = "1.0",
            HotelReservations = new HotelReservations
            {
                Items =
                [
                    new HotelReservation
                    {
                        UniqueId = new UniqueId { Type = "14", Id = uniqueId },
                        ResGuests =
                        [
                            new ResGuest
                            {
                                Profiles = new Profiles
                                {
                                    Items =
                                    [
                                        new ProfileInfo
                                        {
                                            Profile = new Profile
                                            {
                                                Customer = new Customer
                                                {
                                                    PersonName = new PersonName
                                                    {
                                                        GivenName = request.FirstName,
                                                        Surname = request.LastName
                                                    }
                                                }
                                            }
                                        }
                                    ]
                                },
                                GuestRequests = new GuestRequests
                                {
                                    Items =
                                    [
                                        new GuestRequest
                                        {
                                            Code = "WidgetRequest",
                                            Type = "18",
                                            Quantity = 1,
                                            Text = details
                                        }
                                    ]
                                }
                            }
                        ]
                    }
                ]
            }
        };
    }
}
