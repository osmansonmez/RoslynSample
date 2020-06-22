using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            var directory = new DirectoryInfo(@"..\..\..\..\SampleApp1");

            BuildSolution(directory.FullName);

            var instances = MSBuildLocator.QueryVisualStudioInstances().ToList();
            MSBuildLocator.RegisterInstance(instances.First());

            using (var workspace = MSBuildWorkspace.Create())
            {
                var sln = Path.Combine(directory.FullName, "SampleApp1.sln");
                var solution = await workspace.OpenSolutionAsync(sln);
                var projects = solution.Projects;
                foreach (var project in projects)
                {
                    Console.WriteLine(project.Name);
                    var compilation = await project.GetCompilationAsync();
                    var diagnostics = compilation.GetDiagnostics();
                    foreach (var diag in diagnostics)
                    {
                        Console.WriteLine(diag.ToString());
                    }

                    var errorCount = diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error);

                    var documents = project.Documents;
                    foreach (var document in documents)
                    {
                        await ProcessDocument(document, solution, project, compilation);
                    }
                }
            }
            Console.ReadLine();
        }

        private static async Task ProcessDocument(Document document, Solution sln, Project project, Compilation compilation)
        {
            SemanticModel semanticModel = await document.GetSemanticModelAsync();
            var syntaxTree = semanticModel.SyntaxTree;
            var root = syntaxTree.GetRoot();

            var classes = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>();

            foreach (var cls in classes)
            {
                await ProcessClass(cls, sln, semanticModel, compilation);
            }

            var interfaces = root.DescendantNodes()
     .OfType<InterfaceDeclarationSyntax>();
        }

        public static async Task ProcessClass(ClassDeclarationSyntax cls, Solution solution, SemanticModel semanticModel, Compilation compilation)
        {
            var classSignature = semanticModel.GetDeclaredSymbol(cls);
            Console.WriteLine(classSignature.ToString());

            var methods = cls.DescendantNodes()
              .OfType<MethodDeclarationSyntax>();

            var constucterList = cls.DescendantNodes()
                      .OfType<ConstructorDeclarationSyntax>().Where(method =>
              method.Modifiers.Where(modifier =>
                  modifier.Kind() == SyntaxKind.PublicKeyword)
              .Any());

            SyntaxList<AttributeListSyntax> clsAttributes = cls.AttributeLists;

            var variableList = cls.DescendantNodes().OfType<VariableDeclarationSyntax>();

            foreach (var item in variableList)
            {
                foreach (var variable in item.Variables)
                {
                    Console.WriteLine(variable.Identifier.ValueText.ToString());

                }
            }

            var variableAssignments = cls.DescendantNodes().OfType<AssignmentExpressionSyntax>();

            foreach (var variableAssignment in variableAssignments)
            {

                Console.WriteLine($"Left: {variableAssignment.Left}, Right: {variableAssignment.Right}");

            }

            foreach (var method in methods)
            {
             await   ProcessMethod(method, cls, solution, semanticModel, compilation);
            }
            await Task.CompletedTask;
        }

        public static async Task ProcessMethod(MethodDeclarationSyntax Method, ClassDeclarationSyntax cls, Solution sln, SemanticModel semanticModel, Compilation compilation)
        {
            var methodSymbol = semanticModel.GetDeclaredSymbol(Method);
            Console.WriteLine(methodSymbol.ToString()); //AppTest.B.ADD(int)

            var arguments = Method.ParameterList.Parameters;
            foreach (var arg in arguments)
            {
                var typeInfo = semanticModel.GetTypeInfo(arg.ChildNodes().First());
                Console.WriteLine($"param Name : {arg.Identifier.ToString()}, type : {typeInfo.Type.ToString()}");
                
            }

            var invocationList = Method.DescendantNodes()
                      .OfType<InvocationExpressionSyntax>();

            foreach (var invocation in invocationList)
            {
                await ProcessInvocation(invocation, sln, semanticModel, cls);
            }

            await Task.CompletedTask;

        }

        public static async Task ProcessInvocation(InvocationExpressionSyntax invocation, Solution solution, SemanticModel semanticModel, ClassDeclarationSyntax cls)
        {
            string methodName = string.Empty;
            var expr = invocation.Expression;
            if (expr is IdentifierNameSyntax r)
            {
                methodName = r.GetFirstToken().ValueText;
            }

            if (expr is MemberAccessExpressionSyntax m)
            {
                methodName = m.Name.GetFirstToken().ValueText;
            }

            Console.WriteLine(methodName);

            var invokedSymbol = semanticModel.GetSymbolInfo(invocation).Symbol;
            var symbolInfo = semanticModel.GetSymbolInfo(invocation);

            //TODO dynamic nesne geliyor!!!!
            if (symbolInfo.CandidateSymbols != null && symbolInfo.CandidateSymbols.Count() > 0)
            {
                invokedSymbol = symbolInfo.CandidateSymbols.First();
            }

            if (invokedSymbol == null)
            {

                invokedSymbol = semanticModel.GetDeclaredSymbol(invocation);

            }

            IMethodSymbol methodSymbol = invokedSymbol as IMethodSymbol;

            Console.WriteLine(invokedSymbol.ToString()); //AppTest.B.ADD(int)
            int param = 0;
            
            foreach (var arg in invocation.ArgumentList.Arguments)
            {
                var typeInfo = semanticModel.GetTypeInfo(arg.ChildNodes().First());
                var value = arg.Expression.GetFirstToken().ValueText;

                Console.WriteLine($"argName: { methodSymbol.Parameters[param].Name}, argType: {typeInfo.Type.ToString()}, Value:{arg.GetFirstToken().ValueText}");
                param++;
            }
          

            await Task.CompletedTask;

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
