﻿using Dataset_Processor_Desktop.src.Utilities;

using SmartData.Lib.Interfaces;

namespace Dataset_Processor_Desktop.src.ViewModel
{
    public class TagEditorViewModel : BaseViewModel
    {
        private readonly IFileManipulatorService _fileManipulatorService;
        private readonly IImageProcessorService _imageProcessorService;
        private readonly IInputHooksService _inputHooksService;

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
                TotalImageFiles = _imageFiles.Count.ToString();
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
                if (_imageFiles?.Count > 0)
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
                    _loggerService.LatestLogMessage = $".txt or .caption file for current image not found, just type in the editor and one will be created!{Environment.NewLine}{exception.StackTrace}";
                    CurrentImageTags = string.Empty;
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

        private string _totalImageFiles;
        public string TotalImageFiles
        {
            get => _totalImageFiles;
            set
            {
                _totalImageFiles = $"Total files found: {value}";
                OnPropertyChanged(nameof(TotalImageFiles));
            }
        }

        private string _wordsToHighlight;
        public string WordsToHighlight
        {
            get => _wordsToHighlight;
            set
            {
                _wordsToHighlight = value;
                OnPropertyChanged(nameof(WordsToHighlight));
            }
        }

        private string _wordsToFilter;
        public string WordsToFilter
        {
            get => _wordsToFilter;
            set
            {
                _wordsToFilter = value;
                OnPropertyChanged(nameof(WordsToFilter));
            }
        }
        private bool _isExactFilter;
        public bool IsExactFilter
        {
            get => _isExactFilter;
            set
            {
                _isExactFilter = value;
                OnPropertyChanged(nameof(IsExactFilter));
            }
        }

        public string CurrentAndTotal
        {
            get => $"Current viewing: {SelectedItemIndex + 1}/{ImageFiles?.Count}.";
        }

        private bool _buttonEnabled;
        public bool ButtonEnabled
        {
            get => _buttonEnabled;
            set
            {
                _buttonEnabled = value;
                OnPropertyChanged(nameof(ButtonEnabled));
            }
        }

        public RelayCommand PreviousItemCommand { get; private set; }
        public RelayCommand PreviousTenItemsCommand { get; private set; }
        public RelayCommand PreviousOneHundredItemsCommand { get; private set; }
        public RelayCommand NextItemCommand { get; private set; }
        public RelayCommand NextTenItemsCommand { get; private set; }
        public RelayCommand NextOneHundredItemsCommand { get; private set; }
        public RelayCommand SelectInputFolderCommand { get; private set; }
        public RelayCommand BlurImageCommand { get; private set; }
        public RelayCommand OpenInputFolderCommand { get; private set; }
        public RelayCommand SwitchEditorTypeCommand { get; private set; }
        public RelayCommand FilterFilesCommand { get; private set; }
        public RelayCommand ClearFilterCommand { get; private set; }

        private bool _showBlurredImage;

        private MemoryStream _currentImageMemoryStream = null;

        private bool _editingTxt;

        public string CurrentType
        {
            get
            {
                if (_editingTxt)
                {
                    return ".txt";
                }
                else
                {
                    return ".caption";
                }
            }
        }

        private string _currentImageTags;
        public string CurrentImageTags
        {
            get => _currentImageTags;
            set
            {
                _currentImageTags = value;
                OnPropertyChanged(nameof(CurrentImageTags));
                OnPropertyChanged(nameof(CurrentAndTotal));
                string txtFile = Path.ChangeExtension(_imageFiles[_selectedItemIndex], CurrentType);
                _fileManipulatorService.SaveTextForImage(txtFile, _currentImageTags);
            }
        }

        public TagEditorViewModel(IFileManipulatorService fileManipulatorService, IImageProcessorService imageProcessorService, IInputHooksService inputHooksService)
        {
            _fileManipulatorService = fileManipulatorService;
            _imageProcessorService = imageProcessorService;
            _inputHooksService = inputHooksService;

            InputFolderPath = _configsService.Configurations.CombinedOutputFolder;
            _fileManipulatorService.CreateFolderIfNotExist(InputFolderPath);

            PreviousItemCommand = new RelayCommand(GoToPreviousItem);
            PreviousTenItemsCommand = new RelayCommand(GoToPreviousTenItems);
            PreviousOneHundredItemsCommand = new RelayCommand(GoToPreviousOneHundredItems);
            NextItemCommand = new RelayCommand(GoToNextItem);
            NextTenItemsCommand = new RelayCommand(GoToNextTenItems);
            NextOneHundredItemsCommand = new RelayCommand(GoToNextOneHundredItems);
            SelectInputFolderCommand = new RelayCommand(async () => await SelectInputFolderAsync());
            BlurImageCommand = new RelayCommand(async () => await BlurImageAsync());
            OpenInputFolderCommand = new RelayCommand(async () => await OpenFolderAsync(InputFolderPath));
            SwitchEditorTypeCommand = new RelayCommand(SwitchEditorType);
            FilterFilesCommand = new RelayCommand(async () => await FilterFilesAsync());
            ClearFilterCommand = new RelayCommand(ClearFilter);
            ButtonEnabled = true;
            IsExactFilter = false;

            _editingTxt = true;

            SelectedItemIndex = 0;

            _inputHooksService.ButtonF1 += OnF1ButtonDown;
            _inputHooksService.ButtonF2 += OnF2ButtonDown;
            _inputHooksService.ButtonF3 += OnF3ButtonDown;
            _inputHooksService.ButtonF4 += OnF4ButtonDown;
            _inputHooksService.ButtonF5 += OnF5ButtonDown;
            _inputHooksService.ButtonF6 += OnF6ButtonDown;
            _inputHooksService.ButtonF8 += OnF8ButtonDown;

            _inputHooksService.MouseButton3 += OnMouseButton3Down;
            _inputHooksService.MouseButton4 += OnMouseButton4Down;
            _inputHooksService.MouseButton5 += OnMouseButton5Down;
        }

        public async Task SelectInputFolderAsync()
        {
            string result = await _folderPickerService.PickFolderAsync();
            if (!string.IsNullOrEmpty(result))
            {
                InputFolderPath = result;
                LoadImagesFromInputFolder();
            }
        }

        public async Task FilterFilesAsync()
        {
            try
            {
                ButtonEnabled = false;
                List<string> searchResult = await Task.Run(() => _fileManipulatorService.GetFilteredImageFiles(InputFolderPath, CurrentType, WordsToFilter, IsExactFilter));
                if (searchResult.Count > 0)
                {
                    SelectedItemIndex = 0;
                    ImageFiles = searchResult;
                    ImageFiles = ImageFiles.OrderBy(x => int.Parse(Path.GetFileNameWithoutExtension(x))).ToList();
                }
                else
                {
                    _loggerService.LatestLogMessage = "No images found!";
                }
            }
            catch (Exception exception)
            {
                if (exception.GetType() == typeof(FileNotFoundException))
                {
                    _loggerService.LatestLogMessage = "No image files were found in the directory.";
                }
            }
            finally
            {
                if (ImageFiles.Count != 0)
                {
                    SelectedImage = ImageSource.FromFile(_imageFiles[_selectedItemIndex]);
                }
                ButtonEnabled = true;
            }
        }

        public void ClearFilter()
        {
            if (!string.IsNullOrEmpty(InputFolderPath))
            {
                LoadImagesFromInputFolder();
            }
        }

        public void SwitchEditorType()
        {
            _editingTxt = !_editingTxt;
            OnPropertyChanged(nameof(CurrentType));
            UpdateCurrentSelectedTags();
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

        public void GoToPreviousOneHundredItems()
        {
            if (_selectedItemIndex > 0)
            {
                _selectedItemIndex -= 100;
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

        public void GoToNextOneHundredItems()
        {
            if (_selectedItemIndex < ImageFiles.Count - 1)
            {
                _selectedItemIndex += 100;
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
                        MemoryStream imageMemoryStream = await _imageProcessorService.GetBlurredImageAsync(_imageFiles[_selectedItemIndex]);
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
            catch
            {
                _loggerService.LatestLogMessage = $"Something went wrong while loading blurred image! Error log will be saved inside the logs folder.";
            }
        }

        public void UpdateCurrentSelectedTags()
        {
            if (SelectedImage != null)
            {
                try
                {
                    CurrentImageTags = _fileManipulatorService.GetTextFromFile(_imageFiles[_selectedItemIndex], CurrentType);
                }
                catch
                {
                    _loggerService.LatestLogMessage = $".txt or .caption file for current image not found, just type in the editor and one will be created!";
                    CurrentImageTags = string.Empty;
                }
            }
        }

        private void LoadImagesFromInputFolder()
        {
            try
            {
                ImageFiles = _fileManipulatorService.GetImageFiles(InputFolderPath);
                if (ImageFiles.Count != 0)
                {
                    ImageFiles = ImageFiles.OrderBy(x => int.Parse(Path.GetFileNameWithoutExtension(x))).ToList();
                    SelectedItemIndex = 0;
                    OnPropertyChanged(nameof(CurrentAndTotal));
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

        private void OnF1ButtonDown(object sender, EventArgs e)
        {
            if (IsActive)
            {
                MainThread.BeginInvokeOnMainThread(GoToPreviousItem);
            }
        }

        private void OnF2ButtonDown(object sender, EventArgs e)
        {
            if (IsActive)
            {
                MainThread.BeginInvokeOnMainThread(GoToNextItem);
            }
        }

        private void OnF3ButtonDown(object sender, EventArgs e)
        {
            if (IsActive)
            {
                MainThread.BeginInvokeOnMainThread(GoToPreviousTenItems);
            }
        }

        private void OnF4ButtonDown(object sender, EventArgs e)
        {
            if (IsActive)
            {
                MainThread.BeginInvokeOnMainThread(GoToNextTenItems);
            }
        }

        private void OnF5ButtonDown(object sender, EventArgs e)
        {
            if (IsActive)
            {
                MainThread.BeginInvokeOnMainThread(GoToPreviousOneHundredItems);
            }
        }

        private void OnF6ButtonDown(object sender, EventArgs e)
        {
            if (IsActive)
            {
                MainThread.BeginInvokeOnMainThread(GoToNextOneHundredItems);
            }
        }

        private void OnF8ButtonDown(object sender, EventArgs e)
        {
            if (IsActive)
            {
                Task.Run(BlurImageAsync);
            }
        }

        private void OnMouseButton3Down(object sender, EventArgs e)
        {
            if (IsActive)
            {
                Task.Run(BlurImageAsync);
            }
        }

        private void OnMouseButton4Down(object sender, EventArgs e)
        {
            if (IsActive)
            {
                MainThread.BeginInvokeOnMainThread(GoToPreviousItem);
            }
        }

        private void OnMouseButton5Down(object sender, EventArgs e)
        {
            if (IsActive)
            {
                MainThread.BeginInvokeOnMainThread(GoToNextItem);
            }
        }
    }
}