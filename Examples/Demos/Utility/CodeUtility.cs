using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
namespace Examples.Demos
{
   
    public class CodeUtility
    {
        public class CodeBuildInfo
        {
           
            public string BuildPath { get; set; }
            
            public string ProjectName { get; set; }
            public ExecutableOutputResult? ExecutableResult { get; set; }

            public BuildOutputResult? BuildResult { get; set; }

            public CodeBuildInfo() { }

            public CodeBuildInfo(string buildPath, string projectName) { 
                BuildPath = buildPath; 
                ProjectName = projectName; 
            }
        }

        public static CodeBuildInfo BuildAndRunProject(string pathToSolution, string projectToRun, bool runProject = false)
        {
            CodeBuildInfo codeBuildInfo = new CodeBuildInfo(pathToSolution, projectToRun);
            // Path to the target project's directory
            string projectPath = pathToSolution;

            // ... (Code for programmatically building the project as shown in the previous example) ...

            // After the build is successful:
            codeBuildInfo.BuildResult = BuildProject(projectPath);
            
            if (codeBuildInfo.BuildResult.BuildCompleted)
            {
                Console.WriteLine("Build successful! Now running the built project...");
                // Find the executable file
                string executablePath = FindExecutable(projectPath, projectToRun);

                if (!string.IsNullOrEmpty(executablePath))
                {
                    if (runProject)
                    {
                        // Run the executable and capture its output
                        codeBuildInfo.ExecutableResult = RunExecutableAndCaptureOutput(executablePath);
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

        // Function to find the executable file after build
        public static string FindExecutable(string projectPath, string projectName)
        {
            // Assuming a typical .NET Core/5+ project structure
            // You might need to adjust this based on your project setup
            string binPath = Path.Combine(projectPath, projectName, "bin", "Debug", "net8.0"); // Adjust framework if needed
            string executableName = $"{projectName}.exe"; // Replace with your executable name
            string executablePath = Path.Combine(binPath, executableName);

            if (File.Exists(executablePath))
            {
                return executablePath;
            }

            // Check the Release folder as well
            binPath = Path.Combine(projectPath, projectName, "bin", "Release", "net8.0"); // Adjust framework if needed
            executablePath = Path.Combine(binPath, executableName);

            if (File.Exists(executablePath))
            {
                return executablePath;
            }

            return null; // Executable not found
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

        // Function to run the executable and capture its output
        public static ExecutableOutputResult RunExecutableAndCaptureOutput(string executablePath)
        {
            Process process = new Process();
            process.StartInfo.FileName = executablePath;
            process.StartInfo.Arguments = ""; // Pass any arguments to the executable here
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
