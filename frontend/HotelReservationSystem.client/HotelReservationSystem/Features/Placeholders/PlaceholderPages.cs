namespace HotelReservationSystem.Features.Placeholders;

/// <summary>Stand-in for screens that are specified but not built yet.</summary>
public abstract class PlaceholderPage : ContentPage
{
    protected PlaceholderPage(string title)
    {
        Title = title;
        Shell.SetNavBarIsVisible(this, false);
        BackgroundColor = (Color)Application.Current!.Resources["PageBackground"];
        Content = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            Spacing = 8,
            Children =
            {
                new Label
                {
                    Text = title,
                    FontSize = 32,
                    FontFamily = "OpenSansSemibold",
                    TextColor = (Color)Application.Current.Resources["Ink"],
                    HorizontalTextAlignment = TextAlignment.Center,
                },
                new Label
                {
                    Text = "Coming soon.",
                    TextColor = (Color)Application.Current.Resources["Muted"],
                    HorizontalTextAlignment = TextAlignment.Center,
                },
            },
        };
    }
}

public sealed class MyBookingsPage() : PlaceholderPage("My bookings");

public sealed class HelpPage() : PlaceholderPage("Help & FAQ");
