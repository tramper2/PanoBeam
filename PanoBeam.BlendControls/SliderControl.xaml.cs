using System.Windows;

namespace PanoBeam.BlendControls
{
    /// <summary>
    /// Interaction logic for SliderControl.xaml
    /// </summary>
    public partial class SliderControl
    {
        public event RoutedPropertyChangedEventHandler<double> ValueChanged;

        public SliderControl()
        {
            InitializeComponent();
            //DataContext = this;
            if (Content is FrameworkElement frameworkElement) frameworkElement.DataContext = this;
        }

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            "Title", typeof(string), typeof(SliderControl), new PropertyMetadata(default(string)));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value", typeof(double), typeof(SliderControl), new PropertyMetadata(default(double)));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
            "Minimum", typeof(double), typeof(SliderControl), new PropertyMetadata(default(double)));

        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
            "Maximum", typeof(double), typeof(SliderControl), new PropertyMetadata(default(double)));

        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public static readonly DependencyProperty SmallChangeProperty = DependencyProperty.Register(
            "SmallChange", typeof(double), typeof(SliderControl), new PropertyMetadata(default(double)));

        public double SmallChange
        {
            get => (double)GetValue(SmallChangeProperty);
            set => SetValue(SmallChangeProperty, value);
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ValueChanged?.Invoke(this, e);
        }
    }
}
