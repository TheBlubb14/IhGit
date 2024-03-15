using IhGitWpf.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace IhGitWpf
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService<MainViewModel>();
        }
    }
}
