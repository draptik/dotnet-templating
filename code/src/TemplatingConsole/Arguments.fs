module Arguments

open System.IO
open Argu

type Language =
    | Csharp
    | Fsharp

type CliArguments =
    | [<AltCommandLine("-n"); Unique; Mandatory>] Solution_Name of name: string
    | [<AltCommandLine("-o"); Unique; Mandatory>] Output_Directory of path: string
    | [<AltCommandLine("-l"); Unique>] Language of Language
    | [<AltCommandLine("-r"); Unique>] Resource_Directory of path: string
    | [<AltCommandLine("-f"); Unique>] Force of bool

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Solution_Name _ -> "The name of the solution to create"
            | Output_Directory _ -> "The directory where the project will be created"
            | Language _ -> "The language used. Options: c#, f# (defaults to c#)"
            | Resource_Directory _ -> "The directory where the resources are located (defaults to 'resources')"
            | Force _ -> "Force overwrite of existing files (defaults to true)"

// TODO The default path must include the path to the executable
// Example: Calling the exe from another folder than the folder the exe is located in will currently try to get
// the resources from `./resources` which will fail.
let getResourceDirectory (results: ParseResults<CliArguments>) =
    Path.GetFullPath(
        results.GetResult(Resource_Directory, defaultValue = TemplatingLib.Constants.defaultResourceDirectory)
    )

let getSolutionName (results: ParseResults<CliArguments>) =
    results.GetResult(Solution_Name, defaultValue = TemplatingLib.Constants.defaultSolutionName)

let getOutputDirectory (results: ParseResults<CliArguments>) =
    results.GetResult(Output_Directory, defaultValue = TemplatingLib.Constants.defaultOutputDirectory)

let getLanguage (results: ParseResults<CliArguments>) =
    results.GetResult(Language, defaultValue = Language.Csharp)