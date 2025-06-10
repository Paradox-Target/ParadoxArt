using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

namespace Hoi4BlueprintEditor.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        //属性变更通知
        private string _title = "Hoi4 Blueprint Editor";

        public string Title //属性绑定窗口
        {
            get => _title;
            set => SetProperty(ref _title, value); //自动处理变更通知
        }

        public MainViewModel()
        {
            //初始化
        }
    }
}