using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Hoi4BlueprintEditor.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private void SetLanguage(string cultureCode)
        {
            var newCulture = new CultureInfo(cultureCode);
            Thread.CurrentThread.CurrentUICulture = newCulture;
            CultureInfo.CurrentUICulture = newCulture;
            CultureInfo.CurrentCulture = newCulture;
        }

        [RelayCommand]
        private void SetLanguageToEnglish() => SetLanguage("en-US");

        [RelayCommand]
        private void SetLanguageToChinese() => SetLanguage("zh-CN");
    }
}
