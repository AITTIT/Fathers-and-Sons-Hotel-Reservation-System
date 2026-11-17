namespace HotelReservationSystem
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // The permanent sidebar is a desktop layout. On phones fall back to a slide-out menu
            // (the spec's bottom TabBar for mobile is still to do).
            if (DeviceInfo.Idiom == DeviceIdiom.Phone)
                FlyoutBehavior = FlyoutBehavior.Flyout;
        }
    }
}
