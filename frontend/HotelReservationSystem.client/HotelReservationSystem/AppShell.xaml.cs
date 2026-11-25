using HotelReservationSystem.Features.Auth;

namespace HotelReservationSystem
{
    public partial class AppShell : Shell
    {
        private readonly IAuthService _auth;

        public AppShell(IAuthService auth)
        {
            InitializeComponent();
            _auth = auth;

            Routing.RegisterRoute("login", typeof(LoginPage));
            _auth.CurrentUserChanged += (_, _) => RefreshAuthFooter();
            RefreshAuthFooter();

            // The permanent sidebar is a desktop layout. On phones fall back to a slide-out menu
            // (the spec's bottom TabBar for mobile is still to do).
            if (DeviceInfo.Idiom == DeviceIdiom.Phone)
                FlyoutBehavior = FlyoutBehavior.Flyout;
        }

        private void RefreshAuthFooter()
        {
            var user = _auth.CurrentUser;
            var isStaff = user?.IsStaff == true;
            var isAdmin = user?.IsAdmin == true;

            UserLabel.Text = user is null ? "Browsing as guest"
                : isAdmin ? $"{user.DisplayName}\nAdministrator"
                : $"Signed in as {user.DisplayName}";
            AuthButton.Text = user is null ? "Sign in or register" : "Sign out";
            AdminBadge.IsVisible = isAdmin;

            (FlyoutItem Item, bool Visible)[] menu =
            [
                (StaffDashboardItem, isStaff),
                (StaffReservationsItem, isStaff),
                (StaffRoomsItem, isStaff),
                (StaffCustomersItem, isStaff),
                (AdminUsersItem, isAdmin),
                (AdminLogsItem, isAdmin),
                (AdminAuditItem, isAdmin),
                (CustomerRoomsItem, !isStaff && !isAdmin),
                (CustomerBookingsItem, !isStaff && !isAdmin),
                (CustomerHelpItem, !isStaff && !isAdmin),
            ];

            // Order matters: Shell crashes if the current item is hidden and it jumps to one that is then
            // hidden too. So show the new items, move to one of them, and only then hide the rest.
            foreach (var (item, visible) in menu)
                if (visible)
                    item.IsVisible = true;

            if (menu.Any(m => !m.Visible && m.Item == CurrentItem))
                CurrentItem = menu.First(m => m.Visible).Item;

            foreach (var (item, visible) in menu)
                if (!visible)
                    item.IsVisible = false;

            // On sign out, return to the customer start page (sign-in navigation is handled by LoginPage).
            if (user is null && Current is not null)
                Dispatcher.Dispatch(async () => await GoToAsync("//rooms"));
        }

        private async void OnAuthButtonClicked(object? sender, EventArgs e)
        {
            if (_auth.CurrentUser is null)
                await GoToAsync("login");
            else
                await _auth.LogoutAsync();
        }
    }
}
