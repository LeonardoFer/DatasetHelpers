﻿using SmartData.Lib.Enums;

namespace SmartData.Lib.Helpers
{
    public static class Utilities
    {
        private const int _regexTimeoutSeconds = 15;

        /// <summary>
        /// Gets an array of file paths in the specified directory that match any of the provided extensions,
        /// excluding files with the name "sample_prompt_custom.txt."
        /// </summary>
        /// <param name="folderPath">The path of the directory to search.</param>
        /// <param name="searchPattern">A comma-separated list of file extensions to match, e.g., ".txt,.docx,.png".</param>
        /// <returns>An array of strings representing the file paths that match the provided extensions.</returns>
        public static string[] GetFilesByMultipleExtensions(string folderPath, string searchPattern)
        {
            IEnumerable<string> result = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(extension => searchPattern.Contains(Path.GetExtension(extension).ToLower()));

            return result.Where(x => !x.Contains("sample_prompt_custom.txt")).ToArray();
        }

        /// <summary>
        /// Parses a string containing tags, removes extra spaces and formatting,
        /// and splits it into an array of individual tags.
        /// </summary>
        /// <param name="tags">The string containing tags to parse and clean.</param>
        /// <returns>An array of individual tags obtained from the input string.</returns>
        public static string[] ParseAndCleanTags(string tags)
        {
            return tags.Replace(", ", ",").Replace("  ", " ").Split(",");
        }

        /// <summary>
        /// Gets the timeout duration for regular expressions in seconds.
        /// </summary>
        /// <remarks>
        /// The time out is in 10 seconds.
        /// </remarks>
        public static TimeSpan RegexTimeout => TimeSpan.FromSeconds(_regexTimeoutSeconds);

        /// <summary>
        /// Gets an array of supported dimensions.
        /// </summary>
        public static SupportedDimensions[] Values => (SupportedDimensions[])Enum.GetValues(typeof(SupportedDimensions));
    }
}
