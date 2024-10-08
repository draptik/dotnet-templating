[<RequireQualifiedAccess>]
module TemplatingLib.Constants

open Microsoft.FSharp.Core


[<Literal>]
let defaultResourceDirectoryName = "resources"

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
let editorConfig = ".editorconfig"

[<Literal>]
let defaultLibName = "MyLib"

[<Literal>]
let defaultLibTestName = "MyLib.Tests"

[<Literal>]
let latestLts = "8.0.0"

[<Literal>]
let rollForwardPolicy = "latestMajor"