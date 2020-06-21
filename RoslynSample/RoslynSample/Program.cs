using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoslynSample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var directory = @"C:\Users\EOS\source\repos\SampleApp1";

            BuildSolution(directory);
            var instances = MSBuildLocator.QueryVisualStudioInstances().ToList();
            var msbuildDeploymentToUse = AskWhichMSBuildToUse(instances);

            MSBuildLocator.RegisterDefaults();


            using (var workspace = MSBuildWorkspace.Create())
            {
                var sln = Path.Combine(directory, "SampleApp1.sln");
                var solution = await workspace.OpenSolutionAsync(sln);
                var projects =  solution.Projects;
                foreach (var project in projects)
                {
                    Console.WriteLine(project.Name);
                    var compilation = await project.GetCompilationAsync();
                    var diagnostics = compilation.GetDiagnostics();
                    foreach (var diag in diagnostics)
                    {
                        Console.WriteLine(diag.ToString());
                    }

                    var documents = project.Documents;
                }
            }
            Console.ReadLine();
        }

        private static void ProcessDocument(Document document, Solution sln, Project project, Compilation compilation)
        {

        }

        private static void ProcessClasses(Document document, Solution sln, Project project, Compilation compilation)
        {

        }

        private static Tuple<VisualStudioInstance, string> AskWhichMSBuildToUse(List<VisualStudioInstance> instances)
        {
            if (instances.Count == 0)
            {
                Console.WriteLine("No Visual Studio instances found!");
            }

            Console.WriteLine($"0) Custom path");
            for (var i = 1; i <= instances.Count; i++)
            {
                var instance = instances[i - 1];
                var recommended = string.Empty;

                // The dev console is probably always the right choice because the user explicitly opened
                // one associated with a Visual Studio install. It will always be first in the list.
                if (instance.DiscoveryType == DiscoveryType.DeveloperConsole)
                    recommended = " (Recommended!)";

                Console.WriteLine($"{i}) {instance.Name} - {instance.Version}{recommended}- {instance.MSBuildPath}");
            }

            var lastVersion = instances.OrderByDescending(x => x.Version.Major).FirstOrDefault();
            return new Tuple<VisualStudioInstance, string>(lastVersion, null);
        }

        public static void BuildSolution(string slnFolder)
        {
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = slnFolder,
                FileName = Path.Combine(Environment.SystemDirectory, "cmd.exe"),

                // Redirects the standard input so that commands can be sent to the shell.
                RedirectStandardInput = true,
                // Runs the specified command and exits the shell immediately.
                Arguments = @"/c dotnet build"
            };


            var process = Process.Start(startInfo);
            process.OutputDataReceived += ProcessOutputDataHandler;
            process.ErrorDataReceived += ProcessErrorDataHandler;
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Send a directory command and an exit command to the shell
            process.StandardInput.WriteLine("dir");
            process.StandardInput.WriteLine("exit");

            process.WaitForExit();

        }

        public static void ProcessOutputDataHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            Console.WriteLine(outLine.Data);
        }

        public static void ProcessErrorDataHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            Console.WriteLine(outLine.Data);
        }

    }
}
