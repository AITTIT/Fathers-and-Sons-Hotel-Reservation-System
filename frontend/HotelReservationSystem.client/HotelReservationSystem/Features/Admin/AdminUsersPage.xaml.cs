namespace HotelReservationSystem.Features.Admin;

public partial class AdminUsersPage : ContentPage
{
    private readonly AdminUsersViewModel _viewModel;

    public AdminUsersPage(AdminUsersViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.ActivateAsync();
    }

    private async void OnResetPassword(object? sender, EventArgs e)
    {
        if (sender is not BindableObject { BindingContext: UserRowViewModel row })
            return;

        var password = await DisplayPromptAsync("Reset password",
            $"Enter a temporary password for {row.Name} (at least 8 characters).",
            "Reset", "Cancel", maxLength: 72);
        if (password is not null)
            await _viewModel.ResetPasswordAsync(row, password);
    }

    private async void OnRemove(object? sender, EventArgs e)
    {
        if (sender is not BindableObject { BindingContext: UserRowViewModel row })
            return;

        if (await DisplayAlert("Remove user", $"Remove {row.Name} ({row.Email})? This can't be undone.",
                "Remove", "Keep"))
            await _viewModel.RemoveAsync(row);
    }
}
