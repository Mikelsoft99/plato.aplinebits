# AlpineBits GuestRequest Proxy (C#)

Kleiner ASP.NET Core Server mit **einem POST-Endpunkt** (`/alpinebits`), der AlpineBits GuestRequest XML entgegennimmt und an ein ASA-System weiterleitet.

## Struktur

- `src/AlpineBits.GuestRequestProxy/Program.cs` – DI, HTTP-Client, Controller-Mapping
- `src/AlpineBits.GuestRequestProxy/Controllers/AlpineBitsController.cs` – Single POST Endpoint
- `src/AlpineBits.GuestRequestProxy/Services/AsaClient.cs` – Forwarding zum ASA inklusive AlpineBits Header
- `src/AlpineBits.GuestRequestProxy/Models/GuestRequestModels.cs` – XML-Entities (`OTA_HotelGuestRequestsNotifRQ/RS`)
- `src/AlpineBits.GuestRequestProxy/Options/AlpineBitsOptions.cs` – Konfiguration

## Konfiguration

`appsettings.json`:

```json
"AlpineBits": {
  "TargetUrl": "https://asa.example.com/alpinebits",
  "Version": "2024-10",
  "Action": "GuestRequests",
  "Username": "",
  "Password": ""
}
```

## Verhalten des Endpunkts

1. Liest XML aus dem Request-Body.
2. Validiert, ob `OTA_HotelGuestRequestsNotifRQ` deserialisierbar ist.
3. Sendet den Original-Payload an das ASA.
4. Übergibt den ASA-Statuscode und die ASA-Response unverändert zurück.

## Start

```bash
dotnet run --project src/AlpineBits.GuestRequestProxy/AlpineBits.GuestRequestProxy.csproj
```
