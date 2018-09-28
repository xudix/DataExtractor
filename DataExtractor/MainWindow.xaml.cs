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
using System.IO;
using Microsoft.Win32;

namespace DataExtractor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Declaration of some fields
        // The file path that is used last time
        string filePath = String.Empty;

        public MainWindow()
        {
            InitializeComponent();

         

        }

        private void PickTagBottom_Click(object sender, RoutedEventArgs e)
        {
            string tagList = String.Empty;
            string[] tagArray;
            // The dialog to select Tag List
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Select Tag List File",
                FileName = "TagList.txt",
                DefaultExt = ".txt",
                Filter = "Text documents (.txt)|*.txt|All Files|*.*"
            };
            if (!String.IsNullOrEmpty(filePath)) dialog.InitialDirectory = filePath;

            // If the file is selected, record the file path and open the file
            if (dialog.ShowDialog(this) == true)
            {
                filePath = Path.GetDirectoryName(dialog.FileName);
                try
                {
                    tagList = File.ReadAllText(dialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read tag file from disk. Original error: " + ex.Message);
                }
                if (!String.IsNullOrEmpty(tagList))
                {
                    char[] separators = { ' ', ',', '\t', '\n' };
                    tagArray = tagList.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                }
                
            }
        }

        private void PickFileBottom_Click(object sender, RoutedEventArgs e)
        {

        }

        private void PlotButton_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
