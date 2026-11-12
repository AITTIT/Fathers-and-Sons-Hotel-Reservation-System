namespace HotelReservationSystem
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // The design is light-only for now; the template's default styles would otherwise flip to dark.
            UserAppTheme = AppTheme.Light;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
