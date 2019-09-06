using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using InWit.Core.Utils;

namespace InWit.WPF.MultiRangeSlider
{
    public class WitMultiRangeSliderItem : Slider, INotifyPropertyChanged
    {
        #region events
#pragma warning disable 67
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
#pragma warning restore 67
        #endregion

        #region DependencyProperties

        public static readonly DependencyProperty LeftValueProperty =
            DependencyProperty.RegisterAttached("LeftValue", typeof(double), typeof(WitMultiRangeSliderItem),
                                                new FrameworkPropertyMetadata(double.MinValue, LeftValueChanged));

        public static readonly DependencyProperty RightValueProperty =
            DependencyProperty.RegisterAttached("RightValue", typeof(double), typeof(WitMultiRangeSliderItem),
                                                new FrameworkPropertyMetadata(double.MaxValue));

        public static readonly DependencyProperty MinimumValueProperty =
            DependencyProperty.RegisterAttached("MinimumValue", typeof(double), typeof(WitMultiRangeSliderItem),
                                                new FrameworkPropertyMetadata(double.MinValue));

        public static readonly DependencyProperty MaximumValueProperty =
            DependencyProperty.RegisterAttached("MaximumValue", typeof(double), typeof(WitMultiRangeSliderItem),
                                                new FrameworkPropertyMetadata(double.MaxValue, MaximumValueChanged));

        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.RegisterAttached("IsSelected", typeof(bool), typeof(WitMultiRangeSliderItem),
                                        new FrameworkPropertyMetadata(false));


        private static void LeftValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sliderItem = d as WitMultiRangeSliderItem;

            sliderItem.Value = (double)e.NewValue;

        }

        private static void MaximumValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sliderItem = d as WitMultiRangeSliderItem;

            sliderItem.RightValue = (double)e.NewValue;

        }

        #endregion

        #region Fields

        private object m_item;
        private bool m_isBlocked;

        #endregion

        #region Constructors

        public WitMultiRangeSliderItem()
        {
            IsFirst = false;
            IsLast = false;

            Maximum = double.MaxValue;
            Minimum = double.MinValue;
        }

        #endregion

        #region Functions

        protected override void OnValueChanged(double oldValue, double newValue)
        {
            if(m_isBlocked) return;

            m_isBlocked = true;

            var validatedValue = ValidateValue(newValue);

            if (!double.IsNaN(validatedValue))
            {
                Value = validatedValue;
                LeftValue = validatedValue;
            }
            else
            {
                Value = oldValue;
            }

            m_isBlocked = false;

        }

        protected override void OnThumbDragStarted(DragStartedEventArgs e)
        {
            if(Item != null) IsSelected = true;

            base.OnThumbDragStarted(e);
        }

        private double ValidateValue(double value)
        {
            if (value > MinimumValue + TickFrequency && value < MaximumValue - TickFrequency)
                return value;
            if (Math.Abs(value - MaximumValue) < TickFrequency)
                return IsLast ? MaximumValue : (MaximumValue - TickFrequency);
            if (Math.Abs(value - MinimumValue) < TickFrequency)
                return IsFirst ? MinimumValue : (MinimumValue + TickFrequency);

            return double.NaN;
        }

        #endregion

        #region Properties

        public object Item
        {
            get { return m_item; }
            set
            {
                m_item = value;
                this.FirePropertyChanged();
            }
        }
        
        public bool IsFirst { get; set; }

        public bool IsLast { get; set; }

        public double LeftValue
        {
            get { return (double)GetValue(LeftValueProperty); }
            set { SetValue(LeftValueProperty, value); }
        }

        public double RightValue
        {
            get { return (double)GetValue(RightValueProperty); }
            set { SetValue(RightValueProperty, value); }
        }

        public double MaximumValue
        {
            get { return (double)GetValue(MaximumValueProperty); }
            set { SetValue(MaximumValueProperty, value); }
        }

        public double MinimumValue
        {
            get { return (double)GetValue(MinimumValueProperty); }
            set { SetValue(MinimumValueProperty, value); }
        }

        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set
            {
                SetValue(IsSelectedProperty, value);
                this.FirePropertyChanged();
            }
        }

        #endregion
    }
}
