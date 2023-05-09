﻿using SmartData.Lib.Helpers;

namespace SmartData.Lib.Interfaces
{
    public interface IAutoTaggerService
    {
        public bool IsModelLoaded { get; }
        public string ModelPath { get; set; }
        public string TagsPath { get; set; }
        public float Threshold { get; set; }

        public Task GenerateTags(string inputPath, string outputPath, bool weightedCaptions = false);
        public Task GenerateTags(string inputPath, string outputPath, Progress progess, bool weightedCaptions = false);
    }
}
