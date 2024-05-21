module TemplatingLib.Types

open Errors

type ConfigType =
    | GitIgnore
    | EditorConfig
    | GlobalJson

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
      Path: ValidatedPath }

let unwrapProjectCreationInputs (inputs: ProjectCreationInputs) =
    let projectType = convertProjectTypeToString inputs.ProjectType
    let projectName = ValidName.value inputs.ProjectName

    let language = convertLanguageToString inputs.Language

    printfn
        $"unwrapping project creation inputs:\n\tProjectName: %s{projectName},\n\tProjectType: %s{projectType},\n\tLanguage: %s{language},\n\tPath: %s{inputs.Path}..."

    (projectName, projectType, language, inputs.Path)
