namespace DDS.Core.Controls;

public interface ICoreWindowFor<TViewModel>  : ICoreViewFor<TViewModel>
    where TViewModel : class
{
    /// <summary>
    /// Closes the window.
    /// </summary>
    public void Close();

    /// <summary>
    /// Closes a dialog window with the specified result.
    /// </summary>
    /// <param name="dialogResult">The dialog result.</param>
    /// <remarks>
    /// When the window is shown with the <see cref="ShowDialog{TResult}(Window)"/>
    /// or <see cref="ShowDialog{TResult}(Window)"/> method, the
    /// resulting task will produce the <see cref="_dialogResult"/> value when the window
    /// is closed.
    /// </remarks>
    public void Close(object dialogResult);
}