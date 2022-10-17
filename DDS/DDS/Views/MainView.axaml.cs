using System.Reactive;
using Avalonia.Controls;
using DDS.Controls;
using DDS.ViewModels;

namespace DDS.Views
{
    public partial class MainView : BaseUserControl<MainViewModel>
    {
        public MainView()
        {
            InitializeComponent();
        }
    }
}