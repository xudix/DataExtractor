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
using System.Windows.Shapes;
using System.Collections.ObjectModel;

namespace DataExtractor
{
    /// <summary>
    /// Interaction logic for PickTagWindow.xaml
    /// </summary>
    /// 

    // Potential improvement: Add a filter textbox and allow filtering the tags with user input

    public partial class PickTagWindow : Window
    {
        public ObservableCollection<string> TagCollection { get; set; }

        public string[] SelectedTags => (tagListBox.SelectedItems != null) ? tagListBox.SelectedItems.Cast<string>().ToArray() : null;

        public PickTagWindow(string[] tagArray)
        {
            InitializeComponent();
            DataContext = this;
            //tagListBox.ItemsSource = tagArray;
            TagCollection = new ObservableCollection<string>(tagArray);
        }

        

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            //this.Close();
        }
    }
}
