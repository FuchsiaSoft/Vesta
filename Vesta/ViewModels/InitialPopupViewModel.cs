using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Vesta.MVVM;
using Vesta.Properties;

namespace Vesta.ViewModels
{
    class InitialPopupViewModel : ViewModelBase
    {
        public InitialPopupViewModel
            (Window activeWindow = null, Action exitAction = null)
        {
            _ActiveWindow = activeWindow;
            _ExitAction = exitAction;
        }

        private bool _HideFuture;

        public bool HideFuture
        {
            get { return _HideFuture; }
            set
            {
                _HideFuture = value;
                RaisePropertyChangedEvent("HideFuture");
            }
        }

        public ICommand OkCommand { get { return new DelegateCommand(Ok); } }

        private void Ok()
        {
            if (HideFuture)
            {
                Settings.Default.ShowInitialPopup = false;
                Settings.Default.Save();
            }
            CloseWindow();
        }
    }
}
