using WPF3D.ToolBox.ViewModels;

namespace WPF3D.ToolBox.Views
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = new MainWindowViewModel();
        }
    }
}