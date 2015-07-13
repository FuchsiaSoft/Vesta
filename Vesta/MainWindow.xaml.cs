using FirstFloor.ModernUI.Windows.Controls;
using System;
using System.Collections.Generic;
using System.IO;
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
using Vesta.Properties;
using Vesta.ViewModels;

namespace Vesta
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ModernWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            if (Settings.Default.ShowInitialPopup)
            {
                InitialPopupView popupView = new InitialPopupView();
                popupView.DataContext = new InitialPopupViewModel(popupView);
                popupView.ShowDialog();
            }

            this.DataContext = new MainWindowViewModel(this);
        }

        private void DataGrid_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
        }


        private void DataGrid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                IEnumerable<string> draggedPaths = 
                    (string[])e.Data.GetData(DataFormats.FileDrop);

                bool recursiveFolders = false;

                //quick check to see if any folders found
                foreach (string path in draggedPaths)
                {
                    if (Directory.Exists(path)) 
                    {
                        MessageBoxResult result = ModernDialog.ShowMessage
                            ("The data dragged contains one or more folders, would " +
                             "you like the contents of the folders to be included also?",
                             "Folders found", MessageBoxButton.YesNo, this);

                        recursiveFolders = (result == MessageBoxResult.Yes) ? true : false;
                        break;
                    }
                }

                MainWindowViewModel myVM = (MainWindowViewModel)this.DataContext;
                myVM.ConsumeFileStrings(draggedPaths, recursiveFolders);
            }
        }
    }
}
