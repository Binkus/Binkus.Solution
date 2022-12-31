using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Binkus.DependencyInjection;
using DDS.Avalonia.Services;
using DDS.Avalonia.Views;
using DDS.Core;
using DDS.Core.Controls;
using DDS.Core.ViewModels;

namespace DDS.Avalonia.Desktop.Controls;

public class DialogAlertMessageBox : AbstractAsyncDialogAlertMessageBox, IDialogAlertMessageBox
{
    private IServiceProvider Services { get; }

    public DialogAlertMessageBox(IServiceProvider services)
    {
        Services = services;
    }
    
    public override async Task ShowAsync(IDialogAlertMessageBox.DialogConfigBuilder dialogConfig)
    {
        if (Globals.IsDesignMode) return;
        
        var vm = new DialogViewModel(Services, dialogConfig);
        var topLevelWindow = await vm.GetRequiredService<TopLevelService>().CurrentWindow();
        // var dialogWindow = new DialogWindow(topLevelWindow) { DataContext = vm};
        var dialogWindow = new DialogWindow() { DataContext = vm};

        
        SetDialogWindowProperties(dialogWindow, topLevelWindow);
        vm = await dialogWindow.ShowDialog<DialogViewModel?>(topLevelWindow);
        // SetWindowStartupLocationWorkaround(dialogWindow);
    }
    
    private const double Height = 150;
    private const double Width = 400;

    private void SetDialogWindowProperties(DialogWindow dialog, Window topLevel)
    {
        dialog.Height = Height;
        dialog.Width= Width;

        dialog.CanResize = false;
        // dialog.SystemDecorations = SystemDecorations.None;
    }

    
}