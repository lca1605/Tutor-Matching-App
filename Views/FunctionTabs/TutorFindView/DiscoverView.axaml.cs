using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MyApp.Views;

public partial class DiscoverView : UserControl
{
    public DiscoverView()
    {
        InitializeComponent();
        CardHost.Content = new TutorCardView();
        CardCountText.Text = "Gợi ý hôm nay";

        // TODO: Load danh sách gia sư từ DB/service
    }

    private void OnFilterClick(object? sender, RoutedEventArgs e)
    {
        FilterHost.Content      = new FilterPopupView(OnFilterApplied, OnFilterClosed);
        FilterOverlay.IsVisible = true;
    }

    private void OnFilterApplied(FilterOptions options)
    {
        FilterOverlay.IsVisible = false;
        // TODO: reload danh sách theo filter
    }

    private void OnFilterClosed()
        => FilterOverlay.IsVisible = false;
}