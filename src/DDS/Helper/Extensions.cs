// namespace DDS.Helper;
//
// public static class Extensions
// {
//     
// }
//
// public static class ReactiveUiExtensions
// {
//     public static IObservable<bool> ExecuteAsyncIfPossible<TParam, TResult>(this ReactiveCommand<TParam, TResult> cmd) =>
//         cmd.CanExecute.FirstAsync().Where(can => can).Do(async _ => await cmd.Execute());
//
//     public static bool GetAsyncCanExecute<TParam, TResult>(this ReactiveCommand<TParam, TResult> cmd) =>
//         cmd.CanExecute.FirstAsync().Wait();
// }