using LiveCharts;
using LiveCharts.Wpf;
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
        private SeriesCollection pointsToPlot;
        private DateTime[] dateTimes;
        private string[] dateTimeStrs;

        // These two properties are the binding path for the plot
        // Each of them contains pointsPerLine points
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
        public string[] DateTimeStrs
        {
            get => dateTimeStrs;
            set
            {
                if(dateTimeStrs != value)
                {
                    dateTimeStrs = value;
                    NotifyPropertyChanged();
                }
            }
        }

        // The following two properties are used to store the data obtained from data re

        public DateTime[] DateTimes { get; set; }
        public List<float[]> RawData { get; set; }


        public PlotWindow(DateTime StartDateTime, DateTime EndDateTime, string[] SelectedTags, string[] SelectedFiles, int interval = 1, int pointsPerLine = 500)
        {
            ExtractedData extractedData = new ExtractedData(StartDateTime, EndDateTime, SelectedTags, SelectedFiles, interval);

            PointsToPlot = PickPoints(extractedData.RawData, SelectedTags, 0, extractedData.pointCount-1, pointsPerLine);
            DateTimeStrs = PickDates(extractedData.DateTimes, 0, extractedData.pointCount - 1, pointsPerLine);
            InitializeComponent();
            DataContext = this;


        }

        // Create SeriesCollection from a List of float[]. Each LineSeries in SeriesCollection contains about pointsPerLine points.
        private static SeriesCollection PickPoints(IList<float[]> rawData, string[] tagList, int startIndex, int endIndex, int pointsPerLine)
        {
            // If there's no array in rawData, nothing to return
            if (rawData.Count == 0)
                return null;
            // The local variable for storing the points to be plotted
            float[] temp;            
            // Figure out the indexes needed to be taken
            int interval = (endIndex - startIndex) / (pointsPerLine - 1);
            int pointCount = (endIndex - startIndex) / interval + 1;
            // oldIndex is the index of a point in the rawData. newIndex is that in the new array
            int oldIndex, newIndex;
            
            SeriesCollection result = new SeriesCollection();
            
            for (int i = 0; i < rawData.Count; i++) // for each tag (each array in rawData)
            {
                // create an empty array to take the data
                temp = new float[pointCount];
                oldIndex = newIndex = startIndex;
                for (; newIndex < pointCount; newIndex++)
                {
                    temp[newIndex] = rawData[i][oldIndex];
                    oldIndex+=interval;
                }
                var series = new LineSeries()
                {
                    Title = tagList[i],
                    Values = new ChartValues<float>(temp),
                    LineSmoothness = 0,
                    PointGeometry = null,
                    Fill = Brushes.Transparent,
                };
                result.Add(series);
            }
            return result;
        }

        // Create an array of string from an array of DateTime. The new array contains about pointsPerLine points.
        private static string[] PickDates(DateTime[] rawData, int startIndex, int endIndex, int pointsPerLine, string format = "M/d H:mm")
        {
            // If there's no array in rawData, nothing to return
            if (rawData.Length == 0)
                return null;
            // Figure out the indexes needed to be taken
            int interval = (endIndex - startIndex) / (pointsPerLine - 1);
            int pointCount = (endIndex - startIndex) / interval + 1;
            // oldIndex is the index of a point in the rawData. newIndex is that in the new array
            int oldIndex, newIndex;

            string[] result = new string[pointCount];

            oldIndex = newIndex = startIndex;
            for (; newIndex < pointCount; newIndex++)
            {
                result[newIndex] = rawData[oldIndex].ToString(format);
                oldIndex += interval;
            }

            return result;
        }

        // This method is called by the Set accessor of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        private void NotifyPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] String propertyName = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void CartesianChart_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            XAxis.MinValue = Double.NaN;
            XAxis.MaxValue = Double.NaN;
            YAxis.MinValue = Double.NaN;
            YAxis.MaxValue = Double.NaN;
        }
    }
}
