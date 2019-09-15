using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using InWit.Core.Utils;
using InWit.Core.Collections;

namespace InWit.WPF.MultiRangeSlider
{
    ///CPOL License (code project) https://www.codeproject.com/info/cpol10.aspx
    /// <summary>
    /// Interaction logic for WitMultiRangeSlider.xaml
    /// </summary>
    public partial class WitMultiRangeSlider : UserControl, INotifyPropertyChanged
    {
        #region events
#pragma warning disable 67
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
#pragma warning restore 67

        public event EventHandler<WitMultiRangeSliderBarClickedEventArgs> MultiRangeSliderBarClicked = delegate { };
        #endregion

        #region DependencyProperties

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.RegisterAttached("ItemsSource", typeof(IEnumerable), typeof(WitMultiRangeSlider),
                                                new FrameworkPropertyMetadata(ItemsSourceChanged));

        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.RegisterAttached("Items", typeof(ObservableContentCollection<WitMultiRangeSliderItem>), typeof(WitMultiRangeSlider),
                                        new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.RegisterAttached("SelectedItem", typeof(object), typeof(WitMultiRangeSlider),
                                                new FrameworkPropertyMetadata(SelectedItemChanged));

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.RegisterAttached("Minimum", typeof(double), typeof(WitMultiRangeSlider),
                                                new FrameworkPropertyMetadata(0.0, MinimumChanged));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.RegisterAttached("Maximum", typeof(double), typeof(WitMultiRangeSlider),
                                                new FrameworkPropertyMetadata(100.0, MaximumChanged));

        public static readonly DependencyProperty TickFrequencyProperty =
            DependencyProperty.RegisterAttached("TickFrequency", typeof(double), typeof(WitMultiRangeSlider),
                                                new FrameworkPropertyMetadata(1.0, TickFrequencyhanged));

        public static readonly DependencyProperty IsSnapToTickEnabledProperty =
            DependencyProperty.RegisterAttached("IsSnapToTickEnabled", typeof(bool), typeof(WitMultiRangeSlider),
                                                new FrameworkPropertyMetadata(true, IsSnapToTickEnabledChanged));


        private static void ItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var multiRangeSlider = d as WitMultiRangeSlider;

            if (multiRangeSlider == null) return;

            if(multiRangeSlider.Items.Count > 0)
                throw new ExceptionOf<WitMultiRangeSlider>("You can not set ItemsSource and Items simultaneously");

            if (multiRangeSlider.ItemsSource is INotifyCollectionChanged)
                (multiRangeSlider.ItemsSource as INotifyCollectionChanged).CollectionChanged += (_,__) => multiRangeSlider.ResetSliders();

            multiRangeSlider.ResetSliders();
        }

        public void ResetItems()
        {
            InitSliders(false);
        }

        private static void SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var multiRangeSlider = d as WitMultiRangeSlider;

            if (multiRangeSlider == null) return;

            multiRangeSlider.ResetSelectedSlider();
        }

        private static void MinimumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var multiRangeSlider = d as WitMultiRangeSlider;

            if (multiRangeSlider == null) return;

            foreach (var item in multiRangeSlider.Items)
                item.Minimum = multiRangeSlider.Minimum;
        }

        private static void MaximumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var multiRangeSlider = d as WitMultiRangeSlider;

            if (multiRangeSlider == null) return;

            foreach (var item in multiRangeSlider.Items)
                item.Maximum = multiRangeSlider.Maximum;

        }

        private static void TickFrequencyhanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var multiRangeSlider = d as WitMultiRangeSlider;

            if (multiRangeSlider == null) return;

            foreach (var item in multiRangeSlider.Items)
                item.TickFrequency = multiRangeSlider.TickFrequency;

        }

        private static void IsSnapToTickEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var multiRangeSlider = d as WitMultiRangeSlider;

            if (multiRangeSlider == null) return;

            foreach (var item in multiRangeSlider.Items)
                item.IsSnapToTickEnabled = multiRangeSlider.IsSnapToTickEnabled;

        }

        #endregion

        #region Fields

        private Binding m_leftValueBinding;
        private Binding m_rightValueBinding;

        private readonly double m_thumbWidth;

        #endregion

        #region Constructors

        public WitMultiRangeSlider()
        {
            InitializeComponent();

            m_thumbWidth = (double) FindResource("HorizontalSliderThumbWidth");

            Items = new ObservableContentCollection<WitMultiRangeSliderItem>();
            Items.CollectionChanged += SlidersCollectionChanged;
            Items.CollectionContentChanged += SlidersCollectionContentChanged;

            PropertyChanged += (_, __) => ResetSliders();
            SizeChanged += (_, __) => ArrangeSliders();
        }

        #endregion

        #region Functions

        private void ResetSliders()
        {
            ClearSliders();

            if (ItemsSource != null)
                CreateSliders();
        }

        private void ClearSliders()
        {
            var slidersToRemove = Items.ToArray();

            foreach (var slider in slidersToRemove)
            {
                BindingOperations.ClearBinding(slider, WitMultiRangeSliderItem.RightValueProperty);
                BindingOperations.ClearBinding(slider, WitMultiRangeSliderItem.LeftValueProperty);
                BindingOperations.ClearBinding(slider, WitMultiRangeSliderItem.MinimumValueProperty);
                BindingOperations.ClearBinding(slider, WitMultiRangeSliderItem.MaximumValueProperty);

                Items.Remove(slider);
            }
        }

        private void CreateSliders()
        {
            foreach (var item in ItemsSource)
                Items.Add(CreateSlider(item));
            
            InitSliders();
            
        }

        private void InitSliders(bool init = true)
        {
            Items.First().IsFirst = true;

            for(int i = 0; i < Items.Count; i++)
            {
                InitSliderMinimum(i > 0? Items[i-1] : null, Items[i]);
                InitSliderMaximum(Items[i], i < Items.Count - 1 ? Items[i + 1] : null);
            }

            if (init)
                Items.Add(CreateLastSliderFromItem(Items.Last()));
            else Items.Last().MaximumValue = Maximum;

            ArrangeSliders();
        }

        private void ArrangeSliders()
        {
            var nValues = Items.Count - 1;

            for (int i = 0; i < nValues; i++)
            {
                Items[i].Maximum = Maximum + ThumbValue * (nValues - i);
                Items[i].Minimum = Minimum - ThumbValue * i;
            }

            Items.Last().Minimum = Minimum - ThumbValue * nValues;
        }


        private WitMultiRangeSliderItem CreateSlider(object item)
        {
            var slider = new WitMultiRangeSliderItem { Item = item };

            slider.SetBinding(WitMultiRangeSliderItem.LeftValueProperty, new Binding { Source = item, Path = LeftValueBinding.Path, Mode = LeftValueBinding.Mode });
            slider.SetBinding(WitMultiRangeSliderItem.RightValueProperty, new Binding { Source = item, Path = RightValueBinding.Path, Mode = RightValueBinding.Mode });

            return slider;
        }

        private WitMultiRangeSliderItem CreateLastSliderFromItem(WitMultiRangeSliderItem lastItem)
        {
            var slider = new WitMultiRangeSliderItem
            {
                Item = null,
                IsLast = true,
                MaximumValue = Maximum
            };

            slider.SetBinding(WitMultiRangeSliderItem.LeftValueProperty, GetBinding(lastItem, x => x.RightValue));
            slider.SetBinding(WitMultiRangeSliderItem.MinimumValueProperty, GetBinding(lastItem, x => x.LeftValue));

            return slider;

        }

        private void InitSliderMaximum(WitMultiRangeSliderItem slider, WitMultiRangeSliderItem nextSlider)
        {
            slider.SetBinding(WitMultiRangeSliderItem.MaximumValueProperty, nextSlider == null ? GetBinding(slider, x => x.RightValue) : GetBinding(nextSlider, x => x.LeftValue));
        }

        private void InitSliderMinimum(WitMultiRangeSliderItem previousSlider, WitMultiRangeSliderItem slider)
        {
            if (previousSlider == null)
                slider.MinimumValue = Minimum;
            else
                slider.SetBinding(WitMultiRangeSliderItem.MinimumValueProperty, GetBinding(previousSlider, x => x.LeftValue));
        }

        private Binding GetBinding(WitMultiRangeSliderItem slider, Expression<Func<WitMultiRangeSliderItem, double>> expression)
        {
            return new Binding { Source = slider, Path = new PropertyPath(Extensions.NameOfProperty(expression)), Mode = BindingMode.TwoWay };
        }

        private void ResetSelectedSlider()
        {
            var selectedSlider = Items.Single(x => x.Item == SelectedItem);

            if (!selectedSlider.IsSelected)
                selectedSlider.IsSelected = true;

            foreach (var slider in Items.Where(slider => !slider.Equals(selectedSlider)))
                slider.IsSelected = false;
        }

        #region SlidersCollection Events
        private void SlidersCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
                Root.Children.Add(ApplyParentStyle(e.NewItems.Cast<WitMultiRangeSliderItem>().Single()));
            if (e.Action == NotifyCollectionChangedAction.Remove)
                Root.Children.Remove(e.OldItems.Cast<WitMultiRangeSliderItem>().Single());

            if (Items != null && Items.Count > 0)
                Visibility = Visibility.Visible;
            else Visibility = Visibility.Collapsed;
        }

        private void SlidersCollectionContentChanged(object sender, PropertyChangedEventArgs e)
        {
            var senderSlider = sender as WitMultiRangeSliderItem;
            if (senderSlider == null) return;
            if (e.IsProperty((WitMultiRangeSliderItem item) => item.IsSelected) && senderSlider.IsSelected)
                SelectedItem = senderSlider.Item;
        }

        private WitMultiRangeSliderItem ApplyParentStyle(WitMultiRangeSliderItem item)
        {
            item.Minimum = Minimum;
            item.Maximum = Maximum;
            item.TickFrequency = TickFrequency;
            item.IsSnapToTickEnabled = IsSnapToTickEnabled;

            return item;
        }
        #endregion

        #region UserControl Events
        private void WitMultiRangeSliderLoaded(object sender, RoutedEventArgs e)
        {
            if (Items.Count > 0 && ItemsSource == null)
                InitSliders();
        }

        private void SliderBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2) return;

            var position = Math.Round(e.GetPosition(TrackBackground).X * Maximum / (TrackBackground.ActualWidth * TickFrequency));
            position *= TickFrequency;

            if (MultiRangeSliderBarClicked != null)
                MultiRangeSliderBarClicked(this, new WitMultiRangeSliderBarClickedEventArgs(position));
        }
        #endregion

        #endregion

        #region Properties

        private double ThumbValue
        {
            get { return ActualWidth > 0? m_thumbWidth * (Maximum - Minimum)/ActualWidth : 0; }
        }

        #region DependencyProperties

        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public ObservableContentCollection<WitMultiRangeSliderItem> Items
        {
            get { return (ObservableContentCollection<WitMultiRangeSliderItem>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        public object SelectedItem
        {
            get { return GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public double TickFrequency
        {
            get { return (double)GetValue(TickFrequencyProperty); }
            set { SetValue(TickFrequencyProperty, value); }
        }

        public bool IsSnapToTickEnabled
        {
            get { return (bool)GetValue(IsSnapToTickEnabledProperty); }
            set { SetValue(IsSnapToTickEnabledProperty, value); }
        }
        #endregion

        #region Bindings

        public Binding LeftValueBinding
        {
            get { return m_leftValueBinding; }
            set
            {
                m_leftValueBinding = value;
                this.FirePropertyChanged();
            }
        }

        public Binding RightValueBinding
        {
            get { return m_rightValueBinding; }
            set
            {
                m_rightValueBinding = value;
                this.FirePropertyChanged();
            }
        }


        #endregion

        #endregion
    }

    public class WitMultiRangeSliderBarClickedEventArgs : EventArgs
    {
        public WitMultiRangeSliderBarClickedEventArgs(double position)
        {
            Position = position;
        }

        public double Position { get; private set; }
    }
}
