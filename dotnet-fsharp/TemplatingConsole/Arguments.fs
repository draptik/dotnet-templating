module Arguments

open System.IO
open Argu

type CliArguments =
    | [<AltCommandLine("-n"); Unique>] Solution_Name of name: string
    | [<AltCommandLine("-o"); Unique>] Output_Directory of path: string
    | [<AltCommandLine("-r"); Unique>] Resource_Directory of path: string
    | [<AltCommandLine("-f"); Unique>] Force of bool

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Solution_Name _ -> "The name of the solution to create"
            | Output_Directory _ -> "The directory where the project will be created"
            | Resource_Directory _ -> "The directory where the resources are located"
            | Force _ -> "Force overwrite of existing files"

let getResourceDirectory (results: ParseResults<CliArguments>) =
    Path.GetFullPath(
        results.GetResult(Resource_Directory, defaultValue = TemplatingLib.Constants.defaultResourceDirectory)
    )

let getSolutionName (results: ParseResults<CliArguments>) =
    results.GetResult(Solution_Name, defaultValue = TemplatingLib.Constants.defaultSolutionName)

let getOutputDirectory (results: ParseResults<CliArguments>) =
    results.GetResult(Output_Directory, defaultValue = TemplatingLib.Constants.defaultOutputDirectory)
