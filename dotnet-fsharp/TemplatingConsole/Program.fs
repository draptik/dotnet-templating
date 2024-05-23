﻿module TemplatingConsole

open System
open System.IO
open Argu

open Arguments
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
            results.GetResult(Resource_Directory, defaultValue = TemplatingLib.Constants.defaultResourceDirectory)
        )

    let solutionName =
        results.GetResult(Solution_Name, defaultValue = TemplatingLib.Constants.defaultSolutionName)

    let outputDirectory =
        results.GetResult(Output_Directory, defaultValue = TemplatingLib.Constants.defaultOutputDirectory)

    let forceOverWrite =
        results.GetResult(Force, defaultValue = TemplatingLib.Constants.defaultForceOverwrite)

    let rootBuildPropsTemplate =
        Path.Combine(resourceDirectory, $"{TemplatingLib.Constants.DirectoryBuildProps}.template")

    let rootPackagesTemplate =
        Path.Combine(resourceDirectory, $"{TemplatingLib.Constants.DirectoryPackagesProps}.template")

    let gitAttributesTemplate =
        Path.Combine(resourceDirectory, $"{TemplatingLib.Constants.gitAttributes}.template")

    let srcDirBuildPropsTemplate =
        Path.Combine(
            resourceDirectory,
            TemplatingLib.Constants.src,
            $"{TemplatingLib.Constants.DirectoryBuildProps}.template"
        )

    let testsDirBuildPropsTemplate =
        Path.Combine(
            resourceDirectory,
            TemplatingLib.Constants.tests,
            $"{TemplatingLib.Constants.DirectoryBuildProps}.template"
        )

    let defaultTemplates =
        (rootBuildPropsTemplate,
         srcDirBuildPropsTemplate,
         testsDirBuildPropsTemplate,
         rootPackagesTemplate,
         gitAttributesTemplate,
         forceOverWrite)

    let result = workflow solutionName outputDirectory defaultTemplates

    printfn $"Workflow: %A{result}"

    0
