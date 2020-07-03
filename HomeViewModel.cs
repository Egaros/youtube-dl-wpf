﻿using MaterialDesignThemes.Wpf;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Input;

namespace youtube_dl_wpf
{
    public class HomeViewModel : ViewModelBase
    {
        public HomeViewModel(ISnackbarMessageQueue snackbarMessageQueue)
        {
            _snackbarMessageQueue = snackbarMessageQueue ?? throw new ArgumentNullException(nameof(snackbarMessageQueue));

            _link = "";
            _overrideFormats = AppSettings.settings.OverrideFormats;
            _videoFormat = AppSettings.settings.VideoFormat;
            _audioFormat = AppSettings.settings.AudioFormat;
            _metadata = true;
            _thumbnail = true;
            _subtitles = true;
            _playlist = false;
            _customPath = AppSettings.settings.CustomPath;
            _downloadPath = AppSettings.settings.DownloadPath;
            _output = "";

            _browseFolder = new DelegateCommand(OnBrowseFolder);
            _openFolder = new DelegateCommand(OnOpenFolder, CanOpenFolder);
            _startDownload = new DelegateCommand(OnStartDownload, CanStartDownload);
            _listFormats = new DelegateCommand(OnListFormats, CanStartDownload);
            _abortDl = new DelegateCommand(OnAbortDl, (object commandParameter) => _freezeButton);

            if (!String.IsNullOrEmpty(AppSettings.settings.DlPath) && AppSettings.settings.AutoUpdateDl)
                UpdateDl();
        }

        private string _link;
        private bool _overrideFormats;
        private string _videoFormat;
        private string _audioFormat;
        private bool _metadata;
        private bool _thumbnail;
        private bool _subtitles;
        private bool _playlist;
        private bool _customPath;
        private string _downloadPath;
        private string _output;

        private StringBuilder outputString = null!;
        private bool _freezeButton = false; // true for freezing the button
        private Process dlProcess = null!;

        private readonly ISnackbarMessageQueue _snackbarMessageQueue;
        private readonly DelegateCommand _browseFolder;
        private readonly DelegateCommand _openFolder;
        private readonly DelegateCommand _startDownload;
        private readonly DelegateCommand _listFormats;
        private readonly DelegateCommand _abortDl;

        public ICommand BrowseFolder => _browseFolder;
        public ICommand OpenFolder => _openFolder;
        public ICommand StartDownload => _startDownload;
        public ICommand ListFormats => _listFormats;
        public ICommand AbortDl => _abortDl;

        private void PrepareDlProcess()
        {
            dlProcess = new Process();
            dlProcess.StartInfo.FileName = AppSettings.settings.DlPath;
            dlProcess.StartInfo.CreateNoWindow = true;
            dlProcess.StartInfo.UseShellExecute = false;
            dlProcess.StartInfo.RedirectStandardError = true;
            dlProcess.StartInfo.RedirectStandardOutput = true;
            dlProcess.EnableRaisingEvents = true;
            dlProcess.ErrorDataReceived += DlOutputHandler;
            dlProcess.OutputDataReceived += DlOutputHandler;
            dlProcess.Exited += DlProcess_Exited;
        }

        private void DlProcess_Exited(object? sender, EventArgs e)
        {
            dlProcess.Dispose();
            FreezeButton = false;
            _startDownload.InvokeCanExecuteChanged();
            _listFormats.InvokeCanExecuteChanged();
            _abortDl.InvokeCanExecuteChanged();
        }

        private void OnBrowseFolder(object commandParameter)
        {
            Microsoft.Win32.OpenFileDialog folderDialog = new Microsoft.Win32.OpenFileDialog
            {
                FileName = "Folder Selection.",
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true
            };

            if ((string)commandParameter == "DownloadPath")
                folderDialog.InitialDirectory = DownloadPath;

            bool? result = folderDialog.ShowDialog();

            if (result == true)
            {
                if ((string)commandParameter == "DownloadPath")
                    DownloadPath = Path.GetDirectoryName(folderDialog.FileName) ?? "";
            }
        }

        private void OnOpenFolder(object commandParameter)
        {
            try
            {
                Utilities.OpenLink(_downloadPath);
            }
            catch (Exception ex)
            {
                Output = ex.Message;
            }
        }

        private void OnStartDownload(object commandParameter)
        {
            FreezeButton = true;
            _startDownload.InvokeCanExecuteChanged();
            _listFormats.InvokeCanExecuteChanged();
            _abortDl.InvokeCanExecuteChanged();

            outputString = new StringBuilder();
            PrepareDlProcess();

            try
            {
                // make parameter list
                if (!String.IsNullOrEmpty(AppSettings.settings.Proxy))
                {
                    dlProcess.StartInfo.ArgumentList.Add("--proxy");
                    dlProcess.StartInfo.ArgumentList.Add($"{AppSettings.settings.Proxy}");
                }
                if (!String.IsNullOrEmpty(AppSettings.settings.FfmpegPath))
                {
                    dlProcess.StartInfo.ArgumentList.Add("--ffmpeg-location");
                    dlProcess.StartInfo.ArgumentList.Add($"{AppSettings.settings.FfmpegPath}");
                }
                if (_overrideFormats && !String.IsNullOrEmpty(_videoFormat) && !String.IsNullOrEmpty(_audioFormat))
                {
                    dlProcess.StartInfo.ArgumentList.Add("-f");
                    dlProcess.StartInfo.ArgumentList.Add($"{_videoFormat}+{_audioFormat}");
                }
                if (_metadata)
                    dlProcess.StartInfo.ArgumentList.Add("--add-metadata");
                if (_thumbnail)
                    dlProcess.StartInfo.ArgumentList.Add("--embed-thumbnail");
                if (_subtitles)
                {
                    dlProcess.StartInfo.ArgumentList.Add("--write-sub");
                    dlProcess.StartInfo.ArgumentList.Add("--embed-subs");
                }
                if (_playlist)
                {
                    dlProcess.StartInfo.ArgumentList.Add("--yes-playlist");
                }
                else
                {
                    dlProcess.StartInfo.ArgumentList.Add("--no-playlist");
                }
                if (_customPath)
                {
                    dlProcess.StartInfo.ArgumentList.Add("-o");
                    dlProcess.StartInfo.ArgumentList.Add($@"{_downloadPath}\%(title)s-%(id)s.%(ext)s");
                }
                dlProcess.StartInfo.ArgumentList.Add($"{_link}");
                // start download
                dlProcess.Start();
                dlProcess.BeginErrorReadLine();
                dlProcess.BeginOutputReadLine();
            }
            catch (Exception ex)
            {
                outputString.Append(ex.Message);
                outputString.Append(Environment.NewLine);
                Output = outputString.ToString();
            }
            finally
            {
            }
        }

        private void OnListFormats(object commandParameter)
        {
            FreezeButton = true;
            _startDownload.InvokeCanExecuteChanged();
            _listFormats.InvokeCanExecuteChanged();
            _abortDl.InvokeCanExecuteChanged();

            outputString = new StringBuilder();
            PrepareDlProcess();

            try
            {
                // make parameter list
                if (!String.IsNullOrEmpty(AppSettings.settings.Proxy))
                {
                    dlProcess.StartInfo.ArgumentList.Add("--proxy");
                    dlProcess.StartInfo.ArgumentList.Add($"{AppSettings.settings.Proxy}");
                }
                dlProcess.StartInfo.ArgumentList.Add($"-F");
                dlProcess.StartInfo.ArgumentList.Add($"{_link}");
                // start download
                dlProcess.Start();
                dlProcess.BeginErrorReadLine();
                dlProcess.BeginOutputReadLine();
            }
            catch (Exception ex)
            {
                outputString.Append(ex.Message);
                outputString.Append(Environment.NewLine);
                Output = outputString.ToString();
            }
            finally
            {
            }
        }

        private void OnAbortDl(object commandParameter)
        {
            try
            {
                // yes, I know it's bad to just kill the process.
                // but currently .NET Core doesn't have an API for sending ^C or SIGTERM to a process
                // see https://github.com/dotnet/runtime/issues/14628
                // To implement a platform-specific solution,
                // we need to use Win32 APIs.
                // see https://stackoverflow.com/questions/283128/how-do-i-send-ctrlc-to-a-process-in-c
                // I would prefer not to use Win32 APIs in the application.
                dlProcess.Kill();
                outputString.Append("🛑 Aborted.");
                outputString.Append(Environment.NewLine);
                Output = outputString.ToString();
            }
            catch (Exception ex)
            {
                Output = ex.Message;
            }
        }

        private bool CanOpenFolder(object commandParameter)
        {
            return !String.IsNullOrEmpty(_downloadPath) && Directory.Exists(_downloadPath);
        }

        private bool CanStartDownload(object commandParameter)
        {
            return !String.IsNullOrEmpty(Link) && !String.IsNullOrEmpty(AppSettings.settings.DlPath) && !_freezeButton;
        }

        private void UpdateDl()
        {
            FreezeButton = true;
            _startDownload.InvokeCanExecuteChanged();
            _listFormats.InvokeCanExecuteChanged();
            _abortDl.InvokeCanExecuteChanged();

            outputString = new StringBuilder();
            PrepareDlProcess();

            try
            {
                // make parameter list
                if (!String.IsNullOrEmpty(AppSettings.settings.Proxy))
                {
                    dlProcess.StartInfo.ArgumentList.Add("--proxy");
                    dlProcess.StartInfo.ArgumentList.Add($"{AppSettings.settings.Proxy}");
                }
                dlProcess.StartInfo.ArgumentList.Add($"-U");
                // start update
                dlProcess.Start();
                dlProcess.BeginErrorReadLine();
                dlProcess.BeginOutputReadLine();
            }
            catch (Exception ex)
            {
                outputString.Append(ex.Message);
                outputString.Append(Environment.NewLine);
                Output = outputString.ToString();
            }
            finally
            {
            }
        }

        private void DlOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                outputString.Append(outLine.Data);
                outputString.Append(Environment.NewLine);
                Output = outputString.ToString();
            }
        }

        public string Link
        {
            get => _link;
            set
            {
                SetProperty(ref _link, value);
                _startDownload.InvokeCanExecuteChanged();
                _listFormats.InvokeCanExecuteChanged();
                if (String.IsNullOrEmpty(AppSettings.settings.DlPath))
                    _snackbarMessageQueue.Enqueue("youtube-dl path is not set. Go to settings and set the path.");
            }
        }

        public bool OverrideFormats
        {
            get => _overrideFormats;
            set
            {
                SetProperty(ref _overrideFormats, value);
                AppSettings.settings.OverrideFormats = _overrideFormats;
                AppSettings.SaveSettings();
            }
        }

        public string VideoFormat
        {
            get => _videoFormat;
            set
            {
                SetProperty(ref _videoFormat, value);
                AppSettings.settings.VideoFormat = _videoFormat;
                AppSettings.SaveSettings();
            }
        }

        public string AudioFormat
        {
            get => _audioFormat;
            set
            {
                SetProperty(ref _audioFormat, value);
                AppSettings.settings.AudioFormat = _audioFormat;
                AppSettings.SaveSettings();
            }
        }

        public bool Metadata
        {
            get => _metadata;
            set => SetProperty(ref _metadata, value);
        }

        public bool Thumbnail
        {
            get => _thumbnail;
            set => SetProperty(ref _thumbnail, value);
        }

        public bool Subtitles
        {
            get => _subtitles;
            set => SetProperty(ref _subtitles, value);
        }

        public bool Playlist
        {
            get => _playlist;
            set => SetProperty(ref _playlist, value);
        }

        public bool CustomPath
        {
            get => _customPath;
            set
            {
                SetProperty(ref _customPath, value);
                AppSettings.settings.CustomPath = _customPath;
                AppSettings.SaveSettings();
            }
        }

        public string DownloadPath
        {
            get => _downloadPath;
            set
            {
                SetProperty(ref _downloadPath, value);
                _openFolder.InvokeCanExecuteChanged();
                AppSettings.settings.DownloadPath = _downloadPath;
                AppSettings.SaveSettings();
            }
        }

        public string Output
        {
            get => _output;
            set => SetProperty(ref _output, value);
        }

        public bool FreezeButton
        {
            get => _freezeButton;
            set => SetProperty(ref _freezeButton, value);
        }
    }
}
