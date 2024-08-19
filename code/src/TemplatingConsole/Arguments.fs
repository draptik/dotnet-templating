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
            | Language _ -> "The language used. Options: csharp, fsharp (defaults to csharp)"
            | Resource_Directory _ ->
                "The directory where the resources are located (defaults to location of executable + './resources')"
            | Force _ -> "Force overwrite of existing files (defaults to true)"

let getResourceDirectory (results: ParseResults<CliArguments>) resourceBaseDir =
    Path.GetFullPath(
        results.GetResult(
            Resource_Directory,
            defaultValue = Path.Combine(resourceBaseDir, TemplatingLib.Constants.defaultResourceDirectoryName)
        )
    )

let getSolutionName (results: ParseResults<CliArguments>) =
    results.GetResult(Solution_Name, defaultValue = TemplatingLib.Constants.defaultSolutionName)

let getOutputDirectory (results: ParseResults<CliArguments>) =
    results.GetResult(Output_Directory, defaultValue = TemplatingLib.Constants.defaultOutputDirectory)

let getLanguage (results: ParseResults<CliArguments>) =
    results.GetResult(Language, defaultValue = Language.Csharp)