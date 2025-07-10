using LombdaAgentSDK.Agents.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Demos
{
    public class CommandLineTool
    {
        public static List<string> ConsoleLines { get; set; } = new List<string>();

        [ToolAttribute(
            Description = 
            "Use this to run window CMD prompts", 
            In_parameters_description = ["Command to run in windows"])]
        public static string RunCommandLine(string command)
        {
            string dir = Directory.GetCurrentDirectory();
            ConsoleLines.Add($"{dir}>{command}");
            if(!command.StartsWith("/c "))
            {
                command = "/c " + command; // Ensure the command starts with /c to run in cmd.exe
            }
            // Create a new process to run the command
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe"; // Command to run
            process.StartInfo.Arguments = command+ " & echo current directory is: & cd"; // Arguments for the command
            process.StartInfo.UseShellExecute = false; // Don't use the OS shell
            process.StartInfo.RedirectStandardOutput = true; // Redirect standard output to capture output
            process.StartInfo.RedirectStandardError = true; // Redirect standard error to capture error messages
            process.StartInfo.CreateNoWindow = true; // Don't create a new window for the process
            process.StartInfo.WorkingDirectory = dir; // Set the working directory to the current directory

            Console.WriteLine($"Running command: {command} in directory: {dir}");
            Console.WriteLine("Are you sure you want to run this command? (y/n)");
            string userInput = Console.ReadLine();
            if (userInput?.ToLower() != "y")
            {
                Console.WriteLine("Command execution cancelled.");
                return "Command execution cancelled by user.";
            }
            try
            {
                // Start the process
                process.Start();
                // Read the output and error streams
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                // Wait for the process to exit
                process.WaitForExit();
                if (process.ExitCode == 0)
                {
                    Console.WriteLine("Command executed successfully!");
                    ConsoleLines.Add(output);
                    Console.WriteLine($"{output}");
                    return output;
                }
                else
                {
                    Console.WriteLine("Command execution failed!");
                    Console.WriteLine(error);
                    ConsoleLines.Add(error);
                    return error;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message}");
                ConsoleLines.Add(ex.Message);
                return ex.Message;
            }
        }
    }
}
