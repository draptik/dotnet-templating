module TemplatingConsole

open System
open System.IO
open Argu
open FsToolkit.ErrorHandling

open TemplatingLib.Io
open TemplatingLib.Types

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
        Path.Combine(resourceDirectory, src, $"{DirectoryBuildProps}.template")

    let testsDirBuildPropsTemplate =
        Path.Combine(resourceDirectory, tests, $"{DirectoryBuildProps}.template")

    let workflow =
        result {
            let! validSolutionName = ValidName.create solutionName

            let! outputPath = tryToCreateOutputDirectory outputDirectory

            let! _ = tryToCreateOutputDirectory (Path.Combine(outputPath, src))
            let! _ = tryToCreateOutputDirectory (Path.Combine(outputPath, tests))

            let! _ = tryCreateConfigFile GitIgnore outputPath
            let! _ = tryCreateConfigFile EditorConfig outputPath
            let! _ = tryCreateConfigFile GlobalJson outputPath

            let! _ = tryCopy rootBuildPropsTemplate (Path.Combine(outputPath, DirectoryBuildProps))
            let! _ = tryCopy rootPackagesTemplate (Path.Combine(outputPath, DirectoryPackagesProps))
            let! _ = tryCopy gitAttributesTemplate (Path.Combine(outputPath, gitAttributes))

            let! _ = tryCopy srcDirBuildPropsTemplate (Path.Combine(outputPath, src, DirectoryBuildProps))
            let! _ = tryCopy testsDirBuildPropsTemplate (Path.Combine(outputPath, tests, DirectoryBuildProps))

            printfn $"Creating solution: %s{solutionName}"
            let! _ = tryCreateSolution solutionName outputPath

            let lib = ValidName.appendTo validSolutionName defaultLibName
            let libName = ValidName.value lib
            let libPath = ValidatedPath(Path.Combine(outputPath, src, libName))

            let selectedLanguage = Language.CSharp

            let! libProject =
                tryToCreateDotnetProjectWithoutRestore
                    { ProjectCreationInputs.ProjectType = ProjectType.ClassLib
                      ProjectCreationInputs.ProjectName = lib
                      ProjectCreationInputs.Language = selectedLanguage
                      ProjectCreationInputs.Path = libPath }

            printfn "Patching lib project files 1/1..."
            let! _ = libProject |> tryReplacePropertyGroupFromFile selectedLanguage

            let test = ValidName.appendTo validSolutionName defaultLibTestName
            let testName = ValidName.value test
            let testPath = ValidatedPath(Path.Combine(outputPath, tests, testName))
            printfn $"Creating test: %s{testName}"

            let! testProject =
                tryToCreateDotnetProjectWithoutRestore
                    { ProjectCreationInputs.ProjectType = ProjectType.XUnit
                      ProjectCreationInputs.ProjectName = test
                      ProjectCreationInputs.Language = selectedLanguage
                      ProjectCreationInputs.Path = testPath }

            printfn "Creating dependencies..."
            let! _ = tryAddProjectDependency testPath libPath

            printfn "Patching test project files 1/2..."
            let! _ = testProject |> tryReplacePropertyGroupFromFile selectedLanguage
            printfn "Patching test project files 2/2..."
            let! _ = testProject |> tryReplaceItemGroupFromFile selectedLanguage

            printfn "Adding projects to solution file..."
            let! _ = tryAddProjectToSolution outputPath libPath
            let! _ = tryAddProjectToSolution outputPath testPath

            printfn "Done"
            return ()
        }

    printfn "Workflow: %A" workflow

    0
