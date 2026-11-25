using HotelReservationSystem.Features.Admin;
using HotelReservationSystem.Features.Auth;
using HotelReservationSystem.Features.RoomSearch;
using HotelReservationSystem.Features.Rooms;
using HotelReservationSystem.Features.Staff;
using Microsoft.Extensions.Logging;

namespace HotelReservationSystem
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Services (swap MockRoomService for the API-backed implementation later)
            builder.Services.AddSingleton<IRoomService, MockRoomService>();

            // Auth (swap MockAuthService for the API-backed implementation later)
            builder.Services.AddSingleton<MockUserStore>();
            builder.Services.AddSingleton<IAuthService, MockAuthService>();
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<LoginPage>();

            // Room search feature
            builder.Services.AddTransient<RoomSearchViewModel>();
            builder.Services.AddTransient<RoomSearchPage>();

            // Staff dashboard
            builder.Services.AddTransient<StaffDashboardViewModel>();
            builder.Services.AddTransient<StaffDashboardPage>();

            // Admin user management
            builder.Services.AddSingleton<IUserAdminService, MockUserAdminService>();
            builder.Services.AddTransient<AdminUsersViewModel>();
            builder.Services.AddTransient<AdminUsersPage>();

#if WINDOWS
            // Remove the native WinUI input chrome so our rounded Border fields are the only outline.
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoChrome", (handler, _) =>
                handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0));
            Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping("NoChrome", (handler, _) =>
                handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0));
            Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("NoChrome", (handler, _) =>
                handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0));
#endif

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
