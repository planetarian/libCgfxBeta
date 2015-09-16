using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using libCgfx;

namespace ReadCgfxGui.ViewModel
{
    public class MainViewModel : ViewModelBaseExtended
    {
        public ObservableCollection<LogEntry> Log
        {
            get { return _Log; }
            set { SetProperty(() => Log, ref _Log, value); }
        }
        private ObservableCollection<LogEntry> _Log;

        public LogEntry CurrentLogEntry
        {
            get { return _CurrentLogEntry; }
            set { SetProperty(() => CurrentLogEntry, ref _CurrentLogEntry, value); }
        }
        private LogEntry _CurrentLogEntry;


        public int IndentLevel
        {
            get { return _IndentLevel; }
            set { SetProperty(() => IndentLevel, ref _IndentLevel, value); }
        }
        private int _IndentLevel;

        public int Verbosity
        {
            get { return _Verbosity; }
            set { SetProperty(() => Verbosity, ref _Verbosity, value); }
        }
        private int _Verbosity;

        public string FileName
        {
            get { return _FileName; }
            set { SetProperty(() => FileName, ref _FileName, value); }
        }
        private string _FileName;

        public RelayCommand OpenCgfxCommand
        {
            get { return _OpenCgfxCommand; }
            set { SetProperty(() => OpenCgfxCommand, ref _OpenCgfxCommand, value); }
        }
        private RelayCommand _OpenCgfxCommand;

        public Cgfx CgfxObject
        {
            get { return _CgfxObject; }
            set { SetProperty(() => CgfxObject, ref _CgfxObject, value); }
        }
        private Cgfx _CgfxObject;


        public MainViewModel()
        {
            OpenCgfxCommand = new RelayCommand(OpenCgfx);
            Verbosity = 3;
        }

        public void OpenCgfx()
        {
            IndentLevel = 0;
            var rootEntry = new LogEntry
            {
                Message = Path.GetFileName(FileName),
                Entries = new ObservableCollection<LogEntry>()
            };
            CurrentLogEntry = rootEntry;
            Log = new ObservableCollection<LogEntry> {rootEntry};
#if !DEBUG
            try
            {
#endif
                CgfxObject = new Cgfx(FileName, LogMessage);
                LogMessage("Total bytes:   " + CgfxObject.FileSize, 0, 0);
                LogMessage("Covered bytes: " + CgfxObject.Coverage.BytesCovered, 0, 0);
                int i = 0;
                int end = -1;
                var uncovered = new Coverage();
                foreach (CoverageNode node in CgfxObject.Coverage)
                {
                    int start = node.OffsetStart;
                    if (end >= 0 || start != 0)
                        uncovered.Add(end + 1, start - 1);
                    end = node.OffsetEnd;
                    string startExpanded = CtrObject.DisplayValue(node.OffsetStart);
                    string endExpanded = CtrObject.DisplayValue(node.OffsetEnd);
                    LogMessage($"{i++} {startExpanded} - {endExpanded} ({node.Length})", 1, 0);
                }

                int last = CgfxObject.FileSize - 1;
                if (end < last)
                    uncovered.Add(end, last);

                LogMessage("Uncovered bytes: " + uncovered.BytesCovered, 0, 0);
                foreach (CoverageNode node in uncovered)
                {
                    string startExpanded = CtrObject.DisplayValue(node.OffsetStart);
                    string endExpanded = CtrObject.DisplayValue(node.OffsetEnd);
                    LogMessage($"{i++} {startExpanded} - {endExpanded} ({node.Length})", 1, 0);
                }

#if !DEBUG
            }
            catch (Exception ex)
            {
                FileName = "Error: " + ex.Message;
            }
#endif
        }

        private void LogMessage(string message, int indentLevel, int verbosityLevel)
        {
            if (verbosityLevel > Verbosity) return;

            while (IndentLevel < indentLevel)
            {
                CurrentLogEntry = CurrentLogEntry.Entries.Last();
                IndentLevel++;
            }
            while (IndentLevel > indentLevel)
            {
                CurrentLogEntry = CurrentLogEntry.ParentEntry;
                IndentLevel--;
            }

            var newEntry = new LogEntry
            {
                Message = message,
                ParentEntry = CurrentLogEntry,
                Entries = new ObservableCollection<LogEntry>()
            };

            //if (verbosityLevel >= Verbosity)
                CurrentLogEntry.Entries.Add(newEntry);
        }
    }

    public class LogEntry
    {
        public string Message { get; set; }
        public LogEntry ParentEntry { get; set; }
        public ObservableCollection<LogEntry> Entries { get; set; }
    }
}