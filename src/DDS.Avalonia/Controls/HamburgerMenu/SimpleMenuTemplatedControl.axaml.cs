using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace DDS.Avalonia.Controls;

public class SimpleMenuTemplatedControl : TemplatedControl
{
    private bool _isMenuOpen = false;
  
    public bool IsMenuOpen
    {
        get => _isMenuOpen;
        // set => this.SetAndRaise();
        // set => SetAndRaise(ref _isMenuOpen, value);
    }
}