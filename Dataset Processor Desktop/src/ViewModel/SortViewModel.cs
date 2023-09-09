﻿using Dataset_Processor_Desktop.src.Enums;
using Dataset_Processor_Desktop.src.Utilities;

using SmartData.Lib.Enums;
using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;

namespace Dataset_Processor_Desktop.src.ViewModel
{
    public class SortViewModel : BaseViewModel
    {
        private readonly IFileManipulatorService _fileManipulatorService;

        private string _inputFolderPath;
        public string InputFolderPath
        {
            get => _inputFolderPath;
            set
            {
                _inputFolderPath = value;
                OnPropertyChanged(nameof(InputFolderPath));
            }
        }

        private string _outputFolderPath;
        public string OutputFolderPath
        {
            get => _outputFolderPath;
            set
            {
                _outputFolderPath = value;
                OnPropertyChanged(nameof(OutputFolderPath));
            }
        }

        private string _discardedFolderPath;
        public string DiscardedFolderPath
        {
            get => _discardedFolderPath;
            set
            {
                _discardedFolderPath = value;
                OnPropertyChanged(nameof(DiscardedFolderPath));
            }
        }

        private string _backupFolderPath;
        public string BackupFolderPath
        {
            get => _backupFolderPath;
            set
            {
                _backupFolderPath = value;
                OnPropertyChanged(nameof(BackupFolderPath));
            }
        }

        private bool _backupImages = false;
        public bool BackupImages
        {
            get => _backupImages;
            set
            {
                _backupImages = value;
                OnPropertyChanged(nameof(BackupImages));
            }
        }

        private SupportedDimensions _dimension;
        public SupportedDimensions Dimension
        {
            get => _dimension;
            set
            {
                _dimension = value;
                OnPropertyChanged(nameof(Dimension));
            }
        }

        private Progress _sortProgress;
        public Progress SortProgress
        {
            get => _sortProgress;
            set
            {
                _sortProgress = value;
                OnPropertyChanged(nameof(SortProgress));
            }
        }

        private bool _isUiEnabled;
        public bool IsUiEnabled
        {
            get => _isUiEnabled;
            set
            {
                _isUiEnabled = value;
                OnPropertyChanged(nameof(IsUiEnabled));
            }
        }

        public RelayCommand SelectInputFolderCommand { get; private set; }
        public RelayCommand SelectOutputFolderCommand { get; private set; }
        public RelayCommand SelectDiscardedFolderCommand { get; private set; }
        public RelayCommand SelectBackupFolderCommand { get; private set; }
        public RelayCommand SortImagesCommand { get; private set; }

        public RelayCommand OpenInputFolderCommand { get; private set; }
        public RelayCommand OpenOutputFolderCommand { get; private set; }
        public RelayCommand OpenDiscardedFolderCommand { get; private set; }
        public RelayCommand OpenBackupFolderCommand { get; private set; }

        public SortViewModel(IFileManipulatorService fileManipulatorService)
        {
            _fileManipulatorService = fileManipulatorService;

            DiscardedFolderPath = _configsService.Configurations.DiscardedFolder;
            _fileManipulatorService.CreateFolderIfNotExist(DiscardedFolderPath);
            OutputFolderPath = _configsService.Configurations.SelectedFolder;
            _fileManipulatorService.CreateFolderIfNotExist(OutputFolderPath);
            BackupFolderPath = _configsService.Configurations.BackupFolder;
            _fileManipulatorService.CreateFolderIfNotExist(BackupFolderPath);

            SelectInputFolderCommand = new RelayCommand(async () => await SelectInputFolderAsync());
            SelectOutputFolderCommand = new RelayCommand(async () => await SelectOutputFolderAsync());
            SelectDiscardedFolderCommand = new RelayCommand(async () => await SelectDiscardedFolderAsync());
            SelectBackupFolderCommand = new RelayCommand(async () => await SelectBackupFolderAsync());
            SortImagesCommand = new RelayCommand(async () => await SortImagesAsync());

            OpenInputFolderCommand = new RelayCommand(async () => await OpenFolderAsync(InputFolderPath));
            OpenOutputFolderCommand = new RelayCommand(async () => await OpenFolderAsync(OutputFolderPath));
            OpenDiscardedFolderCommand = new RelayCommand(async () => await OpenFolderAsync(DiscardedFolderPath));
            OpenBackupFolderCommand = new RelayCommand(async () => await OpenFolderAsync(BackupFolderPath));

            Dimension = SupportedDimensions.Resolution512x512;
            IsUiEnabled = true;

            TaskStatus = ProcessingStatus.Idle;
        }

        public async Task SelectInputFolderAsync()
        {
            var result = await _folderPickerService.PickFolderAsync();
            if (!string.IsNullOrEmpty(result))
            {
                InputFolderPath = result;
            }
        }
        public async Task SelectOutputFolderAsync()
        {
            var result = await _folderPickerService.PickFolderAsync();
            if (!string.IsNullOrEmpty(result))
            {
                OutputFolderPath = result;
            }
        }
        public async Task SelectDiscardedFolderAsync()
        {
            var result = await _folderPickerService.PickFolderAsync();
            if (!string.IsNullOrEmpty(result))
            {
                DiscardedFolderPath = result;
            }
        }
        public async Task SelectBackupFolderAsync()
        {
            var result = await _folderPickerService.PickFolderAsync();
            if (!string.IsNullOrEmpty(result))
            {
                BackupFolderPath = result;
            }
        }

        public async Task SortImagesAsync()
        {
            IsUiEnabled = false;

            if (SortProgress == null)
            {
                SortProgress = new Progress();
            }
            if (SortProgress.PercentFloat != 0f)
            {
                SortProgress.Reset();
            }

            if (BackupImages)
            {
                TaskStatus = ProcessingStatus.BackingUp;
                await _fileManipulatorService.BackupFilesAsync(_inputFolderPath, _backupFolderPath);
                TaskStatus = ProcessingStatus.Idle;
            }

            TaskStatus = ProcessingStatus.Running;
            try
            {
                await _fileManipulatorService.SortImagesAsync(_inputFolderPath, _discardedFolderPath, _outputFolderPath, SortProgress, Dimension);
            }
            catch (Exception exception)
            {
                _loggerService.LatestLogMessage = $"Something went wrong! Error log will be saved inside the logs folder.";
                await _loggerService.SaveExceptionStackTrace(exception);
            }
            finally
            {
                IsUiEnabled = true;
                TaskStatus = ProcessingStatus.Finished;
            }
        }
    }
}