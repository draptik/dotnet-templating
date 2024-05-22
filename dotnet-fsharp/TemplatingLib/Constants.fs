[<RequireQualifiedAccess>]
module TemplatingLib.Constants

open Microsoft.FSharp.Core


[<Literal>]
let defaultResourceDirectory = "../TemplatingLib/resources"

[<Literal>]
let defaultSolutionName = "Foo"

[<Literal>]
let defaultOutputDirectory = "~/tmp/foo1"

[<Literal>]
let defaultForceOverwrite = true

[<Literal>]
let src = "src"

[<Literal>]
let tests = "tests"

[<Literal>]
let DirectoryBuildProps = "Directory.Build.props"

[<Literal>]
let DirectoryPackagesProps = "Directory.Packages.props"

[<Literal>]
let gitAttributes = ".gitattributes"

[<Literal>]
let defaultLibName = "MyLib"

[<Literal>]
let defaultLibTestName = "MyLib.Tests"
