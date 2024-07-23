module TemplatingConsole

open System
open Argu

open Arguments
open TemplatingLib
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

// simple mapping from "Argument.Language" to "Domain.Language"
// There will only ever be these 2 (c#, f#).
// Famous last words
let mapLang (l: Arguments.Language) =
      match l with
      | Csharp -> Types.Language.CSharp
      | Fsharp -> Types.Language.FSharp
      
[<EntryPoint>]
let main argv =
    let results = parser.ParseCommandLine argv
    let sln = getSolutionName results
    let outDir = getOutputDirectory results
    let resDir = getResourceDirectory results
    let templates = getDefaultTemplates resDir
    let language = getLanguage results |> mapLang
    
    let result = workflow sln outDir language templates
    
    printfn $"Workflow: %A{result}"
    0
