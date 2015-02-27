using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using ReadCgfxGui.ViewModel;

namespace ReadCgfxGui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BrowseClick(object sender, RoutedEventArgs e)
        {
            var context = DataContext as MainViewModel;
            if (context == null) throw new Exception("DataContext incorrect");

            var dialog = new OpenFileDialog();
            dialog.ShowDialog();
            context.FileName = dialog.FileName;

            context.OpenCgfxCommand.Execute(null);
        }
    }
}
