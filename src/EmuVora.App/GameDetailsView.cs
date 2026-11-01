using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using EmuVora.Core;

namespace EmuVora.App;

public sealed partial class MainWindow
{
    private readonly ScrollViewer gameDetailsScroll = new();
    private readonly TextBlock technicalPath = new() { TextWrapping = TextWrapping.Wrap, FontSize = 12, Opacity = .75 };
    private readonly Expander technicalDetails = new() { Header = "Technische Angaben", IsExpanded = false, HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly Button quickPlay = new() { Content = "▶ Spielen", MinWidth = 140, IsEnabled = false };
    private readonly TextBlock selectedGameSummary = new() { Text = "Spiel auswählen", VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly Grid compactGameActions = new() { ColumnDefinitions = new("*,Auto"), ColumnSpacing = 12 };
    private Window? gameDetailsDialog;
    private bool CanShowDetailPane => !settings.TvMode && (ClientSize.Width == 0 || ClientSize.Width >= 1120);
    private void BuildGameDetails()
    {
        detailCover.Width = 160; detailCover.HorizontalAlignment = HorizontalAlignment.Center;
        favorite.HorizontalAlignment = chooseVersion.HorizontalAlignment = HorizontalAlignment.Stretch;
        var details = new StackPanel { Spacing = 12, Margin = new(22, 24) };
        details.Children.Add(new TextBlock { Text = "IM FOKUS", FontSize = 10, LetterSpacing = 2, Opacity = .6 });
        details.Children.Add(new Border { CornerRadius = new(16), Padding = new Thickness(10), Background = new SolidColorBrush(Color.Parse("#10203030")), Child = detailCover, HorizontalAlignment = HorizontalAlignment.Center });
        details.Children.Add(detailTitle); details.Children.Add(detailInfo);
        details.Children.Add(play); details.Children.Add(chooseVersion); details.Children.Add(favorite); details.Children.Add(collectionGame); details.Children.Add(manageCover);
        var technical = new StackPanel { Spacing = 10, IsVisible = false }; technical.Children.Add(new TextBlock { Text = "ROM-Datei", FontWeight = FontWeight.SemiBold }); technical.Children.Add(technicalPath); technical.Children.Add(displayStatus); technicalDetails.Content = technical;
        technicalDetails.PropertyChanged += (_, change) => { if (change.Property == Expander.IsExpandedProperty) technical.IsVisible = technicalDetails.IsExpanded; }; details.Children.Add(technicalDetails);
        var more = new StackPanel { Spacing = 8, IsVisible = false }; foreach (var button in new[] { cover, onlineCover, protectCover, edit, reveal, remove }) { button.HorizontalAlignment = HorizontalAlignment.Stretch; more.Children.Add(button); }
        var editing = new Expander { Header = "Spieldetails bearbeiten", Content = more, HorizontalAlignment = HorizontalAlignment.Stretch };
        editing.PropertyChanged += (_, change) => { if (change.Property == Expander.IsExpandedProperty) more.IsVisible = editing.IsExpanded; }; details.Children.Add(editing);
        gameDetailsScroll.Content = details; detailBorder.Child = gameDetailsScroll;
        compactGameActions.Children.Add(selectedGameSummary); Grid.SetColumn(quickPlay, 1); compactGameActions.Children.Add(quickPlay);
        quickPlay.Classes.Add("accent"); quickPlay.Click += async (_, _) => await PlayAsync();
    }
    private void ApplyGameDetailsLayout()
    {
        var show = gameDetailsDialog == null && !IsHome && CanShowDetailPane && settings.ShowGameDetails;
        detailBorder.IsVisible = show; layout.ColumnDefinitions[2].Width = new GridLength(show ? 300 : 0);
        compactGameActions.IsVisible = !IsHome && !show;
        if (detailsToggle != null)
        {
            detailsToggle.Content = CanShowDetailPane ? "Details" : "Spieldetails";
            detailsToggle.IsEnabled = !IsHome && (CanShowDetailPane || Selected != null && operation == null);
            ToolTip.SetTip(detailsToggle, CanShowDetailPane ? "Rechte Detailspalte ein- oder ausblenden." : "Spieldetails mit Spielen, Versionen und weiteren Aktionen öffnen.");
        }
        quickPlay.MinHeight = settings.TvMode ? 48 : 38; quickPlay.FontSize = selectedGameSummary.FontSize = settings.TvMode ? 18 : 14;
        technicalPath.FontSize = displayStatus.FontSize = settings.TvMode ? 16 : 12; detailInfo.FontSize = settings.TvMode ? 16 : 13;
    }
    private async Task ToggleGameDetailsAsync()
    {
        if (!CanShowDetailPane) { await ShowGameDetailsAsync(); return; }
        var previous = settings.ShowGameDetails; settings.ShowGameDetails = !previous;
        try { settingsStore.Save(settings); } catch { settings.ShowGameDetails = previous; throw; }
        ApplyLayout();
    }
    private async Task ShowGameDetailsAsync()
    {
        if (Selected == null || operation != null || isClosed) return;
        if (gameDetailsDialog is {} existing) { existing.Activate(); return; }
        detailBorder.Child = null;
        var body = new Grid { RowDefinitions = new("*,Auto") }; body.Children.Add(gameDetailsScroll);
        var dialog = FixedDialog("EmuVora · Spieldetails", body); dialog.Width = 680; dialog.Height = 780; gameDetailsDialog = dialog;
        foreach (var key in new[] { "EmuAccent", "EmuSelection", "EmuSurface", "EmuText", "AccentButtonBackground", "AccentButtonBackgroundPointerOver", "AccentButtonBackgroundPressed", "AccentButtonForeground", "AccentButtonForegroundPointerOver", "AccentButtonForegroundPressed" }) dialog.Resources[key] = Resources[key];
        var close = ActionButton("Zur Bibliothek", () => dialog.Close()); close.Margin = new(24, 8, 24, 16); Grid.SetRow(close, 1); body.Children.Add(close);
        try { await dialog.ShowDialog(this); }
        finally
        {
            body.Children.Remove(gameDetailsScroll); gameDetailsDialog = null;
            if (!isClosed) { detailBorder.Child = gameDetailsScroll; controllerUiMode = false; ApplyLayout(); gamesList.Focus(); }
        }
    }
    private void UpdateGameDetailsActions(Game? game)
    {
        quickPlay.IsEnabled = play.IsEnabled;
        selectedGameSummary.Text = game == null ? "Spiel auswählen" : game.Title + " · " + game.System;
        ToolTip.SetTip(selectedGameSummary, game?.Title); ToolTip.SetTip(quickPlay, game == null ? "Zuerst ein Spiel auswählen." : game.Title + " starten");
        technicalPath.Text = game?.Path ?? "Keine Datei ausgewählt.";
        technicalDetails.IsEnabled = game != null; ApplyGameDetailsLayout();
    }
}
