using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WPFLocalizeExtension.Engine;
using System.Globalization;

namespace Hoi4BlueprintEditor.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        public ICommand SetLanguageToEnglishCommand { get; }
        public ICommand SetLanguageToChineseCommand { get; }

        public MainViewModel()
        {
            SetLanguageToEnglishCommand = new RelayCommand(SetLanguageToEnglish);
            SetLanguageToChineseCommand = new RelayCommand(SetLanguageToChinese);
        }

        private void SetLanguage(string cultureCode)
        {
            var newCulture = new CultureInfo(cultureCode);
            LocalizeDictionary.Instance.Culture = newCulture;
            Thread.CurrentThread.CurrentUICulture = newCulture;
        }

        private void SetLanguageToEnglish() => SetLanguage("en-US");
        private void SetLanguageToChinese() => SetLanguage("zh-CN");
    }
}