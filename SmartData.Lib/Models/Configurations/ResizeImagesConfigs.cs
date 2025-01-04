﻿using SmartData.Lib.Enums;

using System.Text.Json.Serialization;

namespace Models.Configurations
{
    public class ResizeImagesConfigs
    {
        [JsonPropertyName("inputFolder")]
        public string InputFolder { get; set; } = string.Empty;

        [JsonPropertyName("outputFolder")]
        public string OutputFolder { get; set; } = string.Empty;

        [JsonPropertyName("outputDimensionSize")]
        public SupportedDimensions OutputDimensionSize { get; set; } = SupportedDimensions.Resolution1024x1024;

        private int _lanczosRadius = 3;
        [JsonPropertyName("lanczosRadius")]
        public int LanczosRadius
        {
            get => _lanczosRadius;
            set
            {
                _lanczosRadius = Math.Clamp(value, 1, 25);
            }
        }

        [JsonPropertyName("applySharpenSigma")]
        public bool ApplySharpenSigma { get; set; } = true;

        private float _sharpenSigma = 0.7f;
        [JsonPropertyName("sharpenSigma")]
        public float SharpenSigma
        {
            get => _sharpenSigma;
            set
            {
                _sharpenSigma = Math.Clamp(value, 0.5f, 5.0f);
            }
        }

        private int _minimumResolutionForSharpen = 256;
        [JsonPropertyName("minimumResolutionForSharpen")]
        public int MinimumResolutionForSharpen
        {
            get => _minimumResolutionForSharpen;
            set
            {
                _minimumResolutionForSharpen = Math.Clamp(value, 256, ushort.MaxValue);
            }
        }
    }
}
