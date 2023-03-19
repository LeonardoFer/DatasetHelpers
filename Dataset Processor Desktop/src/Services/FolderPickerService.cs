﻿using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Storage;

using Dataset_Processor_Desktop.src.Interfaces;

namespace Dataset_Processor_Desktop.src.Services
{
    public class FolderPickerService : IFolderPickerService
    {
        private CancellationToken _cancelToken;

        public CancellationToken CancelToken
        {
            get => _cancelToken;
            set { _cancelToken = value; }
        }

        public FolderPickerService()
        {
            _cancelToken = new CancellationToken();
        }

        public async Task<string> PickFolderAsync()
        {
            if (DeviceInfo.Platform != DevicePlatform.WinUI)
            {
                throw new PlatformNotSupportedException("This platform is not supported!");
            }

            FolderPickerResult result = await FolderPicker.Default.PickAsync(_cancelToken);
            if (!result.IsSuccessful)
            {
                await Toast.Make($"No folder was selected!", ToastDuration.Long).Show(_cancelToken);
                return "";
            }

            return result.Folder.Path;
        }
    }
}