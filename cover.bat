@echo off

rem Install OpenCover and ReportGenerator, and save the path to their executables.
echo Installing packages
nuget install -Verbosity quiet -OutputDirectory packages -Version 4.6.519 OpenCover
nuget install -Verbosity quiet -OutputDirectory packages -Version 3.1.2 ReportGenerator

echo Building Release with symbols
dotnet build -c Release /p:DebugType=Full

if exist coverage\OpenCover.xml del /q coverage\OpenCover.xml
if not exist coverage mkdir coverage

echo Running OpenCover
"packages\OpenCover.4.6.519\tools\OpenCover.Console.exe" -target:"dotnet.exe" -targetargs:"test Fluent.Net.Test\Fluent.Net.Test.csproj --configuration Release --no-build" -filter:"+[*]* -[*.Test*]* -[nunit*]* -[EfCore.InMemoryHelpers]*" -excludebyattribute:"System.CodeDom.Compiler.GeneratedCodeAttribute" -skipautoprops -oldStyle -mergeoutput -register:user -output:"coverage\OpenCover.xml"

echo Running ReportGenerator
"packages\ReportGenerator.3.1.2\tools\ReportGenerator.exe" "-reports:coverage\OpenCover.xml" "-targetdir:coverage"
