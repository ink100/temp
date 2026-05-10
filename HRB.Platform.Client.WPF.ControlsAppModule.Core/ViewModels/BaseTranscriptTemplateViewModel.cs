//using HRB.Platform.Client.WPF.Core.Services.IServices;
//using System.Windows;

//namespace HRB.Platform.Client.WPF.ControlsAppModule.Core.ViewModels
//{
//    public class BaseTranscriptTemplateViewModel : BaseControlsUserControlViewModel
//    {
//        #region Property

//        private Visibility _isShowPlotOne = Visibility.Hidden;

//        /// <summary>
//        /// 是否显示第一个表格
//        /// </summary>
//        public Visibility IsShowPlotOne
//        {
//            get => _isShowPlotOne;
//            set => SetProperty(ref _isShowPlotOne, value);
//        }

//        /// <summary>
//        /// 第一个表格的数据
//        /// </summary>
//        public PlotModel PlotModelOne { get; set; } = new();

//        public readonly LineSeries PlotLineOne = new() { StrokeThickness = 4 };

//        private Visibility _isShowPlotTwo = Visibility.Hidden;

//        /// <summary>
//        /// 是否显示第二个表格
//        /// </summary>
//        public Visibility IsShowPlotTwo
//        {
//            get => _isShowPlotTwo;
//            set => SetProperty(ref _isShowPlotTwo, value);
//        }

//        /// <summary>
//        /// 第二个表格的数据
//        /// </summary>
//        public PlotModel PlotModelTwo { get; set; } = new();

//        public readonly LineSeries PlotLineTwo = new() { StrokeThickness = 4 };

//        /// <summary>
//        /// 曲线图控制器
//        /// </summary>
//        public PlotController ModelController { get; set; } = new();
//        #endregion


//        #region Method


//        /// <summary>
//        /// 初始化图表
//        /// </summary>
//        /// <param name="assessmentMode">操作模式</param>
//        /// <param name="boostBlockingValue">增压阻断值</param>
//        /// <param name="systolicPressure">收缩压</param>
//        /// <param name="diastolicPressure">舒张压</param>
//        /// <param name="isAuscultatorySilence">是否有听诊无音间歇</param>
//        /// <param name="auscultatorySilenceList">听诊无音间歇范围</param>
//        /// <returns></returns>
//        public virtual void InitChart(byte assessmentMode,
//                                      ushort boostBlockingValue,
//                                      ushort systolicPressure,
//                                      ushort diastolicPressure,
//                                      bool isAuscultatorySilence,
//                                      List<ushort> auscultatorySilenceList)
//        {
//            // 1s 10个包，长度显示 3 分钟

//            var chartLengthSeriesByOne = new LineSeries { Color = OxyColor.FromUInt32(0xfc5D5F61) };

//            InitMainSeriesLength(chartLengthSeriesByOne, 3 * 60 * 10);

//            var chartLengthSeriesByTwo = new LineSeries() { Color = OxyColor.FromUInt32(0xfc5D5F61) };

//            InitMainSeriesLength(chartLengthSeriesByTwo, 3 * 60 * 10);

//            PlotModelOne.Axes.Add(new LinearAxis
//            {
//                MajorGridlineStyle = LineStyle.DashDotDot,
//                MajorGridlineColor = OxyColor.FromUInt32(0xfc5D5F61),
//                MajorGridlineThickness = 2,
//                Position = AxisPosition.Left,
//                Minimum = 0,
//                Maximum = 300,
//                MinorStep = 50,
//                MajorStep = 50,
//                MajorTickSize = 0,
//                TextColor = OxyColor.FromUInt32(0xff333333),
//                FontSize = 27
//            });
//            PlotModelOne.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, IsAxisVisible = false });

//            PlotModelTwo.Axes.Add(new LinearAxis
//            {
//                MajorGridlineStyle = LineStyle.DashDotDot,
//                MajorGridlineColor = OxyColor.FromUInt32(0xfc5D5F61),
//                MajorGridlineThickness = 2,
//                Position = AxisPosition.Left,
//                Minimum = 0,
//                Maximum = 300,
//                MinorStep = 50,
//                MajorStep = 50,
//                MajorTickSize = 0,
//                TextColor = OxyColor.FromUInt32(0xff333333),
//                FontSize = 27
//            });
//            PlotModelTwo.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, IsAxisVisible = false });

//            //收缩压+增压阻断值
//            PlotModelOne.Annotations.Add(new LineAnnotation
//            {
//                Color = OxyColor.FromUInt32(0xFFFF7D63),
//                Intercept = systolicPressure + boostBlockingValue,
//                LineStyle = LineStyle.Solid,
//                StrokeThickness = 3,
//            });

//            PlotModelTwo.Annotations.Add(new LineAnnotation
//            {
//                Color = OxyColor.FromUInt32(0xFFFF7D63),
//                Intercept = systolicPressure + boostBlockingValue,
//                LineStyle = LineStyle.Solid,
//                StrokeThickness = 3
//            });

//            switch (assessmentMode)
//            {
//                case 1:
//                    AuscultationChartSettings(systolicPressure, diastolicPressure, isAuscultatorySilence, auscultatorySilenceList);
//                    break;
//                case 2:
//                    PalpationChartSettings(systolicPressure);
//                    break;
//                case 3:
//                    AllChartSettings(systolicPressure, diastolicPressure, isAuscultatorySilence, auscultatorySilenceList);
//                    break;
//            }

//            PlotLineOne.Color = OxyColor.FromUInt32(0xFF27C8BA);
//            PlotLineTwo.Color = OxyColor.FromUInt32(0xFF27C8BA);

//            PlotModelOne.Series.Add(PlotLineOne);
//            PlotLineOne.LineLegendPosition = LineLegendPosition.Start;
//            PlotModelOne.Series.Add(chartLengthSeriesByOne);

//            PlotModelTwo.Series.Add(PlotLineTwo);
//            PlotLineTwo.LineLegendPosition = LineLegendPosition.Start;
//            PlotModelTwo.Series.Add(chartLengthSeriesByTwo);

//            //解绑所有控制事件
//            ModelController.UnbindAll();
//            PlotModelOne.PlotAreaBorderColor = OxyColor.FromUInt32(0xfc5D5F61);
//            PlotModelOne.PlotAreaBorderThickness = new OxyThickness(0, 0, 0, 1);
//            PlotModelTwo.PlotAreaBorderColor = OxyColor.FromUInt32(0xfc5D5F61);
//            PlotModelTwo.PlotAreaBorderThickness = new OxyThickness(0, 0, 0, 1);

//            PlotModelOne.InvalidatePlot(true);
//            PlotModelTwo.InvalidatePlot(true);
//        }

//        /// <summary>
//        /// 限定主轴长度
//        /// </summary>
//        /// <param name="lineSeries">主轴名称</param>
//        /// <param name="length">长度</param>
//        private void InitMainSeriesLength(LineSeries lineSeries, int length)
//        {

//            lineSeries.Points.Clear();
//            for (int i = 0; i < length; i++)
//            {
//                lineSeries.Points.Add(new DataPoint(i + 1, 0));
//            }
//        }

//        /// <summary>
//        /// 听诊图表设置
//        /// </summary>
//        /// <param name="systolicPressure">收缩压</param>
//        /// <param name="diastolicPressure">舒张压</param>
//        /// <param name="isAuscultatorySilence">是否有听诊无音间歇</param>
//        /// <param name="auscultatorySilenceList">听诊无音间歇区域</param>
//        private void AuscultationChartSettings(ushort systolicPressure, ushort diastolicPressure, bool isAuscultatorySilence, List<ushort> auscultatorySilenceList)
//        {
//            //收缩压 舒张压区域
//            PlotModelOne.Annotations.Add(new RectangleAnnotation
//            {
//                MaximumY = systolicPressure,
//                MinimumY = diastolicPressure,
//                Fill = OxyColor.FromUInt32(0x5C27C8BA)
//            });

//            PlotModelTwo.Annotations.Add(new RectangleAnnotation
//            {
//                MaximumY = systolicPressure,
//                MinimumY = diastolicPressure,
//                Fill = OxyColor.FromUInt32(0x5C27C8BA)
//            });


//            if (isAuscultatorySilence && auscultatorySilenceList.Any() && auscultatorySilenceList.Count >= 3)
//            {

//                PlotModelOne.Annotations.Add(new RectangleAnnotation
//                {
//                    MaximumY = auscultatorySilenceList[1],
//                    MinimumY = auscultatorySilenceList[2],
//                    Fill = OxyColor.FromUInt32(0x605F7088)
//                });

//                PlotModelTwo.Annotations.Add(new RectangleAnnotation
//                {
//                    MaximumY = auscultatorySilenceList[1],
//                    MinimumY = auscultatorySilenceList[2],
//                    Fill = OxyColor.FromUInt32(0x605F7088)
//                });
//            }




//        }

//        /// <summary>
//        /// 触诊图表设置
//        /// </summary>
//        private void PalpationChartSettings(ushort systolicPressure)
//        {
//            //收缩压
//            PlotModelOne.Annotations.Add(new LineAnnotation
//            {
//                Color = OxyColor.FromUInt32(0xFF27C8BA),
//                Intercept = systolicPressure,
//                LineStyle = LineStyle.Solid,
//                StrokeThickness = 2
//            });

//            PlotModelTwo.Annotations.Add(new LineAnnotation
//            {
//                Color = OxyColor.FromUInt32(0xFF27C8BA),
//                Intercept = systolicPressure,
//                LineStyle = LineStyle.Solid,
//                StrokeThickness = 2
//            });
//        }

//        /// <summary>
//        /// 听诊 + 触诊 图表设置
//        /// </summary>
//        private void AllChartSettings(ushort systolicPressure, ushort diastolicPressure, bool isAuscultatorySilence, List<ushort> auscultatorySilenceList)
//        {
//            AuscultationChartSettings(systolicPressure, diastolicPressure, isAuscultatorySilence, auscultatorySilenceList);
//        }

//        #endregion

//        public BaseTranscriptTemplateViewModel(
//            ControlsAppContext appContext,
//            IEventAggregator eventAggregator,
//            IRegionManager regionManager,
//            IWpfDeviceRequestService deviceRequestService) : base(appContext, eventAggregator, regionManager, deviceRequestService)
//        {

//        }
//    }
//}
