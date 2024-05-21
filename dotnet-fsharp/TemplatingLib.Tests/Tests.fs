module Tests

open System.Diagnostics
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

let validProjectTypeClassLib = "classlib"
let validProjectTypeXUnit = "xunit"
let validProjectName = "Foo"
let validPath = "~/tmp/foo"
let validLanguageCSharp = "c#"
let validLanguageFSharp = "c#"

let forceOverwrite = true

[<Theory>]
[<InlineData("classlib", "c#")>]
[<InlineData("xunit", "f#")>]
let ``create project - happy case w/ linux`` validProjectType validLanguage =
    let actual =
        createDotnetProject validProjectType validProjectName validPath validLanguage forceOverwrite

    actual |> isOk

[<Fact>]
let ``create project - invalid project type`` () =
    let invalidProjectType = "invalidProjectType"

    let actual =
        createDotnetProject invalidProjectType validProjectName validPath validLanguageCSharp forceOverwrite

    let expected = (UnknownProjectType invalidProjectType)
    actual |> hasError expected

[<Fact>]
let ``create project - invalid project name`` () =
    let invalidProjectName = ""

    let actual =
        createDotnetProject validProjectTypeClassLib invalidProjectName validPath validLanguageCSharp forceOverwrite

    let expected = (InvalidName "Name must not be empty")
    actual |> hasError expected

[<Fact>]
let ``create project - invalid path / empty`` () =
    let invalidPath = ""

    let actual =
        createDotnetProject validProjectTypeClassLib validProjectName invalidPath validLanguageCSharp forceOverwrite

    let expected =
        (CantCreateOutputDirectory "The value cannot be an empty string. (Parameter 'path')")

    actual |> hasError expected

[<Fact>]
let ``create project - invalid path / access denied - linux`` () =
    let invalidPath = "/doesnotexist"

    let actual =
        createDotnetProject validProjectTypeClassLib validProjectName invalidPath validLanguageCSharp forceOverwrite

    let expected =
        (CantCreateOutputDirectory $"Access to the path '%s{invalidPath}' is denied.")

    actual |> hasError expected

[<Fact>]
let ``create project - invalid language`` () =
    let invalidLanguage = "vb.net"

    let actual =
        createDotnetProject validProjectTypeClassLib validProjectName validPath invalidLanguage forceOverwrite

    let expected = (UnknownLanguage invalidLanguage)
    actual |> hasError expected

[<Fact>]
let ``create project - all inputs invalid`` () =
    let invalidProjectType = "invalidProjectType"
    let invalidProjectName = ""
    let invalidPath = "/doesnotexist"
    let invalidLanguage = "vb.net"

    let actual =
        createDotnetProject invalidProjectType invalidProjectName invalidPath invalidLanguage forceOverwrite

    let expected =
        [ (UnknownLanguage invalidLanguage)
          (CantCreateOutputDirectory $"Access to the path '%s{invalidPath}' is denied.")
          (InvalidName "Name must not be empty")
          (UnknownProjectType invalidProjectType) ]

    actual |> hasErrors expected

[<Fact>]
let ``xml experiment 1 - single PropertyGroup present -> remove first PropertyGroup`` () =
    let xml =
        "<Project><PropertyGroup><TargetFramework>net5.0</TargetFramework></PropertyGroup></Project>"

    let doc = System.Xml.Linq.XDocument.Parse xml
    let prop = doc.Descendants("PropertyGroup") |> Seq.head
    prop.Remove()
    let actual = doc.ToString()
    let expected = "<Project />"
    test <@ actual = expected @>

[<Fact>]
let ``xml experiment 2 - multiple PropertyGroups present -> remove first PropertyGroup`` () =
    let xml =
        "<Project><PropertyGroup><TargetFramework>net5.0</TargetFramework></PropertyGroup><PropertyGroup>foo</PropertyGroup></Project>"

    let doc = System.Xml.Linq.XDocument.Parse xml
    let prop = doc.Descendants("PropertyGroup") |> Seq.head
    prop.Remove()
    let actual = doc.ToString()
    let expected = "<Project>\n  <PropertyGroup>foo</PropertyGroup>\n</Project>"
    test <@ actual = expected @>

[<Fact>]
let ``xml experiment 3 - multiple PropertyGroups present -> remove all PropertyGroups`` () =
    let xml =
        "<Project><PropertyGroup><TargetFramework>net5.0</TargetFramework></PropertyGroup><PropertyGroup>foo</PropertyGroup></Project>"

    let doc = System.Xml.Linq.XDocument.Parse xml
    doc.Descendants("PropertyGroup") |> Seq.toList |> List.iter (_.Remove())
    let actual = doc.ToString()
    let expected = "<Project />"
    test <@ actual = expected @>

[<Fact>]
let ``xml experiment 4 - multiple ItemGroups present -> remove first ItemGroup and preserve PropertyGroup`` () =
    let xml =
        "<Project><PropertyGroup><TargetFramework>net5.0</TargetFramework></PropertyGroup><ItemGroup>foo</ItemGroup><ItemGroup>bar</ItemGroup></Project>"

    let doc = System.Xml.Linq.XDocument.Parse xml
    let itemGroup1 = doc.Descendants("ItemGroup") |> Seq.head
    itemGroup1.Remove()

    let actual = doc.ToString()

    let expected =
        "<Project>\n  <PropertyGroup>\n    <TargetFramework>net5.0</TargetFramework>\n  </PropertyGroup>\n  <ItemGroup>bar</ItemGroup>\n</Project>"

    test <@ actual = expected @>


let processStart (executable: string) (arguments: string) =
    let startInfo = ProcessStartInfo(executable, arguments)
    startInfo.UseShellExecute <- false
    startInfo.RedirectStandardOutput <- true
    startInfo.RedirectStandardError <- true
    startInfo.CreateNoWindow <- true
    startInfo.RedirectStandardOutput <- true
    startInfo.RedirectStandardError <- true

    let proc = new Process()
    proc.StartInfo <- startInfo

    try
        proc.Start() |> ignore // NOTE This can throw an exception if the executable is not found

        let stdout = proc.StandardOutput.ReadToEnd()
        let stderr = proc.StandardError.ReadToEnd()

        proc.WaitForExit()

        if stderr.Length > 0 then
            Error $"%s{stderr}"
        else
            Ok $"%s{stdout}"
    with e ->
        Error
            $"Process.Start() failed. Given executable: %s{executable} - Given arguments: %s{arguments} - Error message:  %s{e.Message}"

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
        e.StartsWith "Process.Start() failed. Given executable: doesnotexist - Given arguments: foo"
        =! true

[<Fact>]
let ``process valid - invalid arguments`` () =
    let actual = processStart "dotnet" "invalid arguments"

    match actual with
    | Ok _ -> true =! false
    | Error e -> e.Contains "Could not execute" =! true

[<Fact>]
let ``process valid & arguments syntactically valid, but invalid path - fails w/ permission denied`` () =
    let actual =
        processStart "dotnet" "new classlib --name Foo --output /doesnotexist --no-restore"

    match actual with
    | Ok _ -> true =! false
    | Error e -> e.Contains "Permission denied" =! true
