﻿using Dataset_Processor_Desktop.src.Utilities;

using SmartData.Lib.Interfaces;

namespace Dataset_Processor_Desktop.src.ViewModel
{
    public class TagEditorViewModel : BaseViewModel
    {
        private readonly IFileManipulatorService _fileManipulatorService;
        private readonly IImageProcessorService _imageProcessorService;

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

        private List<string> _imageFiles;
        public List<string> ImageFiles
        {
            get => _imageFiles;
            set
            {
                _imageFiles = value;
                OnPropertyChanged(nameof(ImageFiles));
            }
        }

        private int _selectedItemIndex;
        public int SelectedItemIndex
        {
            get => _selectedItemIndex;
            set
            {
                _selectedItemIndex = value;
                OnPropertyChanged(nameof(SelectedItemIndex));
                if (_imageFiles.Count > 0)
                {
                    SelectedImage = ImageSource.FromFile(_imageFiles[_selectedItemIndex]);
                }
            }
        }

        private ImageSource _selectedImage;
        public ImageSource SelectedImage
        {
            get => _selectedImage;
            set
            {
                try
                {
                    UpdateCurrentSelectedTags();
                }
                catch (Exception exception)
                {
                    _loggerService.LatestLogMessage = $".txt file for current image not found, just type in the editor and one will be created! Error: {exception.StackTrace}";
                }
                finally
                {
                    _selectedImage = value;
                    SelectedImageFilename = Path.GetFileName(_imageFiles[_selectedItemIndex]);
                }
                OnPropertyChanged(nameof(SelectedImage));
            }
        }

        private string _selectedImageFilename;
        public string SelectedImageFilename
        {
            get => _selectedImageFilename;
            set
            {
                _selectedImageFilename = value;
                OnPropertyChanged(nameof(SelectedImageFilename));
            }
        }

        public string _wordsToHighlight;
        public string WordsToHighlight
        {
            get => _wordsToHighlight;
            set
            {
                _wordsToHighlight = value;
                OnPropertyChanged(nameof(WordsToHighlight));
            }
        }

        public RelayCommand PreviousItemCommand { get; private set; }
        public RelayCommand PreviousTenItemsCommand { get; private set; }
        public RelayCommand NextItemCommand { get; private set; }
        public RelayCommand NextTenItemsCommand { get; private set; }
        public RelayCommand SelectInputFolderCommand { get; private set; }
        public RelayCommand BlurImageCommand { get; private set; }
        public RelayCommand OpenInputFolderCommand { get; private set; }

        private string _currentImageTags;

        private bool _showBlurredImage;

        private MemoryStream _currentImageMemoryStream = null;

        public string CurrentImageTags
        {
            get => _currentImageTags;
            set
            {
                _currentImageTags = value;
                OnPropertyChanged(nameof(CurrentImageTags));

                string txtFile = Path.ChangeExtension(_imageFiles[_selectedItemIndex], ".txt");
                _fileManipulatorService.SaveTagsForImage(txtFile, _currentImageTags);
            }
        }

        public TagEditorViewModel(IFileManipulatorService fileManipulatorService, IImageProcessorService imageProcessorService)
        {
            _fileManipulatorService = fileManipulatorService;
            _imageProcessorService = imageProcessorService;

            InputFolderPath = _configsService.Configurations.CombinedOutputFolder;
            _fileManipulatorService.CreateFolderIfNotExist(InputFolderPath);

            ImageFiles = _fileManipulatorService.GetImageFiles(InputFolderPath);

            PreviousItemCommand = new RelayCommand(GoToPreviousItem);
            PreviousTenItemsCommand = new RelayCommand(GoToPreviousTenItems);
            NextItemCommand = new RelayCommand(GoToNextItem);
            NextTenItemsCommand = new RelayCommand(GoToNextTenItems);
            SelectInputFolderCommand = new RelayCommand(async () => await SelectInputFolderAsync());
            BlurImageCommand = new RelayCommand(async () => await BlurImageAsync());
            OpenInputFolderCommand = new RelayCommand(async () => await OpenFolderAsync(InputFolderPath));

            SelectedItemIndex = 0;
        }

        public async Task SelectInputFolderAsync()
        {
            string result = await _folderPickerService.PickFolderAsync();
            if (!string.IsNullOrEmpty(result))
            {
                InputFolderPath = result;
                try
                {
                    ImageFiles = _fileManipulatorService.GetImageFiles(InputFolderPath);
                    if (ImageFiles.Count != 0)
                    {
                        ImageFiles = ImageFiles.OrderBy(x => int.Parse(Path.GetFileNameWithoutExtension(x))).ToList();
                    }
                }
                catch
                {
                    _loggerService.LatestLogMessage = "No image files were found in the directory.";
                }
                finally
                {
                    if (ImageFiles.Count != 0)
                    {
                        SelectedImage = ImageSource.FromFile(_imageFiles[_selectedItemIndex]);
                    }
                }
            }
        }

        public void GoToPreviousItem()
        {
            if (_selectedItemIndex > 0)
            {
                _selectedItemIndex--;
                SelectedImage = ImageSource.FromFile(_imageFiles[_selectedItemIndex]);
                OnPropertyChanged(nameof(SelectedItemIndex));
            }
        }

        public void GoToPreviousTenItems()
        {
            if (_selectedItemIndex > 0)
            {
                _selectedItemIndex -= 10;
                if (_selectedItemIndex < 0)
                {
                    _selectedItemIndex = 0;
                }

                SelectedImage = ImageSource.FromFile(_imageFiles[_selectedItemIndex]);
                OnPropertyChanged(nameof(SelectedItemIndex));
            }
        }

        public void GoToNextItem()
        {
            if (_selectedItemIndex < ImageFiles.Count - 1)
            {
                _selectedItemIndex++;
                SelectedImage = ImageSource.FromFile(_imageFiles[_selectedItemIndex]);
                OnPropertyChanged(nameof(SelectedItemIndex));
            }
        }

        public void GoToNextTenItems()
        {
            if (_selectedItemIndex < ImageFiles.Count - 1)
            {
                _selectedItemIndex += 10;
                if (_selectedItemIndex > ImageFiles.Count - 1)
                {
                    _selectedItemIndex = ImageFiles.Count - 1;
                }
                SelectedImage = ImageSource.FromFile(_imageFiles[_selectedItemIndex]);
                OnPropertyChanged(nameof(SelectedItemIndex));
            }
        }

        public async Task BlurImageAsync()
        {
            _showBlurredImage = !_showBlurredImage;
            try
            {
                if (_showBlurredImage)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        MemoryStream imageMemoryStream = await _imageProcessorService.GetBlurriedImageAsync(_imageFiles[_selectedItemIndex]);
                        imageMemoryStream.Seek(0, SeekOrigin.Begin);
                        _currentImageMemoryStream?.Dispose();
                        MemoryStream imageMemoryStreamCopy = new MemoryStream(imageMemoryStream.ToArray());
                        SelectedImage = ImageSource.FromStream(() => imageMemoryStreamCopy);
                        OnPropertyChanged(nameof(SelectedImage));
                        await imageMemoryStream.DisposeAsync();
                    });
                }
                else
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        SelectedImage = ImageSource.FromFile(_imageFiles[_selectedItemIndex]);
                    });
                }
            }
            catch (Exception exception)
            {
                _loggerService.LatestLogMessage = $"Something went wrong while loading blurred image! {exception.InnerException}";
            }
        }

        public void UpdateCurrentSelectedTags()
        {
            if (SelectedImage != null)
            {
                CurrentImageTags = _fileManipulatorService.GetTagsForImage(_imageFiles[_selectedItemIndex]);
            }
        }
    }
}