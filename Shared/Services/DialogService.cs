using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform.Storage;

namespace GestionCommerciale.Shared.Services;

public sealed class DialogService : IDialogService
{
    private static Window? GetMainWindow() =>
        Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime d
            ? d.MainWindow
            : null;

    public async Task ShowInfoAsync(string title, string message, CancellationToken cancellationToken = default, int autoCloseMs = 0)
    {
        var owner = GetMainWindow();
        var w = new Window
        {
            Title = title,
            MinWidth = 260,
            MaxWidth = 440,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var panel = new StackPanel { Margin = new Avalonia.Thickness(16), Spacing = 12 };
        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MaxWidth = 400
        });

        var ok = new Button { Content = "OK", IsDefault = true, HorizontalAlignment = HorizontalAlignment.Right };
        ok.Click += (_, _) => w.Close();
        panel.Children.Add(ok);
        w.Content = panel;

        if (autoCloseMs > 0)
            _ = Task.Delay(autoCloseMs, cancellationToken).ContinueWith(_ => w.Close(), TaskScheduler.FromCurrentSynchronizationContext());

        if (owner != null)
            await w.ShowDialog(owner);
        else
            w.Show();
    }

    public Task ShowErrorAsync(string title, string message, CancellationToken cancellationToken = default) =>
        ShowInfoAsync(title, message, cancellationToken);

    public async Task<bool> ConfirmAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        var owner = GetMainWindow();
        var w = new Window
        {
            Title = title,
            MinWidth = 360,
            MaxWidth = 520,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var confirmed = false;
        var panel = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 16 };
        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MaxWidth = 480,
            LineHeight = 22
        });

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };

        var no = new Button { Content = "Non" };
        no.Click += (_, _) =>
        {
            confirmed = false;
            w.Close();
        };
        var yes = new Button { Content = "Oui", IsDefault = true };
        yes.Click += (_, _) =>
        {
            confirmed = true;
            w.Close();
        };
        buttons.Children.Add(no);
        buttons.Children.Add(yes);
        panel.Children.Add(buttons);
        w.Content = panel;

        if (owner != null)
            await w.ShowDialog(owner);
        else
            w.Show();

        return confirmed;
    }

    public async Task<bool> ConfirmAvailabilityWarningAsync(
        AvailabilityWarningDialogModel model,
        CancellationToken cancellationToken = default)
    {
        var owner = GetMainWindow();
        var w = new Window
        {
            Title = model.Title,
            MinWidth = 420,
            Width = 480,
            MaxWidth = 560,
            MaxHeight = 640,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var confirmed = false;
        var root = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 14 };

        root.Children.Add(new TextBlock
        {
            Text = model.Header,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            FontSize = 15
        });

        root.Children.Add(CreateChip(
            model.PeriodText,
            bg: "#FEF3C7",
            border: "#FCD34D",
            fg: "#92400E",
            bold: true));

        var productsHost = new StackPanel { Spacing = 12 };
        foreach (var product in model.Products)
        {
            var card = new Border
            {
                Background = Avalonia.Media.Brush.Parse("#FFFBF5"),
                BorderBrush = Avalonia.Media.Brush.Parse("#E8DFD0"),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(10),
                Padding = new Avalonia.Thickness(12)
            };

            var cardBody = new StackPanel { Spacing = 10 };
            cardBody.Children.Add(new TextBlock
            {
                Text = product.ProductTitle,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            });

            var metrics = new WrapPanel { Orientation = Orientation.Horizontal };
            metrics.Children.Add(CreateLabeledChip(product.DemandeLabel, product.DemandeValue, "#FEE2E2", "#FECACA", "#991B1B"));
            metrics.Children.Add(CreateLabeledChip(product.DisponibleLabel, product.DisponibleValue, "#DCFCE7", "#86EFAC", "#166534"));
            metrics.Children.Add(CreateLabeledChip(product.StockLabel, product.StockValue, "#E0E7FF", "#C7D2FE", "#3730A3"));
            metrics.Children.Add(CreateLabeledChip(product.DejaLabel, product.DejaValue, "#FEF3C7", "#FCD34D", "#92400E"));
            cardBody.Children.Add(metrics);

            if (product.Conflicts.Count > 0)
            {
                cardBody.Children.Add(new TextBlock
                {
                    Text = product.ConflictsHeader ?? string.Empty,
                    FontSize = 12,
                    Opacity = 0.75,
                    Margin = new Avalonia.Thickness(0, 4, 0, 0)
                });

                var conflictsWrap = new WrapPanel { Orientation = Orientation.Horizontal };
                foreach (var conflict in product.Conflicts)
                {
                    conflictsWrap.Children.Add(CreateConflictChip(conflict.Title, conflict.Detail));
                }
                cardBody.Children.Add(conflictsWrap);
            }

            card.Child = cardBody;
            productsHost.Children.Add(card);
        }

        root.Children.Add(new ScrollViewer
        {
            Content = productsHost,
            MaxHeight = 420,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        });

        root.Children.Add(new TextBlock
        {
            Text = model.ConfirmQuestion,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            Margin = new Avalonia.Thickness(0, 4, 0, 0)
        });

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };
        var no = new Button { Content = model.NoLabel, MinWidth = 88 };
        no.Click += (_, _) =>
        {
            confirmed = false;
            w.Close();
        };
        var yes = new Button { Content = model.YesLabel, IsDefault = true, MinWidth = 88 };
        yes.Click += (_, _) =>
        {
            confirmed = true;
            w.Close();
        };
        buttons.Children.Add(no);
        buttons.Children.Add(yes);
        root.Children.Add(buttons);

        w.Content = root;

        if (owner != null)
            await w.ShowDialog(owner);
        else
            w.Show();

        return confirmed;
    }

    private static Border CreateChip(string text, string bg, string border, string fg, bool bold = false) =>
        new()
        {
            Background = Avalonia.Media.Brush.Parse(bg),
            BorderBrush = Avalonia.Media.Brush.Parse(border),
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(8),
            Padding = new Avalonia.Thickness(10, 6),
            Margin = new Avalonia.Thickness(0, 0, 8, 8),
            Child = new TextBlock
            {
                Text = text,
                Foreground = Avalonia.Media.Brush.Parse(fg),
                FontWeight = bold ? Avalonia.Media.FontWeight.SemiBold : Avalonia.Media.FontWeight.Normal,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

    private static Border CreateLabeledChip(string label, string value, string bg, string border, string fg)
    {
        var stack = new StackPanel { Spacing = 2 };
        stack.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 10,
            Opacity = 0.75,
            Foreground = Avalonia.Media.Brush.Parse(fg)
        });
        stack.Children.Add(new TextBlock
        {
            Text = value,
            FontSize = 14,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            Foreground = Avalonia.Media.Brush.Parse(fg)
        });

        return new Border
        {
            Background = Avalonia.Media.Brush.Parse(bg),
            BorderBrush = Avalonia.Media.Brush.Parse(border),
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(8),
            Padding = new Avalonia.Thickness(10, 6),
            Margin = new Avalonia.Thickness(0, 0, 8, 8),
            MinWidth = 96,
            Child = stack
        };
    }

    private static Border CreateConflictChip(string title, string detail)
    {
        var stack = new StackPanel { Spacing = 2 };
        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 12,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            Foreground = Avalonia.Media.Brush.Parse("#1E3A5F"),
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MaxWidth = 200
        });
        stack.Children.Add(new TextBlock
        {
            Text = detail,
            FontSize = 11,
            Opacity = 0.8,
            Foreground = Avalonia.Media.Brush.Parse("#1E3A5F")
        });

        return new Border
        {
            Background = Avalonia.Media.Brush.Parse("#EFF6FF"),
            BorderBrush = Avalonia.Media.Brush.Parse("#BFDBFE"),
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(8),
            Padding = new Avalonia.Thickness(10, 6),
            Margin = new Avalonia.Thickness(0, 0, 8, 8),
            Child = stack
        };
    }

    public async Task<string?> PromptPasswordAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        var owner = GetMainWindow();
        var w = new Window
        {
            Title = title,
            MinWidth = 300,
            MaxWidth = 460,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        string? password = null;
        var panel = new StackPanel { Margin = new Avalonia.Thickness(16), Spacing = 12 };
        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MaxWidth = 420
        });

        var input = new TextBox
        {
            PasswordChar = '*',
            MinWidth = 260
        };
        panel.Children.Add(input);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };
        var cancel = new Button { Content = "Annuler" };
        cancel.Click += (_, _) =>
        {
            password = null;
            w.Close();
        };
        var ok = new Button { Content = "Valider", IsDefault = true };
        ok.Click += (_, _) =>
        {
            password = input.Text;
            w.Close();
        };
        buttons.Children.Add(cancel);
        buttons.Children.Add(ok);
        panel.Children.Add(buttons);
        w.Content = panel;

        if (owner != null)
            await w.ShowDialog(owner);
        else
            w.Show();

        return password;
    }

    public async Task<string?> PromptLicenseAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        var owner = GetMainWindow();
        var w = new Window
        {
            Title = title,
            MinWidth = 320,
            MaxWidth = 480,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        string? licenseKey = null;
        var panel = new StackPanel { Margin = new Avalonia.Thickness(16), Spacing = 12 };
        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MaxWidth = 440
        });

        var input = new TextBox
        {
            MinWidth = 280,
            Watermark = "Clé de licence"
        };
        panel.Children.Add(input);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };
        var cancel = new Button { Content = "Quitter" };
        cancel.Click += (_, _) =>
        {
            licenseKey = null;
            w.Close();
        };
        var ok = new Button { Content = "Activer", IsDefault = true };
        ok.Click += (_, _) =>
        {
            licenseKey = input.Text;
            w.Close();
        };
        buttons.Children.Add(cancel);
        buttons.Children.Add(ok);
        panel.Children.Add(buttons);
        w.Content = panel;

        if (owner != null)
            await w.ShowDialog(owner);
        else
            w.Show();

        return licenseKey;
    }

    public async Task<string?> ShowPromptAsync(string title, string message, string? defaultValue = null, CancellationToken cancellationToken = default)
    {
        var owner = GetMainWindow();
        var w = new Window
        {
            Title = title,
            MinWidth = 300,
            MaxWidth = 460,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        string? result = null;
        var panel = new StackPanel { Margin = new Avalonia.Thickness(16), Spacing = 12 };
        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MaxWidth = 420
        });

        var input = new TextBox
        {
            MinWidth = 260,
            Text = defaultValue ?? string.Empty
        };
        panel.Children.Add(input);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };
        var cancel = new Button { Content = "Annuler" };
        cancel.Click += (_, _) =>
        {
            result = null;
            w.Close();
        };
        var ok = new Button { Content = "Valider", IsDefault = true };
        ok.Click += (_, _) =>
        {
            result = input.Text;
            w.Close();
        };
        buttons.Children.Add(cancel);
        buttons.Children.Add(ok);
        panel.Children.Add(buttons);
        w.Content = panel;

        if (owner != null)
            await w.ShowDialog(owner);
        else
            w.Show();

        return result;
    }

    public async Task<List<int>?> ShowBlPickerAsync(string title, IReadOnlyList<(int Id, string Numero, DateTime Date, string MontantLabel)> availableBls, CancellationToken cancellationToken = default)
    {
        var owner = GetMainWindow();
        var w = new Window
        {
            Title = title,
            MinWidth = 400,
            MaxWidth = 600,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        List<int>? result = null;
        var panel = new StackPanel { Margin = new Avalonia.Thickness(16), Spacing = 12 };

        var checkboxes = new List<CheckBox>();
        var listPanel = new StackPanel { Spacing = 4, Margin = new Avalonia.Thickness(0, 0, 0, 8) };
        foreach (var bl in availableBls)
        {
            var cb = new CheckBox
            {
                Content = $"{bl.Numero}  —  {bl.Date:d}  —  {bl.MontantLabel}",
                Tag = bl.Id
            };
            checkboxes.Add(cb);
            listPanel.Children.Add(cb);
        }

        var listHost = availableBls.Count > 8
            ? (Control)new ScrollViewer { Content = listPanel, MaxHeight = 320 }
            : listPanel;
        panel.Children.Add(listHost);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8 };
        var btnCancel = new Button { Content = "Annuler" };
        btnCancel.Click += (_, _) => w.Close();
        var btnAdd = new Button { Content = "Ajouter", IsDefault = true };
        btnAdd.Click += (_, _) =>
        {
            result = checkboxes.Where(cb => cb.IsChecked == true).Select(cb => (int)cb.Tag!).ToList();
            w.Close();
        };
        actions.Children.Add(btnCancel);
        actions.Children.Add(btnAdd);
        panel.Children.Add(actions);

        w.Content = panel;

        if (owner != null)
            await w.ShowDialog(owner);
        else
            w.Show();

        return result;
    }

    public Task<List<int>?> ShowBrPickerAsync(string title, IReadOnlyList<(int Id, string Numero, DateTime Date, string MontantLabel)> availableBrs, CancellationToken cancellationToken = default) =>
        ShowBlPickerAsync(title, availableBrs, cancellationToken);

    public async Task<(DateTime from, DateTime to)?> PickDateRangeAsync(string title, CancellationToken cancellationToken = default)
    {
        var owner = GetMainWindow();
        var w = new Window
        {
            Title = title,
            MinWidth = 360,
            MaxWidth = 520,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        (DateTime from, DateTime to)? result = null;
        var panel = new StackPanel { Margin = new Avalonia.Thickness(16), Spacing = 12 };

        var presets = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Center };

        static CalendarDatePicker CreateCalendar() => new()
        {
            MinWidth = 200,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            SelectedDateFormat = CalendarDatePickerFormat.Custom,
            CustomDateFormatString = "dd/MM/yyyy",
            Watermark = "jj/mm/aaaa",
            IsTodayHighlighted = true,
        };

        var dpFrom = CreateCalendar();
        var dpTo = CreateCalendar();

        void SetRange(DateTime from, DateTime to)
        {
            dpFrom.SelectedDate = from.Date;
            dpTo.SelectedDate = to.Date;
        }

        var btnToday = new Button { Content = "Aujourd'hui" };
        btnToday.Click += (_, _) =>
        {
            var (from, to) = Helpers.DateRangePresets.GetRange(Helpers.DateRangePreset.Today);
            SetRange(from, to);
        };

        var btnThisWeek = new Button { Content = "Cette semaine" };
        btnThisWeek.Click += (_, _) =>
        {
            var (from, to) = Helpers.DateRangePresets.GetRange(Helpers.DateRangePreset.Week);
            SetRange(from, to);
        };

        var btnThisMonth = new Button { Content = "Ce mois" };
        btnThisMonth.Click += (_, _) =>
        {
            var (from, to) = Helpers.DateRangePresets.GetRange(Helpers.DateRangePreset.Month);
            SetRange(from, to);
        };

        presets.Children.Add(btnToday);
        presets.Children.Add(btnThisWeek);
        presets.Children.Add(btnThisMonth);
        panel.Children.Add(presets);

        var dateGrid = new StackPanel { Spacing = 10 };
        var fromRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        fromRow.Children.Add(new TextBlock { Text = "Du:", VerticalAlignment = VerticalAlignment.Center, MinWidth = 30 });
        fromRow.Children.Add(dpFrom);
        dateGrid.Children.Add(fromRow);

        var toRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        toRow.Children.Add(new TextBlock { Text = "Au:", VerticalAlignment = VerticalAlignment.Center, MinWidth = 30 });
        toRow.Children.Add(dpTo);
        dateGrid.Children.Add(toRow);
        panel.Children.Add(dateGrid);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8 };
        var btnClear = new Button { Content = "Effacer" };
        btnClear.Click += (_, _) =>
        {
            result = (DateTime.MinValue, DateTime.MinValue);
            w.Close();
        };
        var btnCancel = new Button { Content = "Annuler" };
        btnCancel.Click += (_, _) => w.Close();
        var btnApply = new Button { Content = "Appliquer", IsDefault = true };
        btnApply.Click += (_, _) =>
        {
            if (!dpFrom.SelectedDate.HasValue && !dpTo.SelectedDate.HasValue)
                result = (DateTime.MinValue, DateTime.MinValue);
            else if (dpFrom.SelectedDate.HasValue && dpTo.SelectedDate.HasValue)
            {
                var from = dpFrom.SelectedDate.Value.Date;
                var to = dpTo.SelectedDate.Value.Date;
                result = from <= to ? (from, to) : (to, from);
            }
            w.Close();
        };
        actions.Children.Add(btnClear);
        actions.Children.Add(btnCancel);
        actions.Children.Add(btnApply);
        panel.Children.Add(actions);

        w.Content = panel;

        if (owner != null)
            await w.ShowDialog(owner);
        else
            w.Show();

        return result;
    }

    public async Task<string?> PickOpenFileAsync(string title, IReadOnlyList<string> patterns, CancellationToken cancellationToken = default)
    {
        var owner = GetMainWindow();
        if (owner?.StorageProvider is not { } sp) return null;

        var filters = new List<FilePickerFileType>
        {
            new(title) { Patterns = patterns.ToList() }
        };

        var result = await sp.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = filters
        });

        return result.Count > 0 ? result[0].TryGetLocalPath() : null;
    }

    public async Task<string?> PickFolderAsync(CancellationToken cancellationToken = default)
    {
        var owner = GetMainWindow();
        if (owner?.StorageProvider is not { } sp) return null;

        var result = await sp.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false
        });

        return result.Count > 0 ? result[0].TryGetLocalPath() : null;
    }

    public async Task<string?> PickSaveFileAsync(string title, string suggestedFileName, IReadOnlyList<string> patterns, CancellationToken cancellationToken = default)
    {
        var owner = GetMainWindow();
        if (owner?.StorageProvider is not { } sp) return null;

        var file = await sp.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedFileName,
            FileTypeChoices = new List<FilePickerFileType>
            {
                new(title) { Patterns = patterns.ToList() }
            }
        });

        return file?.TryGetLocalPath();
    }

    public async Task<bool> SavePickedFileBytesAsync(string title, string suggestedFileName, IReadOnlyList<string> patterns, byte[] content, CancellationToken cancellationToken = default)
    {
        var owner = GetMainWindow();
        if (owner?.StorageProvider is not { } sp) return false;

        var file = await sp.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedFileName,
            FileTypeChoices = new List<FilePickerFileType>
            {
                new(title) { Patterns = patterns.ToList() }
            }
        });

        if (file == null) return false;

        await using var stream = await file.OpenWriteAsync();
        await stream.WriteAsync(content, cancellationToken);
        await stream.FlushAsync(cancellationToken);
        return true;
    }
}
