using System.Linq;
using System.Windows;
using CppLib;

namespace FxToCore.FxApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        void OnButtonClick(object sender, RoutedEventArgs e)
        {
            f2cstring str = new f2cstring(_tbInput.Text);
            _tbInput.Text = str.Reverse();
        }
    }
}
