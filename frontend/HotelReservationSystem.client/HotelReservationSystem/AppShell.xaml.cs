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
            UserLabel.Text = user is null ? "Browsing as guest" : $"Signed in as {user.DisplayName}";
            AuthButton.Text = user is null ? "Sign in or register" : "Sign out";
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
