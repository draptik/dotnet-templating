module TemplatingConsole

open System
open Argu

open Arguments
open TemplatingLib.Types
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
    let sln = getSolutionName results
    let outDir = getOutputDirectory results
    let resDir = getResourceDirectory results
    let templates = getDefaultTemplates resDir

    let result = workflow sln outDir templates

    printfn $"Workflow: %A{result}"
    0
