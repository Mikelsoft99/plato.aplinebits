using System.Xml.Serialization;

namespace AlpineBits.GuestRequestProxy.Models;

[XmlRoot("OTA_HotelGuestRequestsNotifRQ", Namespace = "http://www.opentravel.org/OTA/2003/05")]
public class OtaHotelGuestRequestsNotifRq
{
    [XmlAttribute("EchoToken")]
    public string? EchoToken { get; set; }

    [XmlAttribute("TimeStamp")]
    public string? TimeStamp { get; set; }

    [XmlAttribute("Version")]
    public string? Version { get; set; }

    [XmlElement("HotelReservations")]
    public HotelReservations? HotelReservations { get; set; }
}

public class HotelReservations
{
    [XmlElement("HotelReservation")]
    public List<HotelReservation> Items { get; set; } = new();
}

public class HotelReservation
{
    [XmlElement("UniqueID")]
    public UniqueId? UniqueId { get; set; }

    [XmlArray("ResGuests")]
    [XmlArrayItem("ResGuest")]
    public List<ResGuest> ResGuests { get; set; } = new();
}

public class UniqueId
{
    [XmlAttribute("Type")]
    public string? Type { get; set; }

    [XmlAttribute("ID")]
    public string? Id { get; set; }
}

public class ResGuest
{
    [XmlElement("Profiles")]
    public Profiles? Profiles { get; set; }

    [XmlElement("GuestRequests")]
    public GuestRequests? GuestRequests { get; set; }
}

public class Profiles
{
    [XmlElement("ProfileInfo")]
    public List<ProfileInfo> Items { get; set; } = new();
}

public class ProfileInfo
{
    [XmlElement("Profile")]
    public Profile? Profile { get; set; }
}

public class Profile
{
    [XmlElement("Customer")]
    public Customer? Customer { get; set; }
}

public class Customer
{
    [XmlElement("PersonName")]
    public PersonName? PersonName { get; set; }
}

public class PersonName
{
    [XmlElement("GivenName")]
    public string? GivenName { get; set; }

    [XmlElement("Surname")]
    public string? Surname { get; set; }
}

public class GuestRequests
{
    [XmlElement("GuestRequest")]
    public List<GuestRequest> Items { get; set; } = new();
}

public class GuestRequest
{
    [XmlAttribute("Code")]
    public string? Code { get; set; }

    [XmlAttribute("Quantity")]
    public int Quantity { get; set; } = 1;

    [XmlAttribute("Type")]
    public string? Type { get; set; }

    [XmlElement("Text")]
    public string? Text { get; set; }
}

[XmlRoot("OTA_HotelGuestRequestsNotifRS", Namespace = "http://www.opentravel.org/OTA/2003/05")]
public class OtaHotelGuestRequestsNotifRs
{
    [XmlAttribute("TimeStamp")]
    public string? TimeStamp { get; set; }

    [XmlAttribute("Version")]
    public string? Version { get; set; }

    [XmlElement("Success")]
    public Success? Success { get; set; }

    [XmlElement("Errors")]
    public Errors? Errors { get; set; }
}

public class Success;

public class Errors
{
    [XmlElement("Error")]
    public List<ErrorItem> Items { get; set; } = new();
}

public class ErrorItem
{
    [XmlAttribute("Type")]
    public string? Type { get; set; }

    [XmlAttribute("Code")]
    public string? Code { get; set; }

    [XmlText]
    public string? Value { get; set; }
}
