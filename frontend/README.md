# MAUI client

The client uses .NET 9 MAUI and CommunityToolkit.Mvvm. Authentication, room and
user-administration data currently use mock services, so the backend is not
required to try the UI.

Demo logins (password `Password1`):

| Email | Role |
| --- | --- |
| `guest@example.com` | Customer |
| `staff@example.com` | Staff |
| `admin@example.com` | Admin |

## Run

Install the .NET 9 SDK and the MAUI workload (`dotnet workload install maui`),
along with the platform prerequisites for your target device. Open
`HotelReservationSystem.client/HotelReservationSystem.sln` in Visual Studio or
VS Code with the .NET MAUI extension, select the client project and a device or
emulator, then start debugging.

For platform setup instructions, see the
[MAUI installation guide](https://learn.microsoft.com/dotnet/maui/get-started/installation).