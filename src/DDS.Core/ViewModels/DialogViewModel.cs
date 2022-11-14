using DDS.Core.Controls;

namespace DDS.Core.ViewModels;

public sealed partial class DialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private IDialogAlertMessageBox.DialogConfigBuilder _dialogConfig;

    public DialogViewModel(IServiceProvider services, IDialogAlertMessageBox.DialogConfigBuilder dialogConfig) 
        : base(services) =>
        _dialogConfig = dialogConfig;

    public DialogViewModel(IDialogAlertMessageBox.DialogConfigBuilder dialogConfig) => _dialogConfig = dialogConfig;

    [ActivatorUtilitiesConstructor]
    public DialogViewModel(IServiceProvider services) : base(services) =>
        _dialogConfig = new IDialogAlertMessageBox.DialogConfigBuilder();
    
    public DialogViewModel() => _dialogConfig = new IDialogAlertMessageBox.DialogConfigBuilder()
        .SetButton2Text("Dummy2").SetButton3Text("Dummy3").SetMessage("This is a dummy message for Designer.")
        .SetTitle("Designer does not show the title");

    [RelayCommand] private void Button1(ICoreWindowFor<DialogViewModel> senderWindow) 
        => DoAfter(config => config.Button1Action, senderWindow);

    [RelayCommand] private void Button2(ICoreWindowFor<DialogViewModel> senderWindow)
        => DoAfter(config => config.Button2Action, senderWindow);

    [RelayCommand] private void Button3(ICoreWindowFor<DialogViewModel> senderWindow) 
        => DoAfter(config => config.Button3Action, senderWindow);

    private void DoAfter(Func<IDialogAlertMessageBox.DialogConfigBuilder, Action?> f, ICoreWindowFor<DialogViewModel> senderWindow)
    {
        var action = f.Invoke(DialogConfig);
        action?.Invoke();
        senderWindow.Close();
    }
}