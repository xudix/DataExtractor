using LiveCharts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace DataExtractor
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class PlotWindow : Window, INotifyPropertyChanged
    {


        // This event is required by the INotifyPropertyChanged interface.
        // It notifies UI to update content after the back-end data is changed by program
        public event PropertyChangedEventHandler PropertyChanged;


        public PlotWindow(DateTime StartDateTime, DateTime EndDateTime, string[] SelectedTags, string[] SelectedFiles, int interval = 1)
        {
            ExtractedData extractedData = new ExtractedData(StartDateTime, EndDateTime, SelectedTags, SelectedFiles, interval);
            PointsToPlot = extractedData.PointsToPlot;
            DateTimes = extractedData.DateTimes;
            DateTimeStrs = extractedData.DateTimeStrs;

            InitializeComponent();
            DataContext = this;


        }

        private SeriesCollection pointsToPlot;
        private DateTime[] dateTimes;

        public SeriesCollection PointsToPlot
        {
            get => pointsToPlot;
            set
            {
                if (pointsToPlot != value)
                {
                    pointsToPlot = value;
                    //NotifyPropertyChanged();
                }
            }
        }
        public DateTime[] DateTimes
        {
            get => dateTimes;
            set
            {
                if (dateTimes != value)
                {
                    dateTimes = value;
                    //NotifyPropertyChanged();
                }
            }
        }

        public string[] DateTimeStrs { get; set; }


        // This method is called by the Set accessor of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        private void NotifyPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] String propertyName = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
