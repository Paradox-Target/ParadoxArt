using System.Configuration;
using System.Data;
using System.Globalization;
using System.Windows;
using WPFLocalizeExtension.Engine;

namespace Hoi4BlueprintEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            LocalizeDictionary.Instance.SetCurrentThreadCulture = true;
            LocalizeDictionary.Instance.Culture = Thread.CurrentThread.CurrentUICulture;

            Thread.CurrentThread.CurrentUICulture = new CultureInfo(LocalizeDictionary.Instance.Culture.Name);
        }
    }
}