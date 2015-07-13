using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Vesta.Misc;
using Vesta.MVVM;

namespace Vesta.ViewModels
{
    class ProcessingViewModel : ViewModelBase
    {
        private int _TotalShrinkers = 1;
        private int _ShrinkersDone = 0;

        public ProcessingViewModel(IEnumerable<PdfShrinker> shrinkers,
            int threadCount, Window activeWindow, bool showReport)
        {
            _TotalShrinkers = shrinkers.Count();

            _ActiveWindow = activeWindow;

            ShowReport = showReport;

            ActiveShrinkers = new ObservableCollection<PdfShrinker>();
            CompletedShrinkers = new ObservableCollection<PdfShrinker>();

            CollectionSplitter<PdfShrinker> splitter = new CollectionSplitter<PdfShrinker>();
            IEnumerable<IEnumerable<PdfShrinker>> masterList = 
                splitter.SplitCollection(shrinkers, threadCount);

            StartProcessing(masterList);
        }

        private void StartProcessing(IEnumerable<IEnumerable<PdfShrinker>> masterList)
        {
            foreach (IEnumerable<PdfShrinker> subList in masterList)
            {
                Thread thread = new Thread(() =>
                {
                    ProcessShrinkers(subList);
                });
                thread.Start();
            }
        }

        private void ProcessShrinkers(IEnumerable<PdfShrinker> subList)
        {
            foreach (PdfShrinker shrinker in subList)
            {
                _ActiveWindow.Dispatcher.Invoke(() =>
                {
                    ActiveShrinkers.Add(shrinker);
                });

                shrinker.ShrinkPdf();
                _ShrinkersDone++;
                RaisePropertyChangedEvent("TotalSavedNiceString");
                RaisePropertyChangedEvent("OverallProgress");

                _ActiveWindow.Dispatcher.Invoke(() =>
                {
                    ActiveShrinkers.Remove(shrinker);
                    CompletedShrinkers.Add(shrinker);
                });
            }
        }

        private ObservableCollection<PdfShrinker> _Shrinkers;

        public ObservableCollection<PdfShrinker> Shrinkers
        {
            get { return _Shrinkers; }
            set
            {
                _Shrinkers = value;
                RaisePropertyChangedEvent("Shrinkers");
            }
        }

        private ObservableCollection<PdfShrinker> _ActiveShrinkers;

        public ObservableCollection<PdfShrinker> ActiveShrinkers
        {
            get { return _ActiveShrinkers; }
            set
            {
                _ActiveShrinkers = value;
                RaisePropertyChangedEvent("ActiveShrinkers");
            }
        }

        private ObservableCollection<PdfShrinker> _CompletedShrinkers;

        public ObservableCollection<PdfShrinker> CompletedShrinkers
        {
            get { return _CompletedShrinkers; }
            set
            {
                _CompletedShrinkers = value;
                RaisePropertyChangedEvent("CompletedShrinkers");
            }
        }

        private bool _CloseWhenDone;

        public bool CloseWhenDone
        {
            get { return _CloseWhenDone; }
            set
            {
                _CloseWhenDone = value;
                RaisePropertyChangedEvent("CloseWhenDone");
            }
        }

        private bool _ShowReport;

        public bool ShowReport
        {
            get { return _ShowReport; }
            set
            {
                _ShowReport = value;
                RaisePropertyChangedEvent("ShowReport");
            }
        }

        public double OverallProgress
        {
            get
            {
                return ((double)_ShrinkersDone / (double)_TotalShrinkers);            
            }
        }


        public long TotalSaved
        {
            get
            {
                long originalTotal = CompletedShrinkers.Sum(f => f.OriginalSize);
                long newTotal = CompletedShrinkers.Sum(f => f.NewSize);
                return originalTotal - newTotal;
            }
        }


        private double _Gig = 1073741824;
        private double _Meg = 1048576;
        private double _Kil = 1024;
        public string TotalSavedNiceString
        {
            get
            {
                long saved = TotalSaved;
                if (saved > _Gig) { return ((double)(saved / _Gig)).ToString("0.00 GB"); }
                if (saved > _Meg) { return ((double)(saved / _Meg)).ToString("0.00 MB"); }
                return ((double)(saved / _Kil)).ToString("0.00 KB");
            }
        }
        



    }
}
