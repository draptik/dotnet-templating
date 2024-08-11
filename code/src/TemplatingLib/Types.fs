module TemplatingLib.Types

open System
open System.IO
open Errors

type ConfigType =
    | GitIgnore
    | EditorConfig
    | GlobalJson

let configTypeToString =
    function
    | GitIgnore -> "gitignore"
    | EditorConfig -> "editorconfig"
    | GlobalJson -> "globaljson"

type ValidatedPath = string

type ProjectType =
    | ClassLib
    | XUnit

let tryConvertToProjectType (s: string) =
    match s with
    | "classlib" -> ClassLib |> Ok
    | "xunit" -> XUnit |> Ok
    | e -> Error(UnknownProjectType e)

let convertProjectTypeToString =
    function
    | ClassLib -> "classlib"
    | XUnit -> "xunit"

type Language =
    | CSharp
    | FSharp

let tryConvertToLanguage (s: string) =
    match s with
    | "c#" -> CSharp |> Ok
    | "f#" -> FSharp |> Ok
    | e -> Error(UnknownLanguage e)

let convertLanguageToString =
    function
    | CSharp -> "c#"
    | FSharp -> "f#"

let languageToConfigExtension =
    function
    | CSharp -> "csproj"
    | FSharp -> "fsproj"

type ValidName = private ValidName of string

module ValidName =
    let create (name: string) =
        if name.Length > 0 then
            ValidName name |> Ok
        else
            Error(InvalidName "Name must not be empty")

    let value (ValidName name) = name

    let appendTo (ValidName name) (s: string) = $"{name}.{s}" |> ValidName

type ProjectCreationInputs =
    { ProjectName: ValidName
      ProjectType: ProjectType
      Language: Language
      Path: ValidatedPath
      ForceOverWrite: bool }

let sanitizePath (unvalidatedPath: string) =
    if OperatingSystem.IsWindows() then
        // Not really sure what to do about windows paths?
        // Deal with it later...
        unvalidatedPath
    else
        // dotnet can't handle linux '~', so we need to replace it with the user's home directory
        unvalidatedPath.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))

let unwrapProjectCreationInputs (inputs: ProjectCreationInputs) =
    let projectType = convertProjectTypeToString inputs.ProjectType
    let projectName = ValidName.value inputs.ProjectName
    let language = convertLanguageToString inputs.Language
    let sanitizedPath = sanitizePath inputs.Path
    (projectName, projectType, language, sanitizedPath, inputs.ForceOverWrite)

type Templates =
    { RootBuildProps: string
      SrcDirBuildProps: string
      TestDirBuildProps: string
      RootPackagesProps: string
      GitAttributes: string
      EditorConfigFsharp: string
      ForceOverwrite: bool }

let getDefaultTemplates resourceDirectory =
    let forceOverwrite = Constants.defaultForceOverwrite

    let rootBuildPropsTemplate =
        Path.Combine(resourceDirectory, $"{Constants.DirectoryBuildProps}.template")

    let rootPackagesTemplate =
        Path.Combine(resourceDirectory, $"{Constants.DirectoryPackagesProps}.template")

    let gitAttributesTemplate =
        Path.Combine(resourceDirectory, $"{Constants.gitAttributes}.template")

    let editorConfigFsharp =
        Path.Combine(resourceDirectory, $"{Constants.editorConfig}.fsharp.template")
    
    let srcDirBuildPropsTemplate =
        Path.Combine(resourceDirectory, Constants.src, $"{Constants.DirectoryBuildProps}.template")

    let testsDirBuildPropsTemplate =
        Path.Combine(resourceDirectory, Constants.tests, $"{Constants.DirectoryBuildProps}.template")

    { RootBuildProps = rootBuildPropsTemplate
      SrcDirBuildProps = srcDirBuildPropsTemplate
      TestDirBuildProps = testsDirBuildPropsTemplate
      RootPackagesProps = rootPackagesTemplate
      GitAttributes = gitAttributesTemplate
      EditorConfigFsharp = editorConfigFsharp 
      ForceOverwrite = forceOverwrite }