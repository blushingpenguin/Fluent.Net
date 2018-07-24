#load "./parameters.cake"
#load "./paths.cake"

#tool "nuget:?package=OpenCover&version=4.6.519"
#tool "nuget:?package=ReportGenerator&version=3.1.2"
#tool "nuget:?package=NUnit.ConsoleRunner&version=3.8.0"
#tool "nuget:?package=OpenCoverToCoberturaConverter&version=0.3.3"

var parameters = BuildParameters.GetParameters(Context);
var paths = BuildPaths.GetPaths(Context, parameters);

Setup(context =>
{
    if (!DirectoryExists(paths.Directories.Artifacts))
    {
        CreateDirectory(paths.Directories.Artifacts);
    }

    if (!DirectoryExists(paths.Directories.TestResults))
    {
        CreateDirectory(paths.Directories.TestResults);
    }
});

Task("Clean")
    .Does(() =>
{
    CleanDirectories(paths.Directories.ToClean);
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetCoreRestore(paths.Files.Solution.ToString());
});

Task("Build")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetCoreBuild(paths.Files.Solution.ToString(), new DotNetCoreBuildSettings
    {
        Configuration = parameters.Configuration,
        ArgumentCustomization = arg => arg.AppendSwitch("/p:DebugType","=","Full")
    });
});

Task("Run-Tests")
    .IsDependentOn("Build")
    .Does(() =>
{
    var success = true;
    var openCoverSettings = new OpenCoverSettings
    {
        OldStyle = true,
        MergeOutput = true,
        SkipAutoProps = true
    }
    .WithFilter("+[*]* -[*.Test*]* -[nunit*]* -[EfCore.InMemoryHelpers]*");
    openCoverSettings.ExcludedAttributeFilters.Add("System.CodeDom.Compiler.GeneratedCodeAttribute");

    /*
        This works, might be faster for multiple projects if dotnet test runs the tests in parallel?
        I'm leaving the code that uses the built-in .NET test target for now
     */
    /*
    if (parameters.UseDotNetTest)
    {
        Action<ICakeContext> testAction = context => 
        {
            var argumentBuilder = new ProcessArgumentBuilder();
            argumentBuilder.Append("test")
                           .Append(string.Join(" ", paths.Files.TestProjects.Select(val => MakeAbsolute(val).ToString())))
                           .Append("--configuration Release")
                           .Append("--no-build");
            FilePath dotnetPath = Context.Tools.Resolve("dotnet.exe");
            context.StartProcess(dotnetPath, new ProcessSettings
            {
                Arguments = argumentBuilder
            });
        };

        OpenCover(testAction, paths.Files.TestCoverageOutputOpenCover, openCoverSettings);
    }
    else 
    */
    if (parameters.UseDotNetVsTest)
    {
        Action<ICakeContext> testAction = context => 
        {
            var argumentBuilder = new ProcessArgumentBuilder();
            argumentBuilder.Append("vstest")
                           .Append(string.Join(" ", paths.Files.TestAssemblies.Select(val => MakeAbsolute(val).ToString())))
                           .Append("--Parallel");
            FilePath dotnetPath = Context.Tools.Resolve("dotnet.exe");
            context.StartProcess(dotnetPath, new ProcessSettings
            {
                Arguments = argumentBuilder
            });
        };

        OpenCover(testAction, paths.Files.TestCoverageOutputOpenCover, openCoverSettings);
    }
    else
    {
        foreach(var project in paths.Files.TestProjects)
        {
            try 
            {
                var projectFile = MakeAbsolute(project).ToString();
                var dotNetTestSettings = new DotNetCoreTestSettings
                {
                    Configuration = parameters.Configuration,
                    NoBuild = true
                };

                OpenCover(context => context.DotNetCoreTest(projectFile, dotNetTestSettings),
                    paths.Files.TestCoverageOutputOpenCover, openCoverSettings);
            }
            catch(Exception ex)
            {
                success = false;
                Error("There was an error while running the tests", ex);
            }
        }
    }

    if(success == false)
    {
        throw new CakeException("There was an error while running the tests");
    }
});

Task("Coverage")
    .IsDependentOn("Run-Tests")
    .Does(() =>
{    
    ReportGenerator(paths.Files.TestCoverageOutputOpenCover, paths.Directories.TestResults);
});

Task("Coverage-Cobertura")
    .IsDependentOn("Coverage")
    .Does(() =>
{
    FilePath converterPath = Context.Tools.Resolve("OpenCoverToCoberturaConverter.exe");
    var argumentBuilder = new ProcessArgumentBuilder();
    argumentBuilder
        .Append("-input:\"" + paths.Files.TestCoverageOutputOpenCover + "\"")
        .Append("-output:\"" + paths.Files.TestCoverageOutputCobertura + "\"")
        .Append("-sources:\"" + paths.Directories.RootDir + "\"");

    StartProcess(converterPath, new ProcessSettings
    {
        Arguments = argumentBuilder
    });
});

RunTarget(parameters.Target);
