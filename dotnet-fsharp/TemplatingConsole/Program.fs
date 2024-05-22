module TemplatingConsole

open System
open System.IO
open Argu

open TemplatingConsole.Arguments
open TemplatingLib.Io

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

    let resourceDirectory =
        Path.GetFullPath(
            results.GetResult(Resource_Directory, defaultValue = TemplatingLib.Defaults.defaultResourceDirectory)
        )

    let solutionName =
        results.GetResult(Solution_Name, defaultValue = TemplatingLib.Defaults.defaultSolutionName)

    let outputDirectory =
        results.GetResult(Output_Directory, defaultValue = TemplatingLib.Defaults.defaultOutputDirectory)

    let forceOverWrite =
        results.GetResult(Force, defaultValue = TemplatingLib.Defaults.defaultForceOverwrite)

    printfn $"Creating root folder: %s{outputDirectory}"

    let rootBuildPropsTemplate =
        Path.Combine(resourceDirectory, $"{TemplatingLib.Defaults.DirectoryBuildProps}.template")

    let rootPackagesTemplate =
        Path.Combine(resourceDirectory, $"{TemplatingLib.Defaults.DirectoryPackagesProps}.template")

    let gitAttributesTemplate =
        Path.Combine(resourceDirectory, $"{TemplatingLib.Defaults.gitAttributes}.template")

    let srcDirBuildPropsTemplate =
        Path.Combine(
            resourceDirectory,
            TemplatingLib.Defaults.src,
            $"{TemplatingLib.Defaults.DirectoryBuildProps}.template"
        )

    let testsDirBuildPropsTemplate =
        Path.Combine(
            resourceDirectory,
            TemplatingLib.Defaults.tests,
            $"{TemplatingLib.Defaults.DirectoryBuildProps}.template"
        )

    let result =
        workflow
            solutionName
            outputDirectory
            (rootBuildPropsTemplate,
             srcDirBuildPropsTemplate,
             testsDirBuildPropsTemplate,
             rootPackagesTemplate,
             gitAttributesTemplate,
             forceOverWrite)

    printfn $"Workflow: %A{result}"

    0
