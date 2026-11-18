namespace HotelReservationSystem
{
    public partial class App : Application
    {
        private readonly IServiceProvider _services;

        public App(IServiceProvider services)
        {
            InitializeComponent();
            _services = services;

            // The design is light-only for now; the template's default styles would otherwise flip to dark.
            UserAppTheme = AppTheme.Light;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell(_services.GetRequiredService<Features.Auth.IAuthService>()));
        }
    }
}
