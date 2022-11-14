namespace DDS.Core.Controls;

public interface IDialogAlertMessageBox
{
    void Show(DialogConfigBuilder dialogConfig);
    Task ShowAsync(DialogConfigBuilder dialogConfig);
    
    void Show(Action<DialogConfigBuilder> dialogConfigBuilder);
    Task ShowAsync(Action<DialogConfigBuilder> dialogConfigBuilder);

    //
    
    public class DialogConfigBuilder
    {
        public string Title { get; set; } = "";

        public string Message { get; set; } = "";
        
        public string Button1Text { get; set; } = "Ok";
        public string? Button2Text { get; set; }
        public string? Button3Text { get; set; }

        public Action? Button1Action { get; set; }
        public Action? Button2Action { get; set; }
        public Action? Button3Action { get; set; }
        
        // Action<object? sender, ...>
        
        // Icon

        //
        
        // Builder
        
        public DialogConfigBuilder SetTitle(string title)
        {
            Title = title;
            return this;
        }
        
        public DialogConfigBuilder SetMessage(string message)
        {
            Message = message;
            return this;
        }
        
        public DialogConfigBuilder SetButton1Text(string text)
        {
            Button1Text = text;
            return this;
        }
        
        public DialogConfigBuilder SetButton2Text(string text)
        {
            Button2Text = text;
            return this;
        }
        
        public DialogConfigBuilder SetButton3Text(string text)
        {
            Button3Text = text;
            return this;
        }
        
        public DialogConfigBuilder SetButton1Action(Action action)
        {
            Button1Action = action;
            return this;
        }
        
        public DialogConfigBuilder SetButton2Action(Action action)
        {
            Button2Action = action;
            return this;
        }
        
        public DialogConfigBuilder SetButton3Action(Action action)
        {
            Button3Action = action;
            return this;
        }
        
        public DialogConfigBuilder SetIcon()
        {
            throw new NotImplementedException();
            return this;
        }
    }
}

public abstract class AbstractDialogAlertMessageBox : IDialogAlertMessageBox
{
    public abstract void Show(IDialogAlertMessageBox.DialogConfigBuilder dialogConfig);

    public Task ShowAsync(IDialogAlertMessageBox.DialogConfigBuilder dialogConfig)
    {
        Show(dialogConfig);
        return Task.CompletedTask;
    }


    public void Show(Action<IDialogAlertMessageBox.DialogConfigBuilder> dialogConfigBuilder)
    {
        var config = new IDialogAlertMessageBox.DialogConfigBuilder();
        dialogConfigBuilder.Invoke(config);
        Show(config);
    }

    public Task ShowAsync(Action<IDialogAlertMessageBox.DialogConfigBuilder> dialogConfigBuilder)
    {
        Show(dialogConfigBuilder);
        return Task.CompletedTask;
    }
}

public abstract class AbstractAsyncDialogAlertMessageBox : IDialogAlertMessageBox
{
    public void Show(IDialogAlertMessageBox.DialogConfigBuilder dialogConfig)
    {
        ShowAsync(dialogConfig).GetAwaiter().GetResult();
    }

    public abstract Task ShowAsync(IDialogAlertMessageBox.DialogConfigBuilder dialogConfig);


    public void Show(Action<IDialogAlertMessageBox.DialogConfigBuilder> dialogConfigBuilder)
    {
        ShowAsync(dialogConfigBuilder).GetAwaiter().GetResult();
    }

    public Task ShowAsync(Action<IDialogAlertMessageBox.DialogConfigBuilder> dialogConfigBuilder)
    {
        var config = new IDialogAlertMessageBox.DialogConfigBuilder();
        dialogConfigBuilder.Invoke(config);
        return ShowAsync(config);
    }
}