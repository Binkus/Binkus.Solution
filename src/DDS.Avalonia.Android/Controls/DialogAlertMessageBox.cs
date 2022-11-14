// using AndroidX.AppCompat.App;
using DDS.Core.Controls;
// using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
using AlertDialog = Android.App.AlertDialog;

namespace DDS.Avalonia.Android.Controls;

public sealed class DialogAlertMessageBox : AbstractDialogAlertMessageBox, IDialogAlertMessageBox
{
    public static void ShowTest()
    {
        // var a = new AlertDialog(default, true, default!);
        AlertDialog.Builder dialog = new AlertDialog.Builder(MainActivity.CurrentMainActivity);
        AlertDialog alert = dialog.Create() ?? throw new NullReferenceException();  
        alert.SetTitle("Title");  
        alert.SetMessage("Complex Alert");  
        // alert.SetIcon(Resource.Drawable.alert);  
        alert.SetButton("OK", (c, ev) =>  
        {  
            // Ok button click task  
        });  
        alert.SetButton2("NOPE", (c, ev) =>
        {
            
        });
        alert.SetButton3("CANCEL", (c, ev) =>
        {
            
        });
        alert.Show(); 
    }

    public override void Show(IDialogAlertMessageBox.DialogConfigBuilder dialogConfig)
    {
        var config = dialogConfig;
        
        AlertDialog.Builder dialog = new AlertDialog.Builder(MainActivity.CurrentMainActivity);
        AlertDialog alert = dialog.Create() ?? throw new NullReferenceException();  
        alert.SetTitle(config.Title);  
        alert.SetMessage(config.Message);  
        // alert.SetIcon(Resource.Drawable.alert);
        
        alert.SetButton(config.Button1Text, (c, ev) =>  
        {  
            config.Button1Action?.Invoke();
        });
        
        if (config.Button2Text is not null)
            alert.SetButton2(config.Button2Text, (c, ev) =>
            {
                config.Button2Action?.Invoke();
            });            
        
        
        if (config.Button3Text is not null)
            alert.SetButton3(config.Button3Text, (c, ev) =>
            {
                config.Button3Action?.Invoke();
            });
        
        alert.Show();
    }

    // public Task ShowAsync(IDialogAlertMessageBox.DialogConfigBuilder dialogConfig)
    // {
    //     Show(dialogConfigBuilder);
    //     return Task.CompletedTask;
    // }
    //
    //
    // public void Show(Action<IDialogAlertMessageBox.DialogConfigBuilder> dialogConfigBuilder)
    // {
    //     var config = new IDialogAlertMessageBox.DialogConfigBuilder();
    //     dialogConfigBuilder.Invoke(config);
    //     Show(config);
    // }
    //
    //
    //
    // public Task ShowAsync(Action<IDialogAlertMessageBox.DialogConfigBuilder> dialogConfigBuilder)
    // {
    //     Show(dialogConfigBuilder);
    //     return Task.CompletedTask;
    // }
}