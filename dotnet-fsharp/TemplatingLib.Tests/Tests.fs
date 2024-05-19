module Tests

open TemplatingLib.Io
open Xunit
open Swensen.Unquote

let isOk result =
    match result with
    | Ok _ -> true =! true
    | Error _ -> true =! false

let isError result =
    match result with
    | Ok _ -> true =! false
    | Error _ -> true =! true

let hasErrors (errorsExpected: ApplicationError list) (results: Result<unit, ApplicationError list>) =
    match results with
    | Ok _ -> true =! false
    | Error errors -> List.forall (fun e -> List.contains e errorsExpected) errors =! true

let hasError (error: ApplicationError) (results: Result<unit, ApplicationError list>) = hasErrors [ error ] results

let validProjectType = "classlib"
let validProjectName = "Foo"
let validPath = "~/tmp/foo"
let validLanguage = "c#"

[<Fact>]
let ``create project - happy case w/ linux`` () =
    let actual =
        createDotnetProject validProjectType validProjectName validPath validLanguage

    actual |> isOk

[<Fact>]
let ``create project - invalid project type`` () =
    let invalidProjectType = "invalidProjectType"

    let actual =
        createDotnetProject invalidProjectType validProjectName validPath validLanguage

    let expected = (UnknownProjectType invalidProjectType)
    actual |> hasError expected

[<Fact>]
let ``create project - invalid project name`` () =
    let invalidProjectName = ""

    let actual =
        createDotnetProject validProjectType invalidProjectName validPath validLanguage

    let expected = (InvalidProjectName "Project name must not be empty")
    actual |> hasError expected

[<Fact>]
let ``create project - invalid path / empty`` () =
    let invalidPath = ""

    let actual =
        createDotnetProject validProjectType validProjectName invalidPath validLanguage

    let expected =
        (CantCreateOutputDirectory "The value cannot be an empty string. (Parameter 'path')")

    actual |> hasError expected

[<Fact>]
let ``create project - invalid path / access denied - linux`` () =
    let invalidPath = "/doesnotexist"

    let actual =
        createDotnetProject validProjectType validProjectName invalidPath validLanguage

    let expected =
        (CantCreateOutputDirectory $"Access to the path '%s{invalidPath}' is denied.")

    actual |> hasError expected

[<Fact>]
let ``create project - invalid language`` () =
    let invalidLanguage = "vb.net"

    let actual =
        createDotnetProject validProjectType validProjectName validPath invalidLanguage

    let expected = (UnknownLanguage invalidLanguage)
    actual |> hasError expected

[<Fact>]
let ``create project - all inputs invalid`` () =
    let invalidProjectType = "invalidProjectType"
    let invalidProjectName = ""
    let invalidPath = "/doesnotexist"
    let invalidLanguage = "vb.net"

    let actual =
        createDotnetProject invalidProjectType invalidProjectName invalidPath invalidLanguage

    let expected =
        [ (UnknownLanguage invalidLanguage)
          (CantCreateOutputDirectory $"Access to the path '%s{invalidPath}' is denied.")
          (InvalidProjectName "Project name must not be empty")
          (UnknownProjectType invalidProjectType) ]

    actual |> hasErrors expected
