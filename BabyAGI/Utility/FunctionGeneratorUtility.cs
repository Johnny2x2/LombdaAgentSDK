using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace BabyAGI.Utility
{
    public class BuildOutputResult
    {
        public string Output { get; set; }
        public string Error { get; set; }
        public bool BuildCompleted { get; set; }

        public BuildOutputResult() { }

        public BuildOutputResult(string output, string error, bool completed)
        {
            Output = output;
            Error = error;
            BuildCompleted = completed;
        }
    }
    public class ExecutableOutputResult
    {
        private bool executionCompleted = false;

        public string Output { get; set; }
        public string Error { get; set; }
        public bool ExecutionCompleted { get => executionCompleted; set => executionCompleted = value; }

        public ExecutableOutputResult() { }

        public ExecutableOutputResult(string output, string error, bool completed)
        {
            Output = output;
            Error = error;
            ExecutionCompleted = completed;
        }
    }
    public class CodeBuildInfo
    {

        public string BuildPath { get; set; }

        public string ProjectName { get; set; }
        public ExecutableOutputResult? ExecutableResult { get; set; }

        public BuildOutputResult? BuildResult { get; set; }

        public CodeBuildInfo() { }

        public CodeBuildInfo(string buildPath, string projectName)
        {
            BuildPath = buildPath;
            ProjectName = projectName;
        }
    }
    public static class FunctionGeneratorUtility
    {
        public static string? functionsDirectory { get; set; } = "C:\\Users\\johnl\\source\\repos\\FunctionApplications";


        [Test]
        public static void TestCreateProject()
        {
            CreateNewProject(functionsDirectory, "Test", "Test Project");
        }

        [Test]
        public static void TestBuildProject()
        {
            var result = BuildAndRunProject(Path.Combine(functionsDirectory, "Test"), "FunctionApplication", "net8.0", "hi", true);
        }

        [Test]
        public static void TestGetProjectFiles()
        {
            Console.WriteLine(GetProjectFiles(functionsDirectory, "Test"));
        }

        [Test]
        public static void TestWriteToProject()
        {
            WriteToProject(functionsDirectory, "Test", "Program.cs", "Console.WriteLine(args[1]);");
            var result = BuildAndRunProject(Path.Combine(functionsDirectory, "Test"), "FunctionApplication", "net8.0", "hi by", true);
        }

        [Test]
        public static void TestGetDescription()
        {
            Console.WriteLine(GetProjectDescription(functionsDirectory, "Test"));
        }

        [Test]
        public static void TestReadFile()
        {
            Console.WriteLine(ReadProjectFile(functionsDirectory, "Test","Program.cs"));
        }

        [Test]
        public static void TestGetFunctions()
        {
            Console.WriteLine(string.Join("\n",GetFunctions(functionsDirectory)));
        }

        public static string[] GetFunctions(string FunctionsDirectory)
        {
            if (string.IsNullOrEmpty(FunctionsDirectory))
            {
                throw new InvalidOperationException("FunctionsDirectory is not set. Please set FunctionsDirectory before reading directories.");
            }
            string[] dirs = Directory.GetDirectories(FunctionsDirectory);
            for (int i = 0; i < dirs.Length; i++)
            {
                dirs[i] = dirs[i].Replace(FunctionsDirectory, "").TrimStart('\\');
            }
            return dirs;
        }

        public static bool CreateNewProject(string FunctionsDirectory, string projectName, string projectDescription)
        {
            if (string.IsNullOrEmpty(FunctionsDirectory))
            {
                throw new InvalidOperationException("FunctionsDirectory is not set. Please set FunctionsDirectory before reading directories.");
            }

            string zipPath = Path.Combine(FunctionsDirectory, "FunctionApplicationTemplate.zip");
            string extractPath = Path.Combine(FunctionsDirectory, projectName+"_TEMP");
            string finalPath = Path.Combine(FunctionsDirectory, projectName);

            if (Directory.Exists(extractPath) || Directory.Exists(finalPath))
            {
                return false;
            }

            ZipFile.ExtractToDirectory(zipPath, extractPath);
            Directory.Move(Path.Combine(extractPath,"FunctionApplication"), finalPath);
            Directory.Delete(extractPath, true);

            WriteProjectDescription(FunctionsDirectory, projectName, projectDescription);

            return true;
        }
        public static string GetProjectDescription(string FunctionsDirectory,  string projectName)
        {
            try
            {
                if (string.IsNullOrEmpty(FunctionsDirectory))
                {
                    throw new InvalidOperationException("FunctionsDirectory is not set. Please set FunctionsDirectory before reading directories.");
                }

                string fixedPath = Path.Combine(FunctionsDirectory, projectName, "Description.txt");

                return File.ReadAllText(fixedPath);
            }

            catch (Exception ex)
            {
                return $"Error reading file -> {ex.Message}"; // Return empty string or handle as needed
            }
        }
        public static string ReadProjectFile(string FunctionsDirectory, string projectName, string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(FunctionsDirectory))
                {
                    throw new InvalidOperationException("FunctionsDirectory is not set. Please set FunctionsDirectory before reading directories.");
                }

                filePath = filePath.Trim();

                Path.GetInvalidFileNameChars().ToList().ForEach(c =>
                {
                    if (Path.GetFileName(filePath).Contains(c))
                    {
                        throw new ArgumentException($"File name cannot contain {c}");
                    }
                }
                );
                // Validate the file path
                if (string.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty.");
                }
                if (filePath.StartsWith("..") || filePath.StartsWith("/"))
                {
                    throw new ArgumentException("File path cannot contain relative paths like '..', or '/'.");
                }
                if (filePath.StartsWith("\\"))
                {
                    filePath = filePath.TrimStart('\\');
                }

                string fixedPath = Path.Combine(FunctionsDirectory, projectName, "FunctionApplication", filePath.Trim());

                return File.ReadAllText(fixedPath);
            }

            catch (Exception ex)
            {
                return $"Error reading file -> {ex.Message}"; // Return empty string or handle as needed
            }
        }

        public static bool WriteToProject(string FunctionsDirectory, string projectName, string filePath, string content)
        {
            if (string.IsNullOrEmpty(FunctionsDirectory))
            {
                throw new InvalidOperationException("FunctionsDirectory is not set. Please set FunctionsDirectory before reading directories.");
            }

            string projectPath = Path.Combine(FunctionsDirectory, projectName,"FunctionApplication");

            string directoryPath = Path.GetDirectoryName(Path.Combine(projectPath,filePath));

            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllText(Path.Combine(directoryPath, filePath), content);

            return true;
        }

        public static void WriteProjectDescription(string FunctionsDirectory, string projectName,  string content)
        {
            if (string.IsNullOrEmpty(FunctionsDirectory))
            {
                throw new InvalidOperationException("FunctionsDirectory is not set. Please set FunctionsDirectory before reading directories.");
            }

            string descriptionPath = Path.Combine(FunctionsDirectory, projectName,"Description.txt");

            File.WriteAllText(descriptionPath, content);
        }

        public static void WriteProjectArgs(string FunctionsDirectory, string projectName, string content)
        {
            if (string.IsNullOrEmpty(FunctionsDirectory))
            {
                throw new InvalidOperationException("FunctionsDirectory is not set. Please set FunctionsDirectory before reading directories.");
            }

            string descriptionPath = Path.Combine(FunctionsDirectory, projectName, "ExampleArgs.txt");

            File.WriteAllText(descriptionPath, content);
        }

        public static string GetProjectFiles(string FunctionsDirectory, string projectName)
        {
            if (string.IsNullOrEmpty(FunctionsDirectory))
            {
                throw new InvalidOperationException("FunctionsDirectory is not set. Please set FunctionsDirectory before reading directories.");
            }

            string projectPath = Path.Combine(FunctionsDirectory, projectName, "FunctionApplication");

            return GetAllPaths(projectPath);
        }

        public static string GetAllPaths(string directory)
        {
            List<string> allPaths = new List<string>();

            GetPaths(directory, allPaths);

            for (int i = 0; i < allPaths.Count; i++)
            {
                allPaths[i] = allPaths[i].Replace(directory, "");
            }

            return string.Join(Environment.NewLine, allPaths);
        }

        public static void GetPaths(string directory, List<string> paths)
        {
            try
            {
                // Add files in the current directory
                paths.AddRange(Directory.GetFiles(directory));

                // Add subdirectories in the current directory and recurse
                string[] subdirectories = Directory.GetDirectories(directory);
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

        public static CodeBuildInfo BuildAndRunProject(string pathToFunctions, string projectToRun, string framework = "", string args = "", bool runProject = false)
        {
            CodeBuildInfo codeBuildInfo = new CodeBuildInfo(pathToFunctions, projectToRun);
            // Path to the target project's directory

            // ... (Code for programmatically building the project as shown in the previous example) ...

            // After the build is successful:
            codeBuildInfo.BuildResult = BuildProject(Path.Combine(pathToFunctions, projectToRun));

            if (codeBuildInfo.BuildResult.BuildCompleted)
            {
                Console.WriteLine("Build successful! Now running the built project...");
                // Find the executable file
                string executablePath = FindExecutable(pathToFunctions, projectToRun, framework);

                if (!string.IsNullOrEmpty(executablePath))
                {
                    if (runProject)
                    {
                        // Run the executable and capture its output
                        codeBuildInfo.ExecutableResult = RunExecutableAndCaptureOutput(executablePath, args);
                    }
                }
                else
                {
                    Console.WriteLine("Could not find the executable file.");
                }
            }
            else
            {
                Console.WriteLine("Build failed. Cannot run the built project.");
            }

            return codeBuildInfo;
        }

        public static async Task<ExecutableOutputResult> FindAndRunExecutableAndCaptureOutput(string pathToFunctions, string projectToRun, string framework = "", string args = "")
        {
            string executablePath = FindExecutable(pathToFunctions, projectToRun, framework);

            if (!string.IsNullOrEmpty(executablePath))
            {
                return await RunExecutableAndCaptureOutputAsync(executablePath, args);
            }

            return new ExecutableOutputResult();
        }


        // Function to find the executable file after build
        public static string FindExecutable(string pathToFunctions, string projectName, string framework)
        {
            // Assuming a typical .NET Core/5+ project structure
            // You might need to adjust this based on your project setup
            string binPath = Path.Combine(pathToFunctions, projectName, "FunctionApplication", "bin", "Debug", framework); // Adjust framework if needed
            string executableName = $"FunctionApplication.exe"; // Replace with your executable name
            string executablePath = Path.Combine(binPath, executableName);

            if (File.Exists(executablePath))
            {
                return executablePath;
            }

            // Check the Release folder as well
            binPath = Path.Combine(pathToFunctions, projectName, "FunctionApplication", "bin", "Release", framework); // Adjust framework if needed
            executablePath = Path.Combine(binPath, executableName);

            if (File.Exists(executablePath))
            {
                return executablePath;
            }

            return null; // Executable not found
        }

        // Function to run the executable and capture its output
        public static ExecutableOutputResult RunExecutableAndCaptureOutput(string executablePath, string? arguments = "")
        {
            Process process = new Process();
            process.StartInfo.FileName = executablePath;
            process.StartInfo.Arguments = arguments; // Pass any arguments to the executable here
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            ExecutableOutputResult result = new ExecutableOutputResult();

            try
            {
                process.Start();

                // Read the output and error streams
                result.Output = process.StandardOutput.ReadToEnd();
                result.Error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine("Executable Output:");
                    Console.WriteLine(result.Output);
                }
                else
                {
                    Console.WriteLine("Executable Error:");
                    Console.WriteLine(result.Error);
                }
                result.ExecutionCompleted = true;
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running executable: {ex.Message}");
                result.ExecutionCompleted = false;
                return result;
            }
        }
        public static async Task<ExecutableOutputResult> RunExecutableAndCaptureOutputAsync(string executablePath, string? arguments = "")
        {
            Process process = new Process();
            process.StartInfo.FileName = executablePath;
            process.StartInfo.Arguments = arguments; // Pass any arguments to the executable here
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            ExecutableOutputResult result = new ExecutableOutputResult();

            try
            {
                process.Start();

                // Read the output and error streams
                result.Output = process.StandardOutput.ReadToEnd();
                result.Error = process.StandardError.ReadToEnd();

                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine("Executable Output:");
                    Console.WriteLine(result.Output);
                }
                else
                {
                    Console.WriteLine("Executable Error:");
                    Console.WriteLine(result.Error);
                }
                result.ExecutionCompleted = true;
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running executable: {ex.Message}");
                result.ExecutionCompleted = false;
                return result;
            }
        }
        // Function to build the project using dotnet CLI
        public static BuildOutputResult BuildProject(string path)
        {
            // Path to the target project's directory
            string projectPath = path;

            // Create a new process to run the dotnet build command
            Process process = new Process();
            process.StartInfo.FileName = "dotnet"; // Use "dotnet" command
            process.StartInfo.Arguments = $"build \"{projectPath}\""; // Arguments to build the project
            process.StartInfo.UseShellExecute = false; // Don't use the OS shell
            process.StartInfo.RedirectStandardOutput = true; // Redirect standard output to capture build output
            process.StartInfo.RedirectStandardError = true; // Redirect standard error to capture error messages
            process.StartInfo.CreateNoWindow = true; // Don't create a new window for the process

            BuildOutputResult result = new BuildOutputResult();

            try
            {
                // Start the process
                process.Start();

                // Read the build output (optional)
                result.Output = process.StandardOutput.ReadToEnd();
                result.Error = process.StandardError.ReadToEnd();

                // Wait for the process to exit
                process.WaitForExit();

                // Check the exit code to determine if the build was successful
                if (process.ExitCode == 0)
                {
                    Console.WriteLine("Build successful!");
                    Console.WriteLine(result.Output);
                    result.BuildCompleted = true;
                }
                else
                {
                    Console.WriteLine("Build failed!");
                    Console.WriteLine(result.Error);
                    result.BuildCompleted = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message}");
                result.BuildCompleted = false;
            }

            return result;
        }
    }


}
