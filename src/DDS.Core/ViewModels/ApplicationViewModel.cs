using DDS.Core.Controls;

namespace DDS.Core.ViewModels;

public class ApplicationViewModel : ViewModel
{
    public ApplicationViewModel() : base(Globals.Services)
    {
        ExitCommand = ReactiveCommand.Create(() =>
        {
            if(Globals.ApplicationLifetimeWrapped is ICoreDesktopLifetime desktopLifetime)
            {
                desktopLifetime.Shutdown();
            }
        });

        ToggleCommand = ReactiveCommand.Create(() => { });
    }
    
    [UsedImplicitly] public ReactiveCommand<Unit,Unit> ExitCommand { get; }

    [UsedImplicitly] public ReactiveCommand<Unit,Unit> ToggleCommand { get; }
}