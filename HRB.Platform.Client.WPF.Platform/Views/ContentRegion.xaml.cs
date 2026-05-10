using HRB.Platform.Client.WPF.Core.CustomAttributes;
using HRB.Platform.Client.WPF.Platform.Core;
using HRB.Platform.Client.WPF.Platform.Core.ViewModels;

namespace HRB.Platform.Client.WPF.Platform.Views
{
    [IocBindViewModelForNavigation(typeof(ContentRegionViewModel), GlobalSettings.APP_MODULE_NAME)]
    public partial class ContentRegion
    {
        public ContentRegion()
        {
            InitializeComponent();
        }
    }
}