using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using LiveCharts;
using LiveCharts.Wpf;

namespace DataExtractor
{
    /// <summary>
    /// This is a variation of the DefaultLegend Class of LiveCharts Library
    /// It provides the capability to display values under each legend item
    /// To display the values, bind the Values property to an IList<float> object e.g. an array.
    /// Note: You need to make a new IList<> object everytime you want to update the display
    /// Otherwise, the 
    /// </summary>
    public partial class LegendWithValues : UserControl, IChartLegend
    {
        private List<SeriesViewModel> _series;

        private List<SeriesWithValueViewModel> _seriesWithValue;

        /// <summary>
        /// Initializes a new instance of DefaultLegend class
        /// </summary>
        public LegendWithValues()
        {
            InitializeComponent();
            SeriesWithValue = SeriesWithValueViewModel.CreateList(Series, Values);
            DataContext = this;
        }

        /// <summary>
        /// Property changed event
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the series displayed in the legend.
        /// </summary>
        public List<SeriesViewModel> Series
        {
            get { return _series; }
            set
            {
                _series = value;
                SeriesWithValue = SeriesWithValueViewModel.CreateList(Series, Values);
                OnPropertyChanged("Series");
            }
        }

        /// <summary>
        /// Gets the series With Values displayed in the legend.
        /// </summary>
        public List<SeriesWithValueViewModel> SeriesWithValue
        {
            get { return _seriesWithValue; }
            set
            {
                _seriesWithValue = value;
                OnPropertyChanged("SeriesWithValue");
            }
        }

        /// <summary>
        /// The Values Property
        /// </summary>
        public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
            "Values", typeof(IList<float>), typeof(LegendWithValues), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnValuesPropertyChange)));

        private static void OnValuesPropertyChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = d as LegendWithValues;
            source.SeriesWithValue = SeriesWithValueViewModel.CreateList(source.Series, (IList<float>)e.NewValue);
        }

        /// <summary>
        /// Gets or sets the Values of the legend.
        /// </summary>
        public IList<float> Values
        {
            get { return (IList<float>)GetValue(ValuesProperty); }
            set => SetValue(ValuesProperty, value);
        }
        /// <summary>
        /// The XValue property. XValue is shown on top of the Legend
        /// </summary>
        public static readonly DependencyProperty XValueProperty = DependencyProperty.Register(
            "XValue", typeof(String), typeof(LegendWithValues), new PropertyMetadata(String.Empty));
        /// <summary>
        /// Gets or sets the text to be displayed on top of the legend
        /// </summary>
        public string XValue
        {
            get { return (String)GetValue(XValueProperty); }
            set { SetValue(XValueProperty, value); }
        }


        /// <summary>
        /// The orientation property
        /// </summary>
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            "Orientation", typeof (Orientation?), typeof (LegendWithValues), new PropertyMetadata(null));
        /// <summary>
        /// Gets or sets the orientation of the legend, default is null, if null LiveCharts will decide which orientation to use, based on the Chart.Legend location property.
        /// </summary>
        public Orientation? Orientation
        {
            get { return (Orientation) GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        /// <summary>
        /// The internal orientation property
        /// </summary>
        public static readonly DependencyProperty InternalOrientationProperty = DependencyProperty.Register(
            "InternalOrientation", typeof (Orientation), typeof (LegendWithValues), 
            new PropertyMetadata(default(Orientation)));

        /// <summary>
        /// Gets or sets the internal orientation.
        /// </summary>
        /// <value>
        /// The internal orientation.
        /// </value>
        public Orientation InternalOrientation
        {
            get { return (Orientation) GetValue(InternalOrientationProperty); }
            set { SetValue(InternalOrientationProperty, value); }
        }

        /// <summary>
        /// The bullet size property
        /// </summary>
        public static readonly DependencyProperty BulletSizeProperty = DependencyProperty.Register(
            "BulletSize", typeof(double), typeof(LegendWithValues), new PropertyMetadata(15d));
        /// <summary>
        /// Gets or sets the bullet size, the bullet size modifies the drawn shape size.
        /// </summary>
        public double BulletSize
        {
            get { return (double)GetValue(BulletSizeProperty); }
            set { SetValue(BulletSizeProperty, value); }
        }

        /// <summary>
        /// Called when [property changed].
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null) PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SeriesWithValueViewModel
    {
        public SeriesWithValueViewModel(SeriesViewModel series, float value)
        {
            _series = series;
            Value = value;
        }
        
        

        internal static List<SeriesWithValueViewModel> CreateList(List<SeriesViewModel> series, IList<float> values)
        {
            if (series == null)
                return null;
            var result = new List<SeriesWithValueViewModel>(series.Count);
            for(int i = 0; i<series.Count; i++)
            {
                if(values != null && values.Count>i)   
                    result.Add(new SeriesWithValueViewModel(series[i], values[i]));
                else
                    result.Add(new SeriesWithValueViewModel(series[i], Single.NaN));
            }
            return result;
        }

        private SeriesViewModel _series;
        //
        // Summary:
        //     Series Title
        public string Title
        {
            get => _series.Title;
            set
            {
                _series.Title = value;
            }
        }
        //
        // Summary:
        //     Series stroke
        public System.Windows.Media.Brush Stroke
        {
            get => _series.Stroke;
            set
            {
                _series.Stroke = value;
            }
        }
        //
        // Summary:
        //     Series Stroke thickness
        public double StrokeThickness
        {
            get => _series.StrokeThickness;
            set
            {
                _series.StrokeThickness = value;
            }
        }
        //
        // Summary:
        //     Series Fill
        public System.Windows.Media.Brush Fill
        {
            get => _series.Fill;
            set
            {
                _series.Fill = value;
            }
        }
        //
        // Summary:
        //     Series point Geometry
        public System.Windows.Media.Geometry PointGeometry
        {
            get => _series.PointGeometry;
            set
            {
                _series.PointGeometry = value;
            }
        }
        //
        // Summary:
        //     Value to be shown
        public float Value { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.Windows.Data.IMultiValueConverter" />
    public class OrientationConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts source values to a value for the binding target. The data binding engine calls this method when it propagates the values from source bindings to the binding target.
        /// </summary>
        /// <param name="values">The array of values that the source bindings in the <see cref="T:System.Windows.Data.MultiBinding" /> produces. The value <see cref="F:System.Windows.DependencyProperty.UnsetValue" /> indicates that the source binding has no value to provide for conversion.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value.If the method returns null, the valid null value is used.A return value of <see cref="T:System.Windows.DependencyProperty" />.<see cref="F:System.Windows.DependencyProperty.UnsetValue" /> indicates that the converter did not produce a value, and that the binding will use the <see cref="P:System.Windows.Data.BindingBase.FallbackValue" /> if it is available, or else will use the default value.A return value of <see cref="T:System.Windows.Data.Binding" />.<see cref="F:System.Windows.Data.Binding.DoNothing" /> indicates that the binding does not transfer the value or use the <see cref="P:System.Windows.Data.BindingBase.FallbackValue" /> or the default value.
        /// </returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] == DependencyProperty.UnsetValue) return null;

            return (Orientation?) values[0] ?? (Orientation) values[1];
        }

        /// <summary>
        /// Converts a binding target value to the source binding values.
        /// </summary>
        /// <param name="value">The value that the binding target produces.</param>
        /// <param name="targetTypes">The array of types to convert to. The array length indicates the number and types of values that are suggested for the method to return.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// An array of values that have been converted from the target value back to the source values.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // This class connects the Y axis min and max input box to corresponding properties
    // The main purpose of this class is to handle Double.NaN.
    // When the min/max property is set to NaN, the input box will show empty string
    [ValueConversion(typeof(float), typeof(string))]
    public class StrFloatConverter : IValueConverter
    {
        // Convert method is from Source to Target. Source is Double and target is string
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
            (!Single.IsNaN((float)value)) ? ((float)value).ToString("f") : "";

        // ConvertBack method is from Target to Source
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((string)value == "")
                return Single.NaN;
            try
            {
                return Single.Parse((string)value);
            }
            catch
            {
                return Single.NaN;
            }
        }
    }
}