namespace DDS.Controls;

public class ReactiveViewLocator : ReactiveUI.IViewLocator
{
    public IViewFor? ResolveView<T>(T? viewModel, string? contract = null) => viewModel switch
    {
        TestViewModel context => new TestView() { DataContext = context },
        SecondTestViewModel context => new SecondTestView() { DataContext = context },
        _ => throw new ArgumentOutOfRangeException(nameof(viewModel))
    };
}