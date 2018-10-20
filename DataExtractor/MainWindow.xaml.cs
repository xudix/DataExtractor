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
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Collections;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;

namespace DataExtractor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

        // Possible improvements:
        // Data validation in date and time input
        // date selector for start and end dates
        // Rewrite the ControlTemplate of date input to include a interactive date picker

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // Declaration of some fields
        // The file path that is used last time
        private string filePath = String.Empty;
        // This event is required by the INotifyPropertyChanged interface.
        // It notifies UI to update content after the back-end data is changed by program
        public event PropertyChangedEventHandler PropertyChanged;

        //private TextBoxData tagBoxData = new TextBoxData("SelectedTags");
        //private TextBoxData fileBoxData = new TextBoxData("SelectedFiles");

        public MainWindow()
        {

            InitializeComponent();
            DataContext = this;
            startDateInput.Focus();
            startDateInput.SelectAll();

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
                    char[] separators = { ' ', ',', '\t', '\n', '\r', ';' };
                    tagArray = tagList.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                    PickTagWindow pickTagDialog = new PickTagWindow(tagArray);
                    // The ShowDialog() method of Window class will show the window and disable the mian window.
                    if (pickTagDialog.ShowDialog() == true && pickTagDialog.SelectedTags!=null)
                    {
                        SelectedTags = (SelectedTags != null) ? SelectedTags.Concat(pickTagDialog.SelectedTags).ToArray() : pickTagDialog.SelectedTags;
                    }
                }

            }
        }

        private void PickFileBottom_Click(object sender, RoutedEventArgs e)
        {
            // The dialog to select data files
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Select Data Files",
                DefaultExt = ".csv",
                Filter = "CSV files (.csv)|*.csv|Text documents (.txt)|*.txt|All Files|*.*",
                Multiselect = true
            };
            if (!String.IsNullOrEmpty(filePath)) dialog.InitialDirectory = filePath;

            // If the file is selected, record the file path and open the file
            if (dialog.ShowDialog(this) == true && dialog.FileNames != null)
            {
                filePath = Path.GetDirectoryName(dialog.FileNames[0]);
                //SelectedFiles = SelectedFiles  + String.Join("\r\n", dialog.FileNames) + "\r\n";
                SelectedFiles = (SelectedFiles != null)? SelectedFiles.Concat(dialog.FileNames).ToArray() : dialog.FileNames;
            }
        }

        private void PlotButton_Click(object sender, RoutedEventArgs e)
        {
            PlotWindow plotWindow = new PlotWindow(StartDateTime, EndDateTime, SelectedTags, SelectedFiles,1);
            plotWindow.Show();
        }

        // Selected Tags. It's an array of string.
        // The tagInput textbox is bound to this array, via value converter StringArrayConverter, with parameter "tag"
        private string[] selectedTags;
        public string[] SelectedTags
        {
            get
            {
                return selectedTags;
            }
            set
            {
                if (selectedTags != value)
                {
                    selectedTags = value;
                    NotifyPropertyChanged();
                }
            }
        }

        // Selected data files, an array of string
        // The fileInput textbox is bound to this array, via value converter StringArrayConverter, with parameter "tag"
        private string[] selectedFiles;
        public string[] SelectedFiles
        {
            get
            {
                return selectedFiles;
            }
            set
            {
                if (selectedFiles != value)
                {
                    selectedFiles = value;
                    NotifyPropertyChanged();
                }
            }
        }

        // start date, DateTime object
        // Contains start Date and Time from user input
        // When the start time input is modfied, it will be updated here as well
        //  The startDateInput textbox is bound to this object
        private DateTime startDateTime = DateTime.Today;
        public DateTime StartDateTime
        {
            get
                => startDateTime;
            set
            {
                if (startDateTime != value + startTime)
                {
                    startDateTime = value + startTime;
                    NotifyPropertyChanged();
                }
            }
        }

        // start time. TimeSpan object
        //  The startTimeInput textbox is bound to this object
        private TimeSpan startTime = TimeSpan.Zero;
        public TimeSpan StartTime
        {
            get
                => startTime;
            set
            {
                if (startTime != value)
                {
                    startTime = value;
                    StartDateTime = startDateTime.Date;
                    // Note: The code set the StartDate propety here, which call its setter. The setter will add the new startTime automatically
                    NotifyPropertyChanged();
                }
            }
        }

        // end date, DateTime object
        // Contains end Date and Time from user input
        // When the end time input is modfied, it will be updated here as well
        //  The endDateInput textbox is bound to this object
        private DateTime endDateTime = DateTime.Today;
        public DateTime EndDateTime
        {
            get
                => endDateTime;
            set
            {
                if (endDateTime != value + endTime)
                {
                    endDateTime = value + endTime;
                    NotifyPropertyChanged();
                }
            }
        }

        // start time. TimeSpan object
        //  The endTimeInput textbox is bound to this object
        private TimeSpan endTime = new TimeSpan(23, 59, 59);
        public TimeSpan EndTime
        {
            get
                => endTime;
            set
            {
                if (endTime != value)
                {
                    endTime = value;
                    EndDateTime = endDateTime.Date;
                    // Note: The code set the EndDate propety here, which call its setter. The setter will add the new endTime automatically
                    NotifyPropertyChanged();
                }
            }
        }


        // This method is called by the Set accessor of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        private void NotifyPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] String propertyName = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        // The following method is used to control the behavior of the date and time input TextBox
        // When user click it the first time or double click it, it will select all text.
        // The XAML file register GotKeyboardFocus and MouseDoubleClick event to Textbox_GotFocus method, 
        // and PreviewMouseLeftButtonDown event to SelectivelyIgnoreMouseButton. 
        // It seems that when the mouse click on the textbox, it fires GotKeyboardFocus then PreviewMouseLeftButtonDown. By ignoring the second event, the mouse click will not turn "select all" into the curser at a point.

        // Method comes from https://social.msdn.microsoft.com/Forums/vstudio/en-US/564b5731-af8a-49bf-b297-6d179615819f/how-to-selectall-in-textbox-when-textbox-gets-focus-by-mouse-click?forum=wpf

        private void SelectivelyIgnoreMouseButton(object sender, MouseButtonEventArgs e)
        {
            if (sender != null && !(sender as TextBox).IsKeyboardFocusWithin)
            {
                e.Handled = true;
                (sender as TextBox).Focus();
                //Console.Write("SelIgnor Sender name: " + (sender as TextBox).Name + "; Event: " + e.RoutedEvent + "\r\n");
            }
        }

        private void Textbox_GotFocus(object sender, RoutedEventArgs e)
        {
            (sender as TextBox).SelectAll();
            e.Handled = true;
            //Console.Write("GotFocus Sender name: " + (sender as TextBox).Name + "; Event: " + e.RoutedEvent + "\r\n");
        }


        // class TextBoxData is used for connecting the data in tag and file textbox and the 
        //public class TextBoxData
        //{
        //    private string contentStr = "";
        //    public string ContentStr
        //    {
        //        get { return contentStr; }
        //        set
        //        {
        //            if (contentStr!=value)
        //            {
        //                contentStr = value;
        //                char[] separators = { ' ', ',', '\t', '\n', '\r' };
        //                contentArray = contentStr.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        //                ContentStrChanged?.Invoke(this, EventArgs.Empty);

        //            }
        //        }
        //    }

        //    // Constructor. Set the dataLabel
        //    public TextBoxData(string dataLabel = "")
        //    {
        //        DataLabel = dataLabel;
        //    }

        //    public string DataLabel { get; set; }

        //    private string[] contentArray;
        //    public string[] ContentArray => contentArray;

        //    public event EventHandler ContentStrChanged;
        //}
    }

    // This class is used to connect the tag/file array with the input boxes. The text of input boxes are strings.
    [ValueConversion(typeof(string[]), typeof(string))]
    public class StringArrayConverter : IValueConverter
    {
        private char[] tagSeparators = { ' ', ',', '\t', '\n', '\r', ';', '|', '\"' };
        private char[] fileSeparators = { '\r', '\n', '\t', '\"', '|' };
        // Convert method is from Source to Target. Source is string[] and target is string
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
            (value != null)?String.Join("\r\n", (string[])value)+"\r\n":"";

        // ConvertBack method is from Target to Source
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            switch ((string)parameter)
            {
                case "tag":
                    return ((string)value).Split(tagSeparators, StringSplitOptions.RemoveEmptyEntries);
                case "file":
                    return ((string)value).Split(fileSeparators, StringSplitOptions.RemoveEmptyEntries);
                default:
                    return null;
            }

        }

    }

    // This class connects a input box with a DateTime object via the ExtactedData.ParseDate method
    [ValueConversion(typeof(DateTime), typeof(string))]
    public class StringDateConverter : IValueConverter
    {
        // Convert method is from Source to Target. Source is DateTime and target is string
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
            (value != null) ? ((DateTime)value).ToString(@"yyyy/MM/dd") : "";

        // ConvertBack method is from Target to Source
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return ExtractedData.ParseDate((string)value);
            }
            catch
            {
                return null;
            }
        }
    }

    // This class connects a input box with a DateTime object via the ExtactedData.ParseDate method
    [ValueConversion(typeof(TimeSpan), typeof(string))]
    public class StringTimeConverter : IValueConverter
    {
        // Convert method is from Source to Target. Source is DateTime and target is string
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
            (value != null) ? ((TimeSpan)value).ToString("c") : "";

        // ConvertBack method is from Target to Source
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return ExtractedData.ParseTime((string)value);
            }
            catch
            {
                return null;
            }
        }
    }


}
