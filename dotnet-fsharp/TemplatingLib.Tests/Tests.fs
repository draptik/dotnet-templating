module Tests

open System.IO
open Xunit
open Swensen.Unquote

open TemplatingLib.Io
open TemplatingLib.Errors
open TemplatingLib.Types

let isOk result =
    match result with
    | Ok _ -> true =! true
    | Error _ -> true =! false

let isError result =
    match result with
    | Ok _ -> true =! false
    | Error _ -> true =! true

let hasErrors (errorsExpected: ApplicationError list) (results: Result<ValidatedPath, ApplicationError list>) =
    match results with
    | Ok _ -> true =! false
    | Error errors -> List.forall (fun e -> List.contains e errorsExpected) errors =! true

let hasError (error: ApplicationError) (results: Result<ValidatedPath, ApplicationError list>) =
    hasErrors [ error ] results

let validateProjectName name =
    let x = ValidName.create name

    match x with
    | Ok v -> v
    | Error _ -> failwith $"could not validate project name '{name}'"

[<Fact>]
let ``create project - happy case w/ linux`` () =
    let inputs =
        { ProjectName = validateProjectName "Foo"
          ProjectType = ClassLib
          Language = CSharp
          Path = ValidatedPath "~/tmp/foo"
          ForceOverWrite = true }

    let actual = tryToCreateDotnetProjectWithoutRestore inputs

    actual |> isOk

[<Fact>]
let ``process valid - dotnet --info`` () =
    let actual = processStart "dotnet" "--info"

    match actual with
    | Ok s -> s.StartsWith ".NET SDK" =! true
    | Error _ -> true =! false

[<Fact>]
let ``process invalid`` () =
    let actual = processStart "doesnotexist" "foo"

    match actual with
    | Ok _ -> true =! false
    | Error e ->
        match e with
        | CantStartProcess _ -> true =! true
        | _ -> true =! false

[<Fact>]
let ``process valid - invalid arguments`` () =
    let actual = processStart "dotnet" "invalid arguments"

    match actual with
    | Ok _ -> true =! false
    | Error e ->
        match e with
        | DotNetProcessError _ -> true =! true
        | _ -> true =! false

[<Fact>]
let ``workflow - happy case`` () =
    let resourceDirectory =
        Path.Combine("../../..", TemplatingLib.Constants.defaultResourceDirectory)

    let validSolutionName = TemplatingLib.Constants.defaultSolutionName
    let validOutputDirectory = TemplatingLib.Constants.defaultOutputDirectory

    let actual =
        workflow validSolutionName validOutputDirectory (getDefaultTemplates resourceDirectory)

    match actual with
    | Ok _ -> true =! true
    | Error _ -> true =! false

[<Fact>]
let ``workflow - invalid solution name`` () =
    let resourceDirectory =
        Path.Combine("../../..", TemplatingLib.Constants.defaultResourceDirectory)

    let invalidSolutionName = ""
    let validOutputDirectory = TemplatingLib.Constants.defaultOutputDirectory

    let actual =
        workflow invalidSolutionName validOutputDirectory (getDefaultTemplates resourceDirectory)

    match actual with
    | Ok _ -> true =! false
    | Error e ->
        match e with
        | InvalidName _ -> true =! true
        | _ -> true =! false

[<Fact>]
let ``workflow - invalid output path`` () =
    let resourceDirectory =
        Path.Combine("../../..", TemplatingLib.Constants.defaultResourceDirectory)

    let validSolutionName = TemplatingLib.Constants.defaultSolutionName
    let invalidOutputDirectory = ""

    let actual =
        workflow validSolutionName invalidOutputDirectory (getDefaultTemplates resourceDirectory)

    match actual with
    | Ok _ -> true =! false
    | Error e ->
        match e with
        | CantCreateOutputDirectory _ -> true =! true
        | _ -> true =! false
