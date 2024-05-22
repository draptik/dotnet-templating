module TemplatingConsole

open System
open System.IO
open Argu

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
