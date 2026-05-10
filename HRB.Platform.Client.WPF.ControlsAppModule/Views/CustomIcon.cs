//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Media;

//namespace HRB.Platform.Client.WPF.ControlsAppModule.Views
//{
//    /// <summary>
//    /// CustomIcon.xaml 的交互逻辑
//    /// </summary>
//    public class CustomIcon : Button
//    {
//        static CustomIcon()
//        {
//            DefaultStyleKeyProperty.OverrideMetadata(typeof(CustomIcon), new FrameworkPropertyMetadata(typeof(CustomIcon)));
//        }


//        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
//            nameof(Icon), typeof(string), typeof(CustomIcon), new PropertyMetadata(default(string)));

//        public string Icon
//        {
//            get => (string)GetValue(IconProperty);
//            set => SetValue(IconProperty, value);
//        }


//        public static readonly DependencyProperty PressedIconColorProperty = DependencyProperty.Register(
//            nameof(PressedIconColor), typeof(Brush), typeof(CustomIcon), new PropertyMetadata(Brushes.RoyalBlue));

//        public Brush PressedIconColor
//        {
//            get => (Brush)GetValue(PressedIconColorProperty);
//            set => SetValue(PressedIconColorProperty, value);
//        }


//        public static readonly DependencyProperty HoveredIconColorProperty = DependencyProperty.Register(
//            nameof(HoveredIconColor), typeof(Brush), typeof(CustomIcon),
//            new PropertyMetadata(Brushes.LightBlue));

//        public Brush HoveredIconColor
//        {
//            get => (Brush)GetValue(HoveredIconColorProperty);
//            set => SetValue(HoveredIconColorProperty, value);
//        }


//        public static readonly DependencyProperty DisabledIconColorProperty = DependencyProperty.Register(
//            nameof(DisabledIconColor), typeof(Brush), typeof(CustomIcon), new PropertyMetadata(Brushes.Gray));

//        public Brush DisabledIconColor
//        {
//            get => (Brush)GetValue(DisabledIconColorProperty);
//            set => SetValue(DisabledIconColorProperty, value);
//        }


//        public static readonly DependencyProperty LightingIconColorProperty = DependencyProperty.Register(
//            nameof(LightingIconColor), typeof(Brush), typeof(CustomIcon), new PropertyMetadata(Brushes.LightGreen));

//        public Brush LightingIconColor
//        {
//            get => (Brush)GetValue(LightingIconColorProperty);
//            set => SetValue(LightingIconColorProperty, value);
//        }


//        public static readonly DependencyProperty IsLightedProperty = DependencyProperty.Register(
//            nameof(IsLighted), typeof(bool), typeof(CustomIcon), new PropertyMetadata(default(bool)));

//        public bool IsLighted
//        {
//            get => (bool)GetValue(IsLightedProperty);
//            set => SetValue(IsLightedProperty, value);
//        }

//    }
//}
