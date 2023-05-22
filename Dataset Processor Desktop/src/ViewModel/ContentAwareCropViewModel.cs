﻿using Dataset_Processor_Desktop.src.Enums;
using Dataset_Processor_Desktop.src.Utilities;

using Microsoft.UI.Xaml;

using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;

using System.Diagnostics;

namespace Dataset_Processor_Desktop.src.ViewModel
{
    public class ContentAwareCropViewModel : BaseViewModel
    {
        private readonly IFileManipulatorService _fileManipulatorService;
        private readonly IContentAwareCropService _contentAwareCropService;

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

        private Progress _cropProgress;
        public Progress CropProgress
        {
            get => _cropProgress;
            set
            {
                _cropProgress = value;
                OnPropertyChanged(nameof(CropProgress));
            }
        }

        private double _scoreThreshold;
        public double ScoreThreshold
        {
            get => _scoreThreshold;
            set
            {
                if (Math.Round(value, 2) != _scoreThreshold)
                {
                    _scoreThreshold = Math.Round(value, 2);
                    OnPropertyChanged(nameof(ScoreThreshold));
                }
            }
        }

        private double _iouThreshold;
        public double IouThreshold
        {
            get => _iouThreshold;
            set
            {
                if (Math.Round(value, 2) != _iouThreshold)
                {
                    _iouThreshold = Math.Round(value, 2);
                    OnPropertyChanged(nameof(IouThreshold));
                }
            }
        }

        private double _expansionPercentage;
        public double ExpansionPercentage
        {
            get => _expansionPercentage;
            set
            {
                if (Math.Round(value, 2) != _expansionPercentage)
                {
                    _expansionPercentage = Math.Round(value, 2);
                    OnPropertyChanged(nameof(ExpansionPercentage));
                }
            }
        }

        private readonly Stopwatch _timer;
        public TimeSpan ElapsedTime
        {
            get => _timer.Elapsed;
        }

        public RelayCommand SelectInputFolderCommand { get; private set; }
        public RelayCommand SelectOutputFolderCommand { get; private set; }
        public RelayCommand OpenInputFolderCommand { get; private set; }
        public RelayCommand OpenOutputFolderCommand { get; private set; }
        public RelayCommand CropImagesCommand { get; private set; }

        public ContentAwareCropViewModel(IFileManipulatorService fileManipulatorService, IContentAwareCropService contentAwareCropService)
        {
            _fileManipulatorService = fileManipulatorService;
            _contentAwareCropService = contentAwareCropService;

            InputFolderPath = _configsService.Configurations.SelectedFolder;
            _fileManipulatorService.CreateFolderIfNotExist(InputFolderPath);
            OutputFolderPath = _configsService.Configurations.ResizedFolder;
            _fileManipulatorService.CreateFolderIfNotExist(OutputFolderPath);

            ScoreThreshold = 0.5f;
            IouThreshold = 0.4f;
            ExpansionPercentage = 0.1f;

            SelectInputFolderCommand = new RelayCommand(async () => await SelectInputFolderAsync());
            SelectOutputFolderCommand = new RelayCommand(async () => await SelectOutputFolderAsync());
            OpenInputFolderCommand = new RelayCommand(async () => await OpenFolderAsync(InputFolderPath));
            OpenOutputFolderCommand = new RelayCommand(async () => await OpenFolderAsync(OutputFolderPath));

            CropImagesCommand = new RelayCommand(async () => await CropImagesAsync());

            _timer = new Stopwatch();
            TaskStatus = ProcessingStatus.Idle;
        }

        public async Task SelectInputFolderAsync()
        {
            string result = await _folderPickerService.PickFolderAsync();
            if (!string.IsNullOrEmpty(result))
            {
                InputFolderPath = result;
            }
        }

        public async Task SelectOutputFolderAsync()
        {
            string result = await _folderPickerService.PickFolderAsync();
            if (!string.IsNullOrEmpty(result))
            {
                OutputFolderPath = result;
            }
        }

        public async Task CropImagesAsync()
        {
            if (CropProgress == null)
            {
                CropProgress = new Progress();
            }
            if (CropProgress.PercentFloat != 0f)
            {
                CropProgress.Reset();
            }

            _timer.Reset();
            TaskStatus = ProcessingStatus.Running;
            _contentAwareCropService.ScoreThreshold = (float)ScoreThreshold;
            _contentAwareCropService.IouThreshold = (float)IouThreshold;
            _contentAwareCropService.ExpansionPercentage = (float)ExpansionPercentage + 1.0f;

            try
            {
                _timer.Start();
                DispatcherTimer timer = new DispatcherTimer()
                {
                    Interval = TimeSpan.FromMilliseconds(100)
                };
                timer.Tick += (s, e) => OnPropertyChanged(nameof(ElapsedTime));
                timer.Start();

                await _contentAwareCropService.ProcessCroppedImage(InputFolderPath, OutputFolderPath, CropProgress);
            }
            catch (Exception exception)
            {
                _loggerService.LatestLogMessage = $"Something went wrong! {exception.StackTrace}";
            }
            finally
            {
                TaskStatus = ProcessingStatus.Finished;
                _timer.Stop();
            }
        }
    }
}
