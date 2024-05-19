module TemplatingConsole

open System
open System.IO
open Argu
open FsToolkit.ErrorHandling

open TemplatingLib.Io

type CliArguments =
    | [<AltCommandLine("-n"); Unique>] Solution_Name of name: string
    | [<AltCommandLine("-o"); Unique>] Output_Directory of path: string
    | [<AltCommandLine("-r"); Unique>] Resource_Directory of path: string
    | [<AltCommandLine("-f"); Unique>] Force of bool // TODO

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Solution_Name _ -> "The name of the solution to create"
            | Output_Directory _ -> "The directory where the project will be created"
            | Resource_Directory _ -> "The directory where the resources are located"
            | Force _ -> "Force overwrite of existing files"

let errorHandler =
    ProcessExiter(
        colorizer =
            function
            | ErrorCode.HelpText -> None
            | _ -> Some ConsoleColor.Red
    )

let parser =
    ArgumentParser.Create<CliArguments>(programName = "TemplatingConsole", errorHandler = errorHandler)

[<EntryPoint>]
let main argv =
    let results = parser.ParseCommandLine argv

    let defaultResourceDirectory = "../TemplatingLib/resources"

    let resourceDirectory =
        Path.GetFullPath(results.GetResult(Resource_Directory, defaultValue = defaultResourceDirectory))

    let solutionName = results.GetResult(Solution_Name, defaultValue = "Foo")

    let outputDirectory =
        results.GetResult(Output_Directory, defaultValue = "~/tmp/foo1")

    printfn $"Creating root folder: %s{outputDirectory}"

    let slash = Path.DirectorySeparatorChar

    let src = "src"
    let tests = "tests"
    let DirectoryBuildProps = "Directory.Build.props"
    let DirectoryPackagesProps = "Directory.Packages.props"
    let gitAttributes = ".gitattributes"
    let defaultLibName = "MyLib"
    let defaultLibTestName = "MyLib.Tests"

    let rootBuildPropsTemplate =
        Path.Combine(resourceDirectory, $"{DirectoryBuildProps}.template")

    let rootPackagesTemplate =
        Path.Combine(resourceDirectory, $"{DirectoryPackagesProps}.template")

    let gitAttributesTemplate =
        Path.Combine(resourceDirectory, $"{gitAttributes}.template")

    let srcDirBuildPropsTemplate =
        Path.Combine(resourceDirectory, $"src{slash}{DirectoryBuildProps}.template")

    let testsDirBuildPropsTemplate =
        Path.Combine(resourceDirectory, $"tests{slash}{DirectoryBuildProps}.template")

    let workflow =
        result {
            let! validSolutionName = ValidSolutionName.create solutionName

            let! (ValidatedPath outputPath) = tryToCreateOutputDirectory outputDirectory

            let! srcDir = tryToCreateOutputDirectory (Path.Combine(outputPath, src))
            let! testsDir = tryToCreateOutputDirectory (Path.Combine(outputPath, tests))

            let! _ = tryCreateConfigFile GitIgnore outputPath
            let! _ = tryCreateConfigFile EditorConfig outputPath
            let! _ = tryCreateConfigFile GlobalJson outputPath

            let! _ = tryCopy rootBuildPropsTemplate (Path.Combine(outputPath, DirectoryBuildProps))
            let! _ = tryCopy rootPackagesTemplate (Path.Combine(outputPath, DirectoryPackagesProps))
            let! _ = tryCopy gitAttributesTemplate (Path.Combine(outputPath, gitAttributes))

            let! _ = tryCopy srcDirBuildPropsTemplate (Path.Combine(outputPath, src, DirectoryBuildProps))
            let! _ = tryCopy testsDirBuildPropsTemplate (Path.Combine(outputPath, tests, DirectoryBuildProps))

            let! sln = tryCreateSolution solutionName outputPath

            let! libProject =
                tryToCreateDotnetProjectWithoutRestore
                    { ProjectCreationInputs.ProjectType = ProjectType.ClassLib
                      ProjectCreationInputs.ProjectName = ValidSolutionName.appendTo validSolutionName defaultLibName
                      ProjectCreationInputs.Language = Language.CSharp
                      ProjectCreationInputs.Path = srcDir }

            let! testProject =
                tryToCreateDotnetProjectWithoutRestore
                    { ProjectCreationInputs.ProjectType = ProjectType.XUnit
                      ProjectCreationInputs.ProjectName =
                        ValidSolutionName.appendTo validSolutionName defaultLibTestName
                      ProjectCreationInputs.Language = Language.CSharp
                      ProjectCreationInputs.Path = testsDir }

            return ()
        }

    printfn "Workflow: %A" workflow

    0
