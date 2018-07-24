#load "./parameters.cake"

public class BuildPaths
{
    public BuildFiles Files { get; private set; }
    public BuildDirectories Directories { get; private set; }

    public static BuildPaths GetPaths(ICakeContext context, BuildParameters parameters)
    {
        var configuration =  parameters.Configuration;
        var buildDirectories = GetBuildDirectories(context);
        var testAssemblies = buildDirectories.TestDirs
                                             .Select(dir => dir.Combine("bin")
                                                               .Combine(configuration)
                                                               .Combine(parameters.TargetFramework)
                                                               .CombineWithFilePath(dir.GetDirectoryName() + ".dll"))
                                             .ToList();
        var testProjects =  buildDirectories.TestDirs.Select(dir => dir.CombineWithFilePath(dir.GetDirectoryName() + ".csproj")).ToList();

        var buildFiles = new BuildFiles(
            buildDirectories.RootDir.CombineWithFilePath("Fluent.Net.sln"),
            buildDirectories.TestResults.CombineWithFilePath("OpenCover.xml"),
            buildDirectories.TestResults.CombineWithFilePath("Cobertura.xml"),
            testAssemblies,
            testProjects);
        
        return new BuildPaths
        {
            Files = buildFiles,
            Directories = buildDirectories
        };
    }

    public static BuildDirectories GetBuildDirectories(ICakeContext context)
    {
        var rootDir = (DirectoryPath)context.Directory("../");
        var artifacts = rootDir.Combine(".artifacts");
        var testResults = artifacts.Combine("Test-Results");
        // var integrationTestsDir = rootDir.Combine(context.Directory("Fluent.Net.Test"));
        var unitTestsDir = rootDir.Combine(context.Directory("Fluent.Net.Test"));
        var mainProjectDir = rootDir.Combine(context.Directory("Fluent.Net"));

        var testDirs = new []{
                                unitTestsDir //,
                                //integrationTestsDir
                            };
        var toClean = new[] {
                                 testResults,
                                 // integrationTestsDir.Combine("bin"),
                                 // integrationTestsDir.Combine("obj"),
                                 unitTestsDir.Combine("bin"),
                                 unitTestsDir.Combine("obj"),
                                 mainProjectDir.Combine("bin"),
                                 mainProjectDir.Combine("obj"),
                            };
        return new BuildDirectories(rootDir,
                                    artifacts,
                                    testResults,
                                    testDirs, 
                                    toClean);
    }
}

public class BuildFiles
{
    public FilePath Solution { get; private set; }
    public FilePath TestCoverageOutputOpenCover { get; private set; }
    public FilePath TestCoverageOutputCobertura { get; private set; }
    public ICollection<FilePath> TestAssemblies { get; private set; }
    public ICollection<FilePath> TestProjects { get; private set; }

    public BuildFiles(FilePath solution,
                      FilePath testCoverageOutputOpenCover,
                      FilePath testCoverageOutputCobertura,
                      ICollection<FilePath> testAssemblies,
                      ICollection<FilePath> testProjects)
    {
        Solution = solution;
        TestAssemblies = testAssemblies;
        TestCoverageOutputOpenCover = testCoverageOutputOpenCover;
        TestCoverageOutputCobertura = testCoverageOutputCobertura;
        TestProjects = testProjects;
    }
}

public class BuildDirectories
{
    public DirectoryPath RootDir { get; private set; }
    public DirectoryPath Artifacts { get; private set; }
    public DirectoryPath TestResults { get; private set; }
    public ICollection<DirectoryPath> TestDirs { get; private set; }
    public ICollection<DirectoryPath> ToClean { get; private set; }

    public BuildDirectories(
        DirectoryPath rootDir,
        DirectoryPath artifacts,
        DirectoryPath testResults,
        ICollection<DirectoryPath> testDirs,
        ICollection<DirectoryPath> toClean)
    {
        RootDir = rootDir;
        Artifacts = artifacts;
        TestDirs = testDirs;
        ToClean = toClean;
        TestResults = testResults;
    }
}