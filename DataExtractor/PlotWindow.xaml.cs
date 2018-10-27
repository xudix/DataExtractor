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
        

        // These two properties are the binding path for the plot
        // Each of them contains pointsPerLine points
        private SeriesCollection pointsToPlot;
        public SeriesCollection PointsToPlot
        {
            get => pointsToPlot;
            set
            {
                if (pointsToPlot != value)
                {
                    pointsToPlot = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private string[] dateTimeStrs;
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

        // This field stores the data obtained from the data files

        private ExtractedData extractedData;

        // The PointsPerLine property corresponds to the parameter pointsPerLine in the constructor
        // It depicts the rough number of points in each of the line
        private int pointsPerLine;
        public int PointsPerLine
        {
            get => pointsPerLine;
            set
            {
                if (pointsPerLine != value)
                {
                    pointsPerLine = value;
                    UpdatePoints();
                }
            }
        }

        // start time, DateTime object
        // Contains start Date and Time from user input
        //  The xAxisMinInput textbox is bound to this object
        private DateTime startDateTime;
        public DateTime StartDateTime
        {
            get
                => startDateTime;
            set
            {
                if (startDateTime != value)
                {
                    startDateTime = value;
                    UpdatePoints();
                    NotifyPropertyChanged();
                }
            }
        }

        // end date, DateTime object
        // Contains end Date and Time from user input
        //  The xAxisMaxInput textbox is bound to this object
        private DateTime endDateTime;
        public DateTime EndDateTime
        {
            get
                => endDateTime;
            set
            {
                if (endDateTime != value)
                {
                    endDateTime = value;
                    UpdatePoints();
                    NotifyPropertyChanged();
                }
            }
        }

        // The min value of Y axis
        private double yMin = Double.NaN;
        public double YMin
        {
            get
                => yMin;
            set
            {
                if (yMin != value)
                {
                    yMin = value;
                    NotifyPropertyChanged();
                }
            }
        }

        // The max value of Y axis
        private double yMax = Double.NaN;
        public double YMax
        {
            get
                => yMax;
            set
            {
                if (yMax != value)
                {
                    yMax = value;
                    NotifyPropertyChanged();
                }
            }
        }

        // Constructor
        public PlotWindow(DateTime startDateTime, DateTime endDateTime, string[] selectedTags, string[] selectedFiles, int interval = 1, int pointsPerLine = 500)
        {
            
            extractedData = new ExtractedData(startDateTime, endDateTime, selectedTags, selectedFiles, interval);
            this.startDateTime = extractedData.DateTimes[0];
            this.endDateTime = extractedData.DateTimes[extractedData.pointCount-1];
            this.pointsPerLine = pointsPerLine;
            PointsToPlot = PickPoints(extractedData.RawData, selectedTags, 0, extractedData.pointCount-1, PointsPerLine);
            DateTimeStrs = PickDates(extractedData.DateTimes, 0, extractedData.pointCount - 1, PointsPerLine);
            InitializeComponent();
            DataContext = this;


        }

        // Create SeriesCollection from a List of float[]. Each LineSeries in SeriesCollection contains about pointsPerLine points.
        private static SeriesCollection PickPoints(IList<float[]> rawData, string[] tagList, int startIndex, int endIndex, int pointsPerLine)
        {
            // If there's no array in rawData, nothing to return
            if (rawData.Count == 0)
                return null;
            if (startIndex == -1)
                startIndex = 0;
            if (endIndex == -1)
                endIndex = rawData[0].Length - 1;
            // The local variable for storing the points to be plotted
            float[] temp;            
            // Figure out the indexes needed to be taken
            int interval = (endIndex - startIndex) / (pointsPerLine - 1);
            if (interval < 1)
                interval = 1;
            int pointCount = (endIndex - startIndex) / interval + 1;
            // oldIndex is the index of a point in the rawData. newIndex is that in the new array
            int oldIndex, newIndex;
            
            SeriesCollection result = new SeriesCollection();
            
            for (int i = 0; i < rawData.Count; i++) // for each tag (each array in rawData)
            {
                // create an empty array to take the data
                temp = new float[pointCount];
                newIndex = 0;
                oldIndex = startIndex;
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


        // Create SeriesCollection from a List of float[]. Each LineSeries in SeriesCollection contains about pointsPerLine points.
        // This overload takes the whole ExtractedData object and start/end datetime as the input.
        private static SeriesCollection PickPoints(ExtractedData extractedData, DateTime startDateTime, DateTime endDateTime, int pointsPerLine)
        {
            int startIndex=0, endIndex;
            if (startDateTime > endDateTime)
            {
                DateTime temp = startDateTime;
                startDateTime = endDateTime;
                endDateTime = temp;
            }
            // find the first timestamp that is larger than startDateTime. Will start PickPoints from here
            while (startIndex < extractedData.pointCount && extractedData.DateTimes[startIndex] < startDateTime)
                startIndex++;
            // If we reach the end of the data and found no time stamp larger than startDateTime, will return an empty object
            if (startIndex == extractedData.pointCount)
                return new SeriesCollection();
            endIndex = startIndex;
            // find the fist timeStamp that is larger than endDateTime. End PickPoints one point before that.
            while (endIndex < extractedData.pointCount && extractedData.DateTimes[endIndex] <= endDateTime)
                endIndex++;
            endIndex--;

            return PickPoints(extractedData.RawData, extractedData.Tags, startIndex, endIndex, pointsPerLine);
        }


        // Create an array of string from an array of DateTime. The new array contains about pointsPerLine points.
        private static string[] PickDates(DateTime[] rawData, int startIndex, int endIndex, int pointsPerLine, string format = "M/d H:mm")
        {
            // If there's no array in rawData, nothing to return
            if (rawData.Length == 0)
                return null;
            if (startIndex == -1)
                startIndex = 0;
            if (endIndex == -1)
                endIndex = rawData.Length - 1;
            // Figure out the indexes needed to be taken
            int interval = (endIndex - startIndex) / (pointsPerLine - 1);
            if (interval < 1)
                interval = 1;
            int pointCount = (endIndex - startIndex) / interval + 1;
            // oldIndex is the index of a point in the rawData. newIndex is that in the new array
            int oldIndex, newIndex;

            string[] result = new string[pointCount];

            oldIndex = startIndex;
            newIndex = 0;
            for (; newIndex < pointCount; newIndex++)
            {
                result[newIndex] = rawData[oldIndex].ToString(format);
                oldIndex += interval;
            }

            return result;
        }

        private static string[] PickDates(DateTime[] rawData, DateTime startDateTime, DateTime endDateTime, int pointsPerLine, string format = "M/d H:mm")
        {
            int startIndex = 0, endIndex;
            if (startDateTime > endDateTime)
            {
                DateTime temp = startDateTime;
                startDateTime = endDateTime;
                endDateTime = temp;
            }
            // find the first timestamp that is larger than startDateTime. Will start PickPoints from here
            while (startIndex < rawData.Length && rawData[startIndex] < startDateTime)
                startIndex++;
            // If we reach the end of the data and found no time stamp larger than startDateTime, will return an empty object
            if (startIndex == rawData.Length)
                return new string[0];
            endIndex = startIndex;
            // find the fist timeStamp that is larger than endDateTime. End PickPoints one point before that.
            while (endIndex < rawData.Length && rawData[endIndex] <= endDateTime)
                endIndex++;
            endIndex--;

            return PickDates(rawData, startIndex, endIndex, pointsPerLine, format);
        }
            

        // If the start/end datetime or PointsPerLine is changed, will regenerate the PoitnsToPlot collection as well as the DateTimeStrs
        private void UpdatePoints()
        {
            PointsToPlot = PickPoints(extractedData, StartDateTime, EndDateTime, PointsPerLine);
            DateTimeStrs = PickDates(extractedData.DateTimes, StartDateTime, EndDateTime, PointsPerLine);
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

        // Reset the min value of X axis to earliest datetime
        private void XAxisMinReset_Click(object sender, RoutedEventArgs e)
        {
            StartDateTime = extractedData.DateTimes[0];
        }

        // Reset the max value of X axis to earliest datetime
        private void XAxisMaxReset_Click(object sender, RoutedEventArgs e)
        {
            StartDateTime = extractedData.DateTimes[extractedData.pointCount - 1];
        }

        // Reset the min value of Y axis to earliest datetime
        private void YAxisMinReset_Click(object sender, RoutedEventArgs e)
        {
            YMin = Double.NaN;
        }

        // Reset the max value of Y axis to earliest datetime
        private void YAxisMaxReset_Click(object sender, RoutedEventArgs e)
        {
            YMax = Double.NaN;
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            extractedData.WriteToFile(StartDateTime, EndDateTime, this, "csv");
        }
        private void ExportButton2_Click(object sender, RoutedEventArgs e)
        {
            extractedData.WriteToFile_PipeLine(StartDateTime, EndDateTime, this, "csv");
        }
    }

    // This class connects a input box with a DateTime object via the ExtactedData.ParseDateTime method
    [ValueConversion(typeof(DateTime), typeof(string))]
    public class StringDateTimeConverter : IValueConverter
    {
        // Convert method is from Source to Target. Source is DateTime and target is string
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
            (value != null) ? ((DateTime)value).ToString(@"yyyy/M/d H:mm:ss") : "";

        // ConvertBack method is from Target to Source
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return ExtractedData.ParseDateTime((string)value);
            }
            catch
            {
                return null;
            }
        }
    }

    // This class connects the Y axis min and max input box to corresponding properties
    // The main purpose of this class is to handle Double.NaN.
    // When the min/max property is set to NaN, the input box will show empty string
    [ValueConversion(typeof(double), typeof(string))]
    public class StrDoubleConverter : IValueConverter
    {
        // Convert method is from Source to Target. Source is Double and target is string
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
            (!Double.IsNaN((double)value)) ? ((double)value).ToString("f") : "";

        // ConvertBack method is from Target to Source
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((string)value == "")
                return Double.NaN;
            try
            {
                return Double.Parse((string)value);
            }
            catch
            {
                return Double.NaN;
            }
        }
    }


}
