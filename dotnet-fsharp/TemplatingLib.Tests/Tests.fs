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
let ``xml experiment 1 - single PropertyGroup present -> remove first PropertyGroup`` () =
    let xml =
        "<Project><PropertyGroup><TargetFramework>net5.0</TargetFramework></PropertyGroup></Project>"

    let doc = System.Xml.Linq.XDocument.Parse xml
    let prop = doc.Descendants("PropertyGroup") |> Seq.head
    prop.Remove()
    let actual = doc.ToString()
    let expected = "<Project />"
    actual =! expected

[<Fact>]
let ``xml experiment 2 - multiple PropertyGroups present -> remove first PropertyGroup`` () =
    let xml =
        "<Project><PropertyGroup><TargetFramework>net5.0</TargetFramework></PropertyGroup><PropertyGroup>foo</PropertyGroup></Project>"

    let doc = System.Xml.Linq.XDocument.Parse xml
    let prop = doc.Descendants("PropertyGroup") |> Seq.head
    prop.Remove()
    let actual = doc.ToString()
    let expected = "<Project>\n  <PropertyGroup>foo</PropertyGroup>\n</Project>"
    actual =! expected

[<Fact>]
let ``xml experiment 3 - multiple PropertyGroups present -> remove all PropertyGroups`` () =
    let xml =
        "<Project><PropertyGroup><TargetFramework>net5.0</TargetFramework></PropertyGroup><PropertyGroup>foo</PropertyGroup></Project>"

    let doc = System.Xml.Linq.XDocument.Parse xml
    doc.Descendants("PropertyGroup") |> Seq.toList |> List.iter (_.Remove())
    let actual = doc.ToString()
    let expected = "<Project />"
    actual =! expected

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

    actual =! expected

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
        | ProcessStartError _ -> true =! true
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
        workflow validSolutionName validOutputDirectory (defaultTemplates resourceDirectory)

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
        workflow invalidSolutionName validOutputDirectory (defaultTemplates resourceDirectory)

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
    let invalidOutputDirectory = "/test"

    let actual =
        workflow validSolutionName invalidOutputDirectory (defaultTemplates resourceDirectory)

    match actual with
    | Ok _ -> true =! false
    | Error e ->
        match e with
        | CantCreateOutputDirectory _ -> true =! true
        | _ -> true =! false
