/*
 * Base for this HamburgerMenu is from the ControlCatalog / SampleControls
 * from the main git repo of Avalonia project https://github.com/AvaloniaUI/Avalonia
 * 
 * https://github.com/AvaloniaUI/Avalonia/blob/master/samples/SampleControls/HamburgerMenu/HamburgerMenu.cs  -> this HamburgerMenu.axaml.cs
 * https://github.com/AvaloniaUI/Avalonia/blob/master/samples/SampleControls/HamburgerMenu/HamburgerMenu.xaml -> HamburgerMenu.axaml
 * (Exact commit used as base:)
 * https://github.com/AvaloniaUI/Avalonia/blob/f8706278a8506cefa0c34fefbb0caa27f9db6318/samples/SampleControls/HamburgerMenu/HamburgerMenu.xaml -> HamburgerMenu.axaml
 * https://github.com/AvaloniaUI/Avalonia/blob/f8706278a8506cefa0c34fefbb0caa27f9db6318/samples/SampleControls/HamburgerMenu/HamburgerMenu.cs -> this HamburgerMenu.axaml.cs
 *
 * Copy of MIT License of Avalonia available in $(REPOSITORY_ROOT)/.licenses/Avalonia/license.md
 */


using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

// ReSharper disable MemberCanBePrivate.Global

namespace DDS.Avalonia.Controls;

public partial class HamburgerMenu : TabControl
{
    private SplitView? _splitView;

    public static readonly StyledProperty<IBrush?> PaneBackgroundProperty =
        SplitView.PaneBackgroundProperty.AddOwner<HamburgerMenu>();

    public IBrush? PaneBackground
    {
        get => GetValue(PaneBackgroundProperty);
        set => SetValue(PaneBackgroundProperty, value);
    }

    public static readonly StyledProperty<IBrush?> ContentBackgroundProperty =
        AvaloniaProperty.Register<HamburgerMenu, IBrush?>(nameof(ContentBackground));

    public IBrush? ContentBackground
    {
        get => GetValue(ContentBackgroundProperty);
        set => SetValue(ContentBackgroundProperty, value);
    }

    public static readonly StyledProperty<int> ExpandedModeThresholdWidthProperty =
        AvaloniaProperty.Register<HamburgerMenu, int>(nameof(ExpandedModeThresholdWidth), 1008);

    public int ExpandedModeThresholdWidth
    {
        get => GetValue(ExpandedModeThresholdWidthProperty);
        set => SetValue(ExpandedModeThresholdWidthProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _splitView = e.NameScope.Find<SplitView>("PART_NavigationPane");
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property != BoundsProperty || _splitView is null) return;
        
        var (oldBounds, newBounds) = change.GetOldAndNewValue<Rect>();
        EnsureSplitViewMode(oldBounds, newBounds);
    }

    private void EnsureSplitViewMode(Rect oldBounds, Rect newBounds)
    {
        if (_splitView is null) return;
        
        var threshold = ExpandedModeThresholdWidth;

        if (newBounds.Width >= threshold && oldBounds.Width < threshold)
        {
            _splitView.DisplayMode = SplitViewDisplayMode.Inline;
            _splitView.IsPaneOpen = true;
        }
        else if (newBounds.Width < threshold && oldBounds.Width >= threshold)
        {
            _splitView.DisplayMode = SplitViewDisplayMode.Overlay;
            _splitView.IsPaneOpen = false;
        }
    }
}