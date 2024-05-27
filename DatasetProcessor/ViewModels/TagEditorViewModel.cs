﻿using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using SmartData.Lib.Enums;
using SmartData.Lib.Interfaces;
using SmartData.Lib.Interfaces.MachineLearning;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DatasetProcessor.ViewModels
{
    /// <summary>
    /// View model for the Tag Editor, responsible for managing image tags and text editing.
    /// </summary>
    public partial class TagEditorViewModel : BaseViewModel
    {
        private readonly IFileManipulatorService _fileManipulator;
        private readonly IImageProcessorService _imageProcessor;
        private readonly ICLIPTokenizerService _clipTokenizer;
        private Random _random;

        [ObservableProperty]
        private string _inputFolderPath;
        [ObservableProperty]
        private List<string> _imageFiles;
        [ObservableProperty]
        private string _totalImageFiles;
        [ObservableProperty]
        private int _selectedItemIndex;
        [ObservableProperty]
        private Bitmap _selectedImage;
        [ObservableProperty]
        private string _selectedImageFilename;
        [ObservableProperty]
        private string _wordsToHighlight;
        [ObservableProperty]
        private string _wordsToFilter;
        [ObservableProperty]
        private bool _isExactFilter;
        [ObservableProperty]
        private bool _buttonEnabled;
        [ObservableProperty]
        private string _currentAndTotal;
        [ObservableProperty]
        private bool _editingTxt;
        [ObservableProperty]
        private string _currentImageTags;
        [ObservableProperty]
        private string _currentImageTokenCount;

        [ObservableProperty]
        private SolidColorBrush _tokenTextColor;

        private bool _showBlurredImage;
        private MemoryStream _currentImageMemoryStream = null;

        /// <summary>
        /// Gets the current type of file being edited, either .txt or .caption.
        /// </summary>
        public string CurrentType
        {
            get
            {
                if (EditingTxt)
                {
                    return ".txt";
                }
                else
                {
                    return ".caption";
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the TagEditorViewModel class.
        /// </summary>
        /// <param name="fileManipulator">The file manipulation service for file operations.</param>
        /// <param name="imageProcessor">The image processing service for image-related operations.</param>
        /// <param name="inputHooks">The input hooks service for managing user input.</param>
        /// <param name="clipTokenizer">The clip tokenizer service for token operations.</param>
        /// <param name="logger">The logger service for logging messages.</param>
        /// <param name="configs">The configuration service for application settings.</param>
        public TagEditorViewModel(IFileManipulatorService fileManipulator, IImageProcessorService imageProcessor,
            ICLIPTokenizerService clipTokenizer, ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
            _fileManipulator = fileManipulator;
            _imageProcessor = imageProcessor;
            _clipTokenizer = clipTokenizer;
            _random = new Random();
            _configs = configs;

            InputFolderPath = _configs.Configurations.TagEditorConfigs.InputFolder;
            _fileManipulator.CreateFolderIfNotExist(InputFolderPath);
            IsExactFilter = _configs.Configurations.TagEditorConfigs.ExactMatchesFiltering;
            ButtonEnabled = true;
            EditingTxt = true;

            SelectedItemIndex = 0;
            CurrentImageTokenCount = string.Empty;

            TokenTextColor = new SolidColorBrush(Colors.LightGreen);
        }

        /// <summary>
        /// Updates the current selected image tags based on the selected image.
        /// </summary>
        public void UpdateCurrentSelectedTags()
        {
            if (SelectedImage != null)
            {
                try
                {
                    CurrentImageTags = _fileManipulator.GetTextFromFile(ImageFiles[SelectedItemIndex], CurrentType);
                }
                catch
                {
                    Logger.SetLatestLogMessage($".txt or .caption file for current image not found, just type in the editor and one will be created!",
                        LogMessageColor.Warning);
                    CurrentImageTags = string.Empty;
                }
            }
        }

        /// <summary>
        /// Navigates to a specific item in the image list.
        /// </summary>
        /// <param name="parameter">The navigation parameter indicating the item index.</param>
        [RelayCommand]
        private void GoToItem(string parameter)
        {
            try
            {
                int.TryParse(parameter, out int parameterInt);

                if (ImageFiles?.Count != 0)
                {
                    SelectedItemIndex += parameterInt;
                }
            }
            catch
            {
                Logger.SetLatestLogMessage("Couldn't load the image.", LogMessageColor.Error);
            }
        }

        /// <summary>
        /// Navigates to a random item in the image list.
        /// </summary>
        [RelayCommand]
        private void GoToRandomItem()
        {
            if (ImageFiles?.Count != 0 && ImageFiles != null)
            {
                SelectedItemIndex = _random.Next(0, ImageFiles.Count);
            }
        }

        /// <summary>
        /// Switches between editing .txt and .caption files and updates the view accordingly.
        /// </summary>
        [RelayCommand]
        private void SwitchEditorType()
        {
            EditingTxt = !EditingTxt;
            OnPropertyChanged(nameof(CurrentType));
        }

        /// <summary>
        /// Toggles the display of a blurred image for the currently selected image asynchronously.
        /// </summary>
        [RelayCommand]
        public async Task BlurImageAsync()
        {
            _showBlurredImage = !_showBlurredImage;
            try
            {
                if (_showBlurredImage)
                {
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        MemoryStream imageMemoryStream = await _imageProcessor.GetBlurredImageAsync(ImageFiles[SelectedItemIndex]);
                        imageMemoryStream.Seek(0, SeekOrigin.Begin);
                        _currentImageMemoryStream?.Dispose();
                        MemoryStream imageMemoryStreamCopy = new MemoryStream(imageMemoryStream.ToArray());
                        SelectedImage = new Bitmap(imageMemoryStream);
                        await imageMemoryStream.DisposeAsync();
                    });
                }
                else
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        SelectedImage = SelectBitmapInterpolation();
                    });
                }
            }
            catch
            {
                Logger.SetLatestLogMessage($"Something went wrong while loading blurred image!", LogMessageColor.Warning);
            }
        }

        /// <summary>
        /// Asynchronously filters and loads image files based on specified filter criteria.
        /// </summary>
        [RelayCommand]
        private async Task FilterFilesAsync()
        {
            try
            {
                ButtonEnabled = false;
                List<string> searchResult = await Task.Run(() => _fileManipulator.GetFilteredImageFiles(InputFolderPath, CurrentType, WordsToFilter, IsExactFilter));
                if (searchResult.Count > 0)
                {
                    SelectedItemIndex = 0;
                    ImageFiles = searchResult;
                    ImageFiles = ImageFiles.OrderBy(x => int.Parse(Path.GetFileNameWithoutExtension(x))).ToList();
                }
                else
                {
                    Logger.SetLatestLogMessage("No images found!", LogMessageColor.Warning);
                }
            }
            catch (FileNotFoundException)
            {
                Logger.SetLatestLogMessage("No image files were found in the directory.", LogMessageColor.Error);
            }
            catch (Exception exception)
            {
                Logger.SetLatestLogMessage($"Something went wrong! Error log will be saved inside the logs folder.",
                        LogMessageColor.Error);
                await Logger.SaveExceptionStackTrace(exception);
            }
            finally
            {
                if (ImageFiles.Count != 0)
                {
                    SelectedImage = SelectBitmapInterpolation();
                }
                ButtonEnabled = true;
            }
        }

        /// <summary>
        /// Clears the applied filter and reloads all images from the original input folder.
        /// </summary>
        [RelayCommand]
        private void ClearFilter()
        {
            if (!string.IsNullOrEmpty(InputFolderPath))
            {
                LoadImagesFromInputFolder();
            }
        }

        /// <summary>
        /// Selects an input folder and loads images from it.
        /// </summary>
        [RelayCommand]
        private async Task SelectInputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                InputFolderPath = result;
                LoadImagesFromInputFolder();
            }
        }

        /// <summary>
        /// Copies the current image tags to the clipboard asynchronously.
        /// </summary>
        [RelayCommand]
        private async Task CopyCurrentImageTagsToClipboard()
        {
            await CopyToClipboard(CurrentImageTags);
        }

        /// <summary>
        /// Counts the tokens for the current image tags asynchronously.
        /// Downloads the necessary onnx extension file if it is not present, and updates the token count and text color.
        /// </summary>
        /// <returns>A Task representing the asynchronous operation.</returns>
        [RelayCommand]
        private async Task CountTokensForCurrentImage()
        {
            if (_fileManipulator.FileNeedsToBeDownloaded(AvailableModels.CLIPTokenizer))
            {
                await _fileManipulator.DownloadModelFile(AvailableModels.CLIPTokenizer);
            }

            int count = await Task.Run(() => _clipTokenizer.CountTokens(CurrentImageTags));
            CurrentImageTokenCount = $"Token count: {count}/75";
            if (count < 75)
            {
                TokenTextColor = new SolidColorBrush(Colors.LightGreen);
            }
            else
            {
                TokenTextColor = new SolidColorBrush(Colors.PaleVioletRed);
            }
        }

        /// <summary>
        /// Loads image files from the specified input folder and prepares the view model for editing.
        /// </summary>
        private void LoadImagesFromInputFolder()
        {
            try
            {
                ImageFiles = _fileManipulator.GetImageFiles(InputFolderPath);
                if (ImageFiles.Count != 0)
                {
                    ImageFiles = ImageFiles.OrderBy(x => int.Parse(Path.GetFileNameWithoutExtension(x))).ToList();
                    SelectedItemIndex = 0;
                }
            }
            catch
            {
                Logger.SetLatestLogMessage("No image files were found in the directory.", LogMessageColor.Error);
            }
            finally
            {
                if (ImageFiles.Count != 0)
                {
                    SelectedImage = SelectBitmapInterpolation();
                }
            }
        }

        /// <summary>
        /// Handles changes in the EditingTxt property to update the selected image tags.
        /// </summary>
        partial void OnEditingTxtChanged(bool value)
        {
            UpdateCurrentSelectedTags();
        }

        /// <summary>
        /// Handles changes in the ImageFiles property to update the total image files count.
        /// </summary>
        partial void OnImageFilesChanged(List<string> value)
        {
            TotalImageFiles = $"Total files found: {ImageFiles.Count.ToString()}.";
        }

        /// <summary>
        /// Handles changes in the SelectedItemIndex property to ensure it stays within the valid range.
        /// </summary>
        partial void OnSelectedItemIndexChanged(int value)
        {
            if (ImageFiles?.Count > 0)
            {
                SelectedItemIndex = Math.Clamp(value, 0, ImageFiles.Count - 1);
                SelectedImage = SelectBitmapInterpolation();
            }
            else
            {
                SelectedItemIndex = 0;
            }
        }

        /// <summary>
        /// Handles changes in the SelectedImage property to update the current selected image tags.
        /// </summary>
        partial void OnSelectedImageChanged(Bitmap value)
        {
            try
            {
                UpdateCurrentSelectedTags();
            }
            catch (Exception exception)
            {
                Logger.SetLatestLogMessage($".txt or .caption file for current image not found, just type in the editor and one will be created!{Environment.NewLine}{exception.StackTrace}",
                    LogMessageColor.Warning);
                CurrentImageTags = string.Empty;
            }
            finally
            {
                CurrentAndTotal = $"Currently viewing: {SelectedItemIndex + 1}/{ImageFiles?.Count}.";
                SelectedImageFilename = $"Current file: {Path.GetFileName(ImageFiles[SelectedItemIndex])}.";
            }
        }

        /// <summary>
        /// Handles changes in the CurrentImageTags property to save the updated tags to the selected image's file.
        /// </summary>
        partial void OnCurrentImageTagsChanged(string value)
        {
            try
            {
                string txtFile = Path.ChangeExtension(ImageFiles[SelectedItemIndex], CurrentType);
                _fileManipulator.SaveTextToFile(txtFile, CurrentImageTags);
            }
            catch (NullReferenceException)
            {
                Logger.SetLatestLogMessage("You need to select a folder with image files!", LogMessageColor.Warning);
            }
        }

        /// <summary>
        /// Selects a bitmap with optional interpolation based on the size of the image.
        /// </summary>
        /// <returns>The selected bitmap.</returns>
        private Bitmap SelectBitmapInterpolation()
        {
            Bitmap imageBitmap = new Bitmap((ImageFiles[SelectedItemIndex]));
            if (imageBitmap.PixelSize.Width < 256 || imageBitmap.PixelSize.Height < 256)
            {
                double aspectRatio = (double)imageBitmap.PixelSize.Width / imageBitmap.PixelSize.Height;
                int targetWidth = 512;
                int targetHeight = 512;

                if (aspectRatio > 1)
                {
                    targetHeight = (int)(targetWidth / aspectRatio);
                }
                else
                {
                    targetWidth = (int)(targetHeight * aspectRatio);
                }

                imageBitmap = imageBitmap.CreateScaledBitmap(new PixelSize(targetWidth, targetHeight), BitmapInterpolationMode.None);
            }

            return imageBitmap;
        }
    }
}
