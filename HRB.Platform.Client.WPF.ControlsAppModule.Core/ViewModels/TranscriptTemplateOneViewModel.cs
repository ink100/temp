//using HRB.Platform.Client.WPF.Core.Services.IServices;

//namespace HRB.Platform.Client.WPF.ControlsAppModule.Core.ViewModels
//{
//    public class TranscriptTemplateOneViewModel : BaseTranscriptTemplateViewModel
//    {
//        #region Field

//        private const string PalpationStr = "-";

//        #endregion

//        #region Property

//        private string _memberName;
//        /// <summary>
//        /// 姓名
//        /// </summary>
//        public string MemberName
//        {
//            get => _memberName;
//            set => SetProperty(ref _memberName, value);
//        }

//        private string _memberNumber;

//        /// <summary>
//        /// 学号
//        /// </summary>
//        public string MemberNumber
//        {
//            get => _memberNumber;
//            set => SetProperty(ref _memberNumber, value);
//        }

//        private double _grade;

//        /// <summary>
//        /// 分数
//        /// </summary>
//        public double Grade
//        {
//            get => _grade;
//            set => SetProperty(ref _grade, value);
//        }

//        private string _assessmentModeStr = "Unknown";

//        /// <summary>
//        /// 考核模式字符串
//        /// </summary>
//        public string AssessmentModeStr
//        {
//            get => _assessmentModeStr;
//            set => SetProperty(ref _assessmentModeStr, value);
//        }

//        private TimeSpan _assessmentTotalTime;

//        /// <summary>
//        /// 考核总时长
//        /// </summary>
//        public TimeSpan AssessmentTotalTime
//        {
//            get => _assessmentTotalTime;
//            set => SetProperty(ref _assessmentTotalTime, value);
//        }

//        private DateTime _assessmentDate;

//        /// <summary>
//        /// 考核日期
//        /// </summary>
//        public DateTime AssessmentDate
//        {
//            get => _assessmentDate;
//            set => SetProperty(ref _assessmentDate, value);
//        }

//        private double _systolicBloodPressureCorrectRate;
//        /// <summary>
//        /// 收缩压正确率
//        /// </summary>
//        public double SystolicBloodPressureCorrectRate
//        {
//            get => _systolicBloodPressureCorrectRate;
//            set => SetProperty(ref _systolicBloodPressureCorrectRate, value);
//        }

//        private ushort _systolicBloodPressureInput;

//        /// <summary>
//        /// 收缩压填写值
//        /// </summary>
//        public ushort SystolicBloodPressureInput
//        {
//            get => _systolicBloodPressureInput;
//            set => SetProperty(ref _systolicBloodPressureInput, value);
//        }

//        private ushort _systolicBloodPressureSet;

//        /// <summary>
//        /// 收缩压设定值
//        /// </summary>
//        public ushort SystolicBloodPressureSet
//        {
//            get => _systolicBloodPressureSet;
//            set => SetProperty(ref _systolicBloodPressureSet, value);
//        }

//        private double _diastolicPressureCorrectRate;

//        /// <summary>
//        /// 舒张压正确率
//        /// </summary>
//        public double DiastolicPressureCorrectRate
//        {
//            get => _diastolicPressureCorrectRate;
//            set => SetProperty(ref _diastolicPressureCorrectRate, value);
//        }

//        private string _diastolicPressureInput = PalpationStr;

//        /// <summary>
//        /// 舒张压输入值
//        /// </summary>
//        public string DiastolicPressureInput
//        {
//            get => _diastolicPressureInput;
//            set => SetProperty(ref _diastolicPressureInput, value);
//        }

//        private string _diastolicPressureSet = PalpationStr;

//        /// <summary>
//        /// 舒张压设定值
//        /// </summary>
//        public string DiastolicPressureSet
//        {
//            get => _diastolicPressureSet;
//            set => SetProperty(ref _diastolicPressureSet, value);
//        }

//        private double _pulseRateCorrectRate;

//        /// <summary>
//        /// 脉率正确率
//        /// </summary>
//        public double PulseRateCorrectRate
//        {
//            get => _pulseRateCorrectRate;
//            set => SetProperty(ref _pulseRateCorrectRate, value);
//        }

//        private ushort _pulseRateInput;

//        /// <summary>
//        /// 脉率输入值
//        /// </summary>
//        public ushort PulseRateInput
//        {
//            get => _pulseRateInput;
//            set => SetProperty(ref _pulseRateInput, value);
//        }

//        private ushort _pulseRateSet;

//        /// <summary>
//        /// 脉率设定值
//        /// </summary>
//        public ushort PulseRateSet
//        {
//            get => _pulseRateSet;
//            set => SetProperty(ref _pulseRateSet, value);
//        }

//        private double _systolicBloodPressureScore;

//        /// <summary>
//        /// 收缩压得分
//        /// </summary>
//        public double SystolicBloodPressureScore
//        {
//            get => _systolicBloodPressureScore;
//            set => SetProperty(ref _systolicBloodPressureScore, value);
//        }

//        private double _systolicBloodPressureLessScore;
//        /// <summary>
//        /// 收缩压扣分
//        /// </summary>
//        public double SystolicBloodPressureLessScore
//        {
//            get => _systolicBloodPressureLessScore;
//            set => SetProperty(ref _systolicBloodPressureLessScore, value);
//        }

//        private string _diastolicPressureScore = PalpationStr;

//        /// <summary>
//        /// 舒张压得分
//        /// </summary>
//        public string DiastolicPressureScore
//        {
//            get => _diastolicPressureScore;
//            set => SetProperty(ref _diastolicPressureScore, value);
//        }

//        private string _diastolicPressureLessScore = PalpationStr;

//        /// <summary>
//        /// 舒张压扣分
//        /// </summary>
//        public string DiastolicPressureLessScore
//        {
//            get => _diastolicPressureLessScore;
//            set => SetProperty(ref _diastolicPressureLessScore, value);
//        }

//        private double _pulseRateScore;

//        /// <summary>
//        /// 脉率得分
//        /// </summary>
//        public double PulseRateScore
//        {
//            get => _pulseRateScore;
//            set => SetProperty(ref _pulseRateScore, value);
//        }

//        private double _pulseRateLessScore;

//        /// <summary>
//        /// 脉率扣分
//        /// </summary>
//        public double PulseRateLessScore
//        {
//            get => _pulseRateLessScore;
//            set => SetProperty(ref _pulseRateLessScore, value);
//        }

//        private double _boostBlockingValueScore;

//        /// <summary>
//        /// 增压阻断值得分
//        /// </summary>
//        public double BoostBlockingValueScore
//        {
//            get => _boostBlockingValueScore;
//            set => SetProperty(ref _boostBlockingValueScore, value);
//        }

//        private double _boostBlockingValueLessScore;

//        /// <summary>
//        /// 增压阻断值扣分
//        /// </summary>
//        public double BoostBlockingValueLessScore
//        {
//            get => _boostBlockingValueLessScore;
//            set => SetProperty(ref _boostBlockingValueLessScore, value);
//        }

//        private string _auscultatorySilenceScore = PalpationStr;

//        /// <summary>
//        /// 听诊无音间歇得分
//        /// </summary>
//        public string AuscultatorySilenceScore
//        {
//            get => _auscultatorySilenceScore;
//            set => SetProperty(ref _auscultatorySilenceScore, value);
//        }

//        private string _auscultatorySilenceLessScore = PalpationStr;

//        /// <summary>
//        /// 听诊无音间歇扣分
//        /// </summary>
//        public string AuscultatorySilenceLessScore
//        {
//            get => _auscultatorySilenceLessScore;
//            set => SetProperty(ref _auscultatorySilenceLessScore, value);
//        }

//        private double _deflationRateScore;

//        /// <summary>
//        /// 放气速度分数
//        /// </summary>
//        public double DeflationRateScore
//        {
//            get => _deflationRateScore;
//            set => SetProperty(ref _deflationRateScore, value);
//        }

//        private double _deflationRateLessScore;

//        /// <summary>
//        /// 放气速度扣分
//        /// </summary>
//        public double DeflationRateLessScore
//        {
//            get => _deflationRateLessScore;
//            set => SetProperty(ref _deflationRateLessScore, value);
//        }

//        private string _brachialArteryPalpationScore = PalpationStr;

//        /// <summary>
//        /// 肱动脉触诊分数 
//        /// </summary>
//        public string BrachialArteryPalpationScore
//        {
//            get => _brachialArteryPalpationScore;
//            set => SetProperty(ref _brachialArteryPalpationScore, value);
//        }

//        private string _brachialArteryPalpationLessScore = PalpationStr;

//        /// <summary>
//        /// 肱动脉触诊扣分
//        /// </summary>
//        public string BrachialArteryPalpationLessScore
//        {
//            get => _brachialArteryPalpationLessScore;
//            set => SetProperty(ref _brachialArteryPalpationLessScore, value);
//        }

//        private double _cuffPositionScore;

//        /// <summary>
//        ///  袖带位置分数
//        /// </summary>
//        public double CuffPositionScore
//        {
//            get => _cuffPositionScore;
//            set => SetProperty(ref _cuffPositionScore, value);
//        }

//        private double _cuffPositionLessScore;

//        /// <summary>
//        ///  袖带位置扣分
//        /// </summary>
//        public double CuffPositionLessScore
//        {
//            get => _cuffPositionLessScore;
//            set => SetProperty(ref _cuffPositionLessScore, value);
//        }

//        private double _onTimeScore;

//        /// <summary>
//        /// 按时完成得分
//        /// </summary>
//        public double OnTimeScore
//        {
//            get => _onTimeScore;
//            set => SetProperty(ref _onTimeScore, value);
//        }

//        private double _onTimeLessScore;

//        /// <summary>
//        /// 按时完成扣分
//        /// </summary>
//        public double OnTimeLessScore
//        {
//            get => _onTimeLessScore;
//            set => SetProperty(ref _onTimeLessScore, value);
//        }




//        #endregion

//        #region Method

//        public override void InitChart(byte assessmentMode, ushort boostBlockingValue, ushort systolicPressure,
//            ushort diastolicPressure, bool isAuscultatorySilence, List<ushort> auscultatorySilenceList)
//        {
//            base.InitChart(assessmentMode, boostBlockingValue, systolicPressure, diastolicPressure, isAuscultatorySilence, auscultatorySilenceList);
//        }

//        #endregion

//        public TranscriptTemplateOneViewModel(
//            ControlsAppContext appContext,
//            IEventAggregator eventAggregator,
//            IRegionManager regionManager,
//            IWpfDeviceRequestService deviceRequestService) : base(appContext, eventAggregator, regionManager, deviceRequestService)
//        {
//        }

//    }
//}