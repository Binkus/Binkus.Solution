using System.Windows.Input;

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
        if (cmd.CanExecute() is false) return false;
        cmd.Execute(Unit.Default).SubscribeAndDisposeOnNext();
        return true;
    }
    
    public static bool ExecuteIfExecutable<TParam, TResult>(this ReactiveCommandBase<TParam, TResult> cmd, TParam? executionParam = default)
    {
        if (cmd.CanExecute() is false) return false;
        cmd.Execute(executionParam ?? default!).SubscribeAndDisposeOnNext();
        return true;
    }
}