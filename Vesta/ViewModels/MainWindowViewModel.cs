using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Vesta.Misc;
using Vesta.MVVM;

namespace Vesta.ViewModels
{
    internal class SelectedFileInfo
    {
        private double _Gig = 1073741824;
        private double _Meg = 1048576;
        private double _Kil = 1024;
        public string Name { get; set; }
        public string Size
        {
            get
            {
                if (Length > _Gig) { return ((double)(Length / _Gig)).ToString("0.00 GB"); }
                if (Length > _Meg) { return ((double)(Length / _Meg)).ToString("0.00 MB"); }
                return ((double)(Length / _Kil)).ToString("0.00 KB");
            }
        }
        public long Length { get; set; }
        public string FullName { get; set; }
        public bool IsSelected { get; set; }
    }

    class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel(Window activeWindow)
        {
            _ActiveWindow = activeWindow;

            Overwrite = true;
            JpegEncoding = 0.1;
            PreserveMetaData = true;
            PreserveAttributes = true;
            PreserveCreation = true;
            PreserveModified = true;
            ShowReport = false;
            ThreadCount = 8;

            MarkFree();
        }

        #region Observable Properties

        private ObservableCollection<SelectedFileInfo> _PdfFiles;

        public ObservableCollection<SelectedFileInfo> PdfFiles
        {
            get
            {
                if (_PdfFiles == null) _PdfFiles = new ObservableCollection<SelectedFileInfo>();
                return _PdfFiles;
            }
            set
            {
                _PdfFiles = value;
                RaisePropertyChangedEvent("PdfFiles");
            }
        }

        private bool _Overwrite;

        public bool Overwrite
        {
            get { return _Overwrite; }
            set
            {
                _Overwrite = value;
                RaisePropertyChangedEvent("Overwrite");
            }
        }

        private bool _SaveNew;

        public bool SaveNew
        {
            get { return _SaveNew; }
            set
            {
                _SaveNew = value;
                RaisePropertyChangedEvent("SaveNew");
            }
        }

        private string _NewPath;

        public string NewPath
        {
            get { return _NewPath; }
            set
            {
                _NewPath = value;
                RaisePropertyChangedEvent("NewPath");
            }
        }

        private double _JpegEncoding;

        public double JpegEncoding
        {
            get { return _JpegEncoding; }
            set
            {
                _JpegEncoding = value;
                RaisePropertyChangedEvent("JpegEncoding");
            }
        }

        private bool _PreserveMetaData;

        public bool PreserveMetaData
        {
            get { return _PreserveMetaData; }
            set
            {
                _PreserveMetaData = value;
                RaisePropertyChangedEvent("PreserveMetaData");
            }
        }


        private bool _PreserveCreation;

        public bool PreserveCreation
        {
            get { return _PreserveCreation; }
            set
            {
                _PreserveCreation = value;
                RaisePropertyChangedEvent("PreserveCreation");
            }
        }

        private bool _PreserveAccessed;

        public bool PreserveAccessed
        {
            get { return _PreserveAccessed; }
            set
            {
                _PreserveAccessed = value;
                RaisePropertyChangedEvent("PreserveAccessed");
            }
        }

        private bool _PreserveModified;

        public bool PreserveModified
        {
            get { return _PreserveModified; }
            set
            {
                _PreserveModified = value;
                RaisePropertyChangedEvent("PreserveModified");
            }
        }

        private bool _PreserveAttributes;

        public bool PreserveAttributes
        {
            get { return _PreserveAttributes; }
            set
            {
                _PreserveAttributes = value;
                RaisePropertyChangedEvent("PreserveAttributes");
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

        private int _ThreadCount;

        public int ThreadCount
        {
            get { return _ThreadCount; }
            set
            {
                _ThreadCount = value;
                RaisePropertyChangedEvent("ThreadCount");
            }
        }

       
        #endregion

        #region UI properties

        private bool _IsBusy;

        public bool IsBusy
        {
            get { return _IsBusy; }
            set
            {
                _IsBusy = value;
                RaisePropertyChangedEvent("IsBusy");
                RaisePropertyChangedEvent("EnableControls");
                RaisePropertyChangedEvent("Opacity");
            }
        }

        public double Opacity
        {
            get
            {
                if (IsBusy) return 0.5;
                return 1;
            }
        }

        public bool EnableControls
        {
            get { return IsBusy == false; }
        }

        private void MarkBusy()
        {
            IsBusy = true;
        }

        private void MarkFree()
        {
            IsBusy = false;
        }

        #endregion

        #region Commands

        public ICommand AddFilesCommand { get { return new DelegateCommand(AddFiles); } }

        private void AddFiles()
        {
            OpenFileDialog fdg = new OpenFileDialog()
            {
                CheckFileExists = true,
                Filter = "PDF files (*.pdf)|*.pdf",
                CheckPathExists = true,
                Multiselect = true,
                Title = "Select PDF files..."
            };

            bool? result = fdg.ShowDialog();

            if (result == true)
            {
                ConsumeFileStrings(fdg.FileNames);
            }
        }

        public void ConsumeFileStrings(IEnumerable<string> files, bool recursive = false)
        {
            Thread thread = new Thread(() =>
            {
                MarkBusy();
                DoFileProcessing(files, recursive);
                MarkFree();
            });
            thread.Start();
        }

        private void DoFileProcessing(IEnumerable<string> files, bool recursive)
        {
            foreach (string fileName in files)
            {
                FileInfo file = new FileInfo(fileName);
                if (file.Exists && file.Extension.ToUpper() == ".PDF")
                {
                    _ActiveWindow.Dispatcher.Invoke(() =>
                    {
                        PdfFiles.Add(new SelectedFileInfo()
                        {
                            Name = file.Name,
                            FullName = file.FullName,
                            IsSelected = false,
                            Length = file.Length
                        });
                    });
                }

                if (Directory.Exists(fileName) && recursive)
                {
                    DirectoryInfo folder = new DirectoryInfo(fileName);

                    List<string> subPaths = new List<string>();

                    foreach (FileInfo subFile in folder.EnumerateFiles())
                    {
                        subPaths.Add(subFile.FullName);
                    }
                    foreach (DirectoryInfo subFolder in folder.EnumerateDirectories())
                    {
                        subPaths.Add(subFolder.FullName);
                    }

                    DoFileProcessing(subPaths, recursive);
                }
            }
        }

        public ICommand RemoveSelectedCommand { get { return new DelegateCommand(RemoveSelected); } }

        private void RemoveSelected()
        {
            IEnumerable<SelectedFileInfo> filesToRemove =
                PdfFiles.Where(f => f.IsSelected == true).ToList();

            foreach (SelectedFileInfo file in filesToRemove)
            {
                PdfFiles.Remove(file);
            }
        
        }

        public ICommand ClearListCommand { get { return new DelegateCommand(ClearList); } }

        private void ClearList()
        {
            PdfFiles.Clear();
        }

        public ICommand ProcessCommand { get { return new DelegateCommand(Process); } }

        private void Process()
        {
            List<PdfShrinker> shrinkers = new List<PdfShrinker>();

            ShrinkOptions options = new ShrinkOptions()
            {
                EncodingQuality = (int)(JpegEncoding * 100),
                NewFolder = NewPath,
                SaveOption = Overwrite ? SaveOption.Overwrite : SaveOption.SaveNew,
                RetainAccessedDate = PreserveAccessed,
                RetainAttributes = PreserveAttributes,
                RetainCreationDate = PreserveCreation,
                RetainModifiedDate = PreserveModified
            };

            foreach (SelectedFileInfo file in PdfFiles)
            {
                shrinkers.Add(new PdfShrinker(new FileInfo(file.FullName), options));
            }

            ProcessingView view = new ProcessingView();
            view.DataContext = new ProcessingViewModel
                (shrinkers, ThreadCount, view, ShowReport);
            view.Show();
            CloseWindow();
        }

        #endregion

    }
}
