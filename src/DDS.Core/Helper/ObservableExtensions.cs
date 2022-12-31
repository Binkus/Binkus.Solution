using System.Windows.Input;
using Microsoft.VisualStudio.Threading;

namespace DDS.Core.Helper;

public static class ObservableExtensions
{
    public static IDisposable SubscribeAndDisposeOnNext<T>(this IObservable<T> source, Action<T>? onNext = default)
    {
        IDisposable? subscription = null;
        // ReSharper disable once AccessToModifiedClosure
        subscription = source.Subscribe(x =>
        {
            onNext?.Invoke(x);
            subscription?.Dispose();
        });
        return subscription;
    }

    public static bool CanExecute(this ICommand command)
    {
        return command.CanExecute(null);
    }

    public static bool ExecuteIfExecutable<TResult>(this ReactiveCommandBase<Unit, TResult> cmd)
    {
        try
        {
            if (cmd.CanExecute() is false) return false;
            cmd.Execute(Unit.Default).SubscribeAndDisposeOnNext();
            return true;
        }
        catch (ReactiveUI.UnhandledErrorException e)
        {
            Debug.WriteLine(e);
            throw e.InnerException ?? e;
        }
    }
    
    public static bool ExecuteIfExecutable<TParam, TResult>(this ReactiveCommandBase<TParam, TResult> cmd, TParam? executionParam = default)
    {
        try
        {
            if (cmd.CanExecute() is false) return false;
            cmd.Execute(executionParam ?? default!).SubscribeAndDisposeOnNext();
            return true;
        }
        catch (ReactiveUI.UnhandledErrorException e)
        {
            Debug.WriteLine(e);
            throw e.InnerException ?? e;
        }
    }
    
    //
    
    public static JoinableTask ExecuteIfExecutableAsync<TParam, TResult>
        (this ReactiveCommand<TParam, TResult> cmd, TParam? executionParam = default, JoinableTaskFactory? taskFactory = null)
    {
        taskFactory ??= Globals.JoinUiTaskFactory;
        var r = taskFactory.RunAsync(async () =>
        {
            await taskFactory.SwitchToMainThreadAsync();
            try
            {
                bool result = false;
                if (await cmd.CanExecute.FirstAsync().Where(can => can).Do(x => result = x))
                    await cmd.Execute(executionParam!);
                return result;
            }
            catch (ReactiveUI.UnhandledErrorException e)
            {
                Debug.WriteLine(e);
                throw e.InnerException ?? e;
            }
        });
        return r;
    }

    // public static IObservable<bool> ExecuteAsyncIfPossible<TParam, TResult>(this ReactiveCommand<TParam, TResult> cmd) =>
    //      cmd.CanExecute.FirstAsync().Where(can => can).Do(async _ => await cmd.Execute());

     // public static bool GetAsyncCanExecute<TParam, TResult>(this ReactiveCommand<TParam, TResult> cmd) =>
     //     cmd.CanExecute.FirstAsync().Wait();
}