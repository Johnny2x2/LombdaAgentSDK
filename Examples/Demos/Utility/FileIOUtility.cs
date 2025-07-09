using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Demos
{
    public class FileIOUtility
    {
        public static string SafeWorkingDirectory { get; set; } = string.Empty;

        public static string ReadFile(string filePath)
        {
            if(string.IsNullOrEmpty(SafeWorkingDirectory))
            {
                throw new InvalidOperationException("SafeWorkingDirectory is not set. Please set SafeWorkingDirectory before reading directories.");
            }
            
            string FixedPath = Path.Combine(SafeWorkingDirectory, filePath);

            return File.ReadAllText(FixedPath);
        }

        public static void WriteFile(string filePath, string content) {

            if (string.IsNullOrEmpty(SafeWorkingDirectory))
            {
                throw new InvalidOperationException("SafeWorkingDirectory is not set. Please set SafeWorkingDirectory before reading directories.");
            }

            string FixedPath = Path.Combine(SafeWorkingDirectory, filePath);

            string? directoryPath = Path.GetDirectoryName(FixedPath);

            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllText(FixedPath, content);
        }

        public static string GetAllPaths(string directory)
        {
            if (string.IsNullOrEmpty(SafeWorkingDirectory))
            {
                throw new InvalidOperationException("SafeWorkingDirectory is not set. Please set SafeWorkingDirectory before reading directories.");
            }

            List<string> allPaths = new List<string>();

            GetPaths(directory, allPaths);

            for (int i = 0; i<allPaths.Count; i++)
            {
                allPaths[i] = allPaths[i].Replace(SafeWorkingDirectory, "");
            }

            foreach (string path in allPaths)
            {
                Console.WriteLine(path);
            }

            return string.Join(Environment.NewLine, allPaths);
        }

        public static void GetPaths(string directory, List<string> paths)
        {
            if (string.IsNullOrEmpty(SafeWorkingDirectory))
            {
                throw new InvalidOperationException("SafeWorkingDirectory is not set. Please set SafeWorkingDirectory before reading directories.");
            }

            string SafePath = Path.Combine(SafeWorkingDirectory, directory);

            try
            {
                // Add files in the current directory
                paths.AddRange(Directory.GetFiles(SafePath));

                // Add subdirectories in the current directory and recurse
                string[] subdirectories = Directory.GetDirectories(SafePath);
                //paths.AddRange(subdirectories);
                foreach (string subdirectory in subdirectories)
                {
                    if (subdirectory.Contains("bin")) continue;
                    if (subdirectory.Contains("obj")) continue;
                    GetPaths(subdirectory, paths);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Access denied to: {ex.Message}"); // Handle exceptions gracefully
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.WriteLine($"Directory not found: {ex.Message}"); // Handle exceptions gracefully
            }
        }
    }
}
