using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Vesta.MVVM
{
    abstract class ViewModelBase : ObservableObject
    {
        protected Window _ActiveWindow = null;
        protected Action _ExitAction = null;
        protected bool _ActionCompleted = false;

        public virtual void CloseWindow()
        {
            if (_ActiveWindow != null)
            {
                _ActiveWindow.Close();
                ExecuteExitAction();
            }
        }

        public virtual void ExecuteExitAction()
        {
            if (_ExitAction != null)
            {
                _ExitAction.Invoke(); 
            }
            _ActionCompleted = true;
        }

        ~ViewModelBase()
        {
            if (!_ActionCompleted && _ExitAction != null)
            {
                _ExitAction.Invoke();
            }
        }
    }
}
