﻿using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

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

        /// <summary>
        /// This event is fired when SyncZoom is true and the X range of the plot is changed.
        /// The event fired here is subscribed by the MainWindow, which will transmit the event to other PlotWindows
        /// The listeners (PlotWindows) with SyncZoom set to true will update the X range of their plot
        /// </summary>
        public event EventHandler<PlotRangeChangedEventArgs> PlotRangeChanged = delegate { };
        

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

        // The Resolution property corresponds to the parameter resolultion in the constructor
        // It depicts the rough number of points in each of the line
        private int resolution;
        public int Resolution
        {
            get => resolution;
            set
            {
                if (resolution != value)
                {
                    resolution = value;
                    UpdatePoints();
                }
            }
        }
        public int PointsPerLine
        {
            get => dateTimeStrs.Length;
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
                    if(SyncZoom)
                        PlotRangeChanged(this, new PlotRangeChangedEventArgs(startDateTime, endDateTime, this));
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
                    if(SyncZoom)
                        PlotRangeChanged(this, new PlotRangeChangedEventArgs(startDateTime, endDateTime, this));
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

        private Point convertedPoint;
        public Point ConvertedPoint
        {
            get => convertedPoint;
            set
            {
                if (convertedPoint != value)
                {
                    convertedPoint = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private Point rawPoint;
        public Point RawPoint
        {
            get => rawPoint;
            set
            {
                if (rawPoint != value)
                {
                    rawPoint = value;
                    NotifyPropertyChanged();
                }
            }
        }

        // These fields are used to support the draw-to-zoom function
        // Click the mouse on the chart and draw a rectangular area. 
        // The chart will zoom in to the rectangular area.
        private Point mouseDownPoint, mouseUpPoint;
        private bool isZoomDrawing;
        public bool IsZoomDrawing
        {
            get => isZoomDrawing;
            set
            {
                if (isZoomDrawing != value)
                {
                    isZoomDrawing = value;
                    NotifyPropertyChanged();
                }
            }
        }

        // ZoomBox shows up when the user use mouse for drag to zoom
        private double zoomBoxWidth;
        public double ZoomBoxWidth
        {
            get => zoomBoxWidth;
            set
            {
                if(zoomBoxWidth != value)
                {
                    zoomBoxWidth = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private double zoomBoxHeight;
        public double ZoomBoxHeight
        {
            get => zoomBoxHeight;
            set
            {
                if (zoomBoxHeight != value)
                {
                    zoomBoxHeight = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private Thickness zoomBoxMargin;
        public Thickness ZoomBoxMargin
        {
            get => zoomBoxMargin;
            set
            {
                if (zoomBoxMargin != value)
                {
                    zoomBoxMargin = value;
                    NotifyPropertyChanged();
                }
            }
        }

        // CursorValues is a list of float that represents the values of all tags at a time
        // This property is shown in the Legend with Values
        private float[] cursor1Values;
        public float[] Cursor1Values
        {
            get => cursor1Values;
            set
            {
                if (cursor1Values != value)
                {
                    cursor1Values = value;
                    NotifyPropertyChanged();
                }
            }
        }
        
        
        private string cursor1Time;
        /// <summary>
        /// Property for the displayed time for cursor 1
        /// </summary>
        public string Cursor1Time
        {
            get => cursor1Time;
            set
            {
                if (cursor1Time != value)
                {
                    cursor1Time = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        ///  Property for the location of CursorLine1
        /// </summary>
        private double cursor1X;
        public double Cursor1X
        {
            get => cursor1X;
            set
            {
                if (cursor1X != value)
                {
                    cursor1X = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// This property decides whether this plot window will zoom with other synchronized plot windows.
        /// </summary>
        /// It's also binded to the SyncZoomCheckBox
        public bool SyncZoom { get; set; }

        // The List of PlotRange will keep track of all previous zooming activities. Thus, zooming can be reversed
        private List<PlotRange> plotRanges = new List<PlotRange>();
        // currentZoomIndex indicates where we are at the List of plotRanges.
        // If currentZoomIndex = plotRanges.Count-1, i.e. currently at the last zoom, then new zooming will add new entry to List plotRanges
        // If currenZoomIndex is anything smaller, then new zooming will erase the following plotRanges and create new item
        private int currentZoomIndex;

        // Constructor
        public PlotWindow(DateTime startDateTime, DateTime endDateTime, string[] selectedTags, string[] selectedFiles, int interval = 1, int resolution = 1000, MainWindow parent = null)
        {
            
            extractedData = new ExtractedData(startDateTime, endDateTime, selectedTags, selectedFiles, interval);
            this.startDateTime = extractedData.DateTimes[0];
            this.endDateTime = extractedData.DateTimes[extractedData.pointCount-1];
            this.resolution = resolution;
            PointsToPlot = PickPoints(extractedData.RawData, selectedTags, 0, extractedData.pointCount-1, Resolution);
            DateTimeStrs = PickDates(extractedData.DateTimes, 0, extractedData.pointCount - 1, Resolution);
            cursor1Values = new float[extractedData.Tags.Length];
            InitializeComponent();
            DataContext = this;
            // Record the zooming history
            plotRanges.Add(new PlotRange(StartDateTime, EndDateTime, YMin, YMax));
            currentZoomIndex = 0;

            if(parent != null)
                // Subscribe the plotwindow to the
                WeakEventManager<MainWindow, PlotRangeChangedEventArgs>.AddHandler(parent, "TransmitPlotRangeChanged", OnPlotWindowRangeChanged);


        }

        // Create SeriesCollection from a List of float[]. Each LineSeries in SeriesCollection contains about resolution points.
        private static SeriesCollection PickPoints(IList<float[]> rawData, string[] tagList, int startIndex, int endIndex, int resolution)
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
            int interval = (endIndex - startIndex) / (resolution - 1);
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


        // Create SeriesCollection from a List of float[]. Each LineSeries in SeriesCollection contains about resolution points.
        // This overload takes the whole ExtractedData object and start/end datetime as the input.
        private static SeriesCollection PickPoints(ExtractedData extractedData, DateTime startDateTime, DateTime endDateTime, int resolution)
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

            return PickPoints(extractedData.RawData, extractedData.Tags, startIndex, endIndex, resolution);
        }


        // Create an array of string from an array of DateTime. The new array contains about resolution points.
        private static string[] PickDates(DateTime[] rawData, int startIndex, int endIndex, int resolution, string format = "yyyy/M/d H:mm:ss")
        {
            // If there's no array in rawData, nothing to return
            if (rawData.Length == 0)
                return null;
            if (startIndex == -1)
                startIndex = 0;
            if (endIndex == -1)
                endIndex = rawData.Length - 1;
            // Figure out the indexes needed to be taken
            int interval = (endIndex - startIndex) / (resolution - 1);
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

        private static string[] PickDates(DateTime[] rawData, DateTime startDateTime, DateTime endDateTime, int resolution, string format = "yyyy/M/d H:mm:ss")
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

            return PickDates(rawData, startIndex, endIndex, resolution, format);
        }
            

        // If the start/end datetime or Resolution is changed, will regenerate the PoitnsToPlot collection as well as the DateTimeStrs
        private void UpdatePoints()
        {
            PointsToPlot = PickPoints(extractedData, StartDateTime, EndDateTime, Resolution);
            DateTimeStrs = PickDates(extractedData.DateTimes, StartDateTime, EndDateTime, Resolution);
        }

        // write the current plot rage into the plotRanges list
        // This method should be called whenever the plot rnage is changed by zooming or sizing.
        private void RecordRange()
        {
            // If currentZoomIndex = plotRanges.Count-1, i.e. currently at the last zoom, then new zooming will add new entry to List plotRanges
            // If currenZoomIndex is anything smaller, then new zooming will erase the following plotRanges and create new item
            if (currentZoomIndex < plotRanges.Count - 1)
                plotRanges.RemoveRange(currentZoomIndex + 1, plotRanges.Count - currentZoomIndex - 1);
            plotRanges.Add(new PlotRange(StartDateTime, EndDateTime, YMin, YMax));
            currentZoomIndex = plotRanges.Count-1;
        }

        // Roll back the plot range to previous one in List plotRanges 
        private void ZoomPrevious_Click(object sender, RoutedEventArgs e)
            => ZoomPrevious();


        /// <summary>
        /// Roll back the plot range to previous in List plotRanges
        /// </summary>
        /// <param name="zoomIndex">The index of the requested range in List PlotRanges.
        /// zoomIndex = 0 corresponds to the starting range; zoomIndex = -1 will move the range back by one step</param>
        private void ZoomPrevious(int zoomIndex = -1)
        {
            // if currentZoomIndex is already 0, no need to do anything
            if (zoomIndex == -1)
            {
                if (currentZoomIndex > 0)
                    currentZoomIndex--;
                else
                    return;
            }
            else if(zoomIndex>=0 && zoomIndex < plotRanges.Count)
            {
                currentZoomIndex = zoomIndex;
            }
            startDateTime = plotRanges[currentZoomIndex].startDateTime;
            NotifyPropertyChanged("StartDateTime");
            EndDateTime = plotRanges[currentZoomIndex].endDateTime;
            YMax = plotRanges[currentZoomIndex].yMax;
            YMin = plotRanges[currentZoomIndex].yMin;
        }
        
        // Roll back the plot range to previous one in List plotRanges 
        private void ZoomNext_Click(object sender, RoutedEventArgs e)
        {
            if (currentZoomIndex < plotRanges.Count-1)
                currentZoomIndex++;
            else
                return;
            startDateTime = plotRanges[currentZoomIndex].startDateTime;
            NotifyPropertyChanged("StartDateTime");
            EndDateTime = plotRanges[currentZoomIndex].endDateTime;
            YMax = plotRanges[currentZoomIndex].yMax;
            YMin = plotRanges[currentZoomIndex].yMin;
        }

        // This method is called by the Set accessor of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        private void NotifyPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] String propertyName = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void CartesianChart_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ZoomPrevious(0);
            e.Handled = true;
        }

        // Reset the min value of X axis to earliest datetime
        private void XAxisMinReset_Click(object sender, RoutedEventArgs e)
        {
            StartDateTime = extractedData.DateTimes[0];
        }

        // Reset the max value of X axis to earliest datetime
        private void XAxisMaxReset_Click(object sender, RoutedEventArgs e)
        {
            EndDateTime = extractedData.DateTimes[extractedData.pointCount - 1];
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
        //private void ExportButton2_Click(object sender, RoutedEventArgs e)
        //{
        //    extractedData.WriteToFile_PipeLine(StartDateTime, EndDateTime, this, "csv");
        //}

        private void Chart_MouseMove(object sender, MouseEventArgs e)
        {
            // Simply moving the mouse in the chart. Will move the cursor
            Cursor1X = e.GetPosition(Chart).X;
            if (Cursor1X > Chart.ActualWidth - Chart.ChartLegend.ActualWidth)
                Cursor1X = Chart.ActualWidth - Chart.ChartLegend.ActualWidth;
            //Cursor1Margin = new Thickness(position, 0, Chart.ActualWidth - position - 1, XAxis.ActualHeight);
            UpdateLegendValues(Chart.ConvertToChartValues(new Point(Cursor1X, 0)).X);
            //RawPoint = e.GetPosition(Chart);
            //ConvertedPoint = Chart.ConvertToChartValues(RawPoint);

            // If the mouse is moving in the chart, allow the following behavior:
            // IsZoomDrawing means the mouse left button was pressed. Performing zoom
            if (IsZoomDrawing)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    Point currentPoint = e.GetPosition(Chart);
                    double xmin = mouseDownPoint.X;
                    double xmax = currentPoint.X;
                    double ymin = currentPoint.Y;
                    double ymax = mouseDownPoint.Y;
                    // make sure min is smaller than max
                    if (xmin > xmax)
                    {
                        double temp = xmin;
                        xmin = xmax;
                        xmax = temp;
                    }
                    // Limit the zoombox in the chart
                    
                    if (xmax > Chart.ActualWidth - Chart.ChartLegend.ActualWidth)
                        xmax = Chart.ActualWidth - Chart.ChartLegend.ActualWidth;
                    if (ymin > ymax)
                    {
                        double temp = ymin;
                        ymin = ymax;
                        ymax = temp;
                    }
                    // Limit the zoombox in the chart
                    if (ymin < 0) ymin = 0;
                    if (ymax > Chart.ActualHeight) ymax = Chart.ActualHeight;
                    if (xmax >= xmin && ymax >= ymin) { 
                        ZoomBoxWidth = xmax - xmin;
                        ZoomBoxHeight = ymax - ymin;
                        ZoomBoxMargin = new Thickness(xmin, ymin, Chart.ActualWidth - xmax, Chart.ActualHeight - ymax);
                    }
                }
                else // If the user moved the mouse to outside the Grid, release the button, and move it back into the chart, zooming will be canceled
                {
                    IsZoomDrawing = false;
                    ZoomBoxHeight = 0;
                    ZoomBoxWidth = 0;
                }
            }
            else 
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    // If IsZoomDrawing is not set but mouse left button is pressed, then the user probably pressed the button outside the chart and moved it inside.
                    // Will start zooming
                    // Previousw MouseLeftButtonDown event handler is moved here
                    // The reason is, if the user click the mouse left button outside the chart and move the mouse inside,
                    // the program won't be able to catch it. 
                    mouseDownPoint = e.GetPosition(Chart);
                    IsZoomDrawing = true;
                }
                else
                {
                    
                }
                    
            }
        }

        private void Chart_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IsZoomDrawing)
            {
                mouseUpPoint = e.GetPosition(Chart);
                IsZoomDrawing = false;
                ZoomBoxHeight = 0;
                ZoomBoxWidth = 0;
                // If the up and down points are very close to each other, it's not zooming
                if ((mouseDownPoint.X - mouseUpPoint.X) * (mouseDownPoint.X - mouseUpPoint.X) + (mouseDownPoint.Y - mouseUpPoint.Y) * (mouseDownPoint.Y - mouseUpPoint.Y) < 100)
                    return;

                mouseDownPoint = Chart.ConvertToChartValues(mouseDownPoint);
                mouseUpPoint = Chart.ConvertToChartValues(mouseUpPoint);
                // Find the coordinates for the zoom rectangular
                double xmin = mouseDownPoint.X;
                double xmax = mouseUpPoint.X;
                double ymin = mouseUpPoint.Y;
                double ymax = mouseDownPoint.Y;
                // make sure min is smaller than max
                if (xmin > xmax)
                {
                    double temp = xmin;
                    xmin = xmax;
                    xmax = temp;
                }
                if (ymin > ymax)
                {
                    double temp = ymin;
                    ymin = ymax;
                    ymax = temp;
                }
                // If the start point is outside the range. No need to continue.
                int startIndex = (int)xmin;
                if (startIndex < 0) startIndex = 0;
                else if (startIndex >= PointsPerLine) return;
                // If the change is too small, the user is probably not intended to zoom
                if ((ymax - ymin) / (YAxis.ActualMaxValue - YAxis.ActualMinValue) > 0.02)
                {
                    YMax = ymax;
                    YMin = ymin;
                }
                // If the change is too small, the user is probably not intended to zoom
                if ((xmax - xmin) / PointsPerLine > 0.02)
                {
                    
                    int endIndex = (int)Math.Ceiling(xmax);
                    if (endIndex >= PointsPerLine) endIndex = PointsPerLine - 1;
                    // Here we are changing the private field "startDateTime" instead of the property "StartDateTime"
                    // because if we chagne the property, UpdatePoints() method will be invoked and DateTimeStrs will be changed.
                    startDateTime = ExtractedData.ParseDateTime(DateTimeStrs[startIndex]);
                    NotifyPropertyChanged("StartDateTime");
                    // Here we invoke NotifyPropertyChanged and UpdatePoints manually because
                    // when the zoom operation is ended (mouse up) outside the chart, EndDatTime will be the same. 
                    // In this case, the UpdatePoints method will not be invoked. 
                    // However, we may need to invoke it since StartDateTime may have changed.
                    endDateTime = ExtractedData.ParseDateTime(DateTimeStrs[endIndex]);
                    NotifyPropertyChanged("EndDateTime");
                    UpdatePoints();
                    PlotRangeChanged(this, new PlotRangeChangedEventArgs(startDateTime, endDateTime, this));
                }
                RecordRange();
            }
            //else
            //{
            //    // Not zooming. simply clicking
            //    //Will update the values shown in the legend
                

            //}
        }

        /// <summary>
        /// Respond to the PlotRangeChanged events raised by other PlotWindows and transmitted by the MainWindow
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnPlotWindowRangeChanged(object source, PlotRangeChangedEventArgs e)
        {
            if(e.InitialSource as PlotWindow != this)
            {
                if (SyncZoom) // Only when SyncZoom == true will this window respond to this event
                {
                    if (e.StartDateTime != StartDateTime || e.EndDateTime != EndDateTime)
                    {
                        // We are modifying the fields to avoid firing the PlotRangeChanged event again.
                        if (e.StartDateTime != StartDateTime)
                        {
                            startDateTime = e.StartDateTime;
                            NotifyPropertyChanged("StartDateTime");
                        }
                        if (e.EndDateTime != EndDateTime)
                        {
                            endDateTime = e.EndDateTime;
                            NotifyPropertyChanged("EndDateTime");
                        }
                        UpdatePoints();
                        RecordRange();
                    }
                }
            }
            
        }

        // update the tag values based on the X axis positionin the chart
        // chartValue is the X position of the point
        // typically, it shoule come from: chartValues = Chart.ConvertToChartValues(e.GetPosition(Chart)).X
        // where e is a MouseEventArg
        private void UpdateLegendValues(double chartValues)
        {
            
            int valueIndex = (int)Math.Round(chartValues);
            if (valueIndex >= 0 && valueIndex < PointsPerLine)
            {
                // If we change the elements of CursorValues one by one, the setter will not be called, and NotifyPropertyChanged will not be fired
                // In addition, the DependencyProperty in LegendWith Values will be changed but the PropertyChangedCallback will not be triggered
                // This is probably because the array object (reference to the array) is never chagned. 
                // Assigning a new array to it will trigger the PropertyChangedCallback
                float[] temp = new float[extractedData.Tags.Length];
                for (int i = 0; i < PointsToPlot.Count; i++)
                {
                    temp[i] = (float)PointsToPlot[i].Values[valueIndex];
                }
                Cursor1Values = temp;
                Cursor1Time = DateTimeStrs[valueIndex];
            }
        }

        public struct PlotRange
        {
            public DateTime startDateTime;
            public DateTime endDateTime;
            public double yMin;
            public double yMax;

            public PlotRange(DateTime startDateTime, DateTime endDateTime, double yMin, double yMax)
            {
                this.startDateTime = startDateTime;
                this.endDateTime = endDateTime;
                this.yMax = yMax;
                this.yMin = yMin;
            }
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

    // Calculate the height of the cursor line based on the height of the Chart and height of the XAxis
    // The cursor height is the difference between the Chart.ActualHeight and XAxis.ActualHeight
    public class CursorHeightCalc: IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return (double)values[0] - (double)values[1];
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


}
