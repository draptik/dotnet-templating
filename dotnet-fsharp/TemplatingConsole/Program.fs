module TemplatingConsole

open System
open System.IO
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

    let resourceDirectory =
        Path.GetFullPath(
            results.GetResult(Resource_Directory, defaultValue = TemplatingLib.Constants.defaultResourceDirectory)
        )

    let solutionName =
        results.GetResult(Solution_Name, defaultValue = TemplatingLib.Constants.defaultSolutionName)

    let outputDirectory =
        results.GetResult(Output_Directory, defaultValue = TemplatingLib.Constants.defaultOutputDirectory)

    let result =
        workflow solutionName outputDirectory (defaultTemplates resourceDirectory)

    printfn $"Workflow: %A{result}"

    0
