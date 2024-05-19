namespace TemplatingLib

open System.Diagnostics
open System.IO
open FsToolkit.ErrorHandling

module Io =

    type ApplicationError =
        | InvalidProjectName of string
        | UnknownProjectType of string
        | UnknownLanguage of string
        | CantCreateOutputDirectory of string
        | CantCreateDotnetProject of string

    type ValidatedPath = ValidatedPath of string

    type ProjectType = | ClassLib

    let tryConvertToProjectType (s: string) =
        match s with
        | "classlib" -> ClassLib |> Ok
        | e -> Error(UnknownProjectType e)

    let convertProjectTypeToString =
        function
        | ClassLib -> "classlib"

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

    type ValidProjectName = private ValidProjectName of string

    module ValidProjectName =
        let create (name: string) =
            if name.Length > 0 then
                ValidProjectName name |> Ok
            else
                Error(InvalidProjectName "Project name must not be empty")

        let value (ValidProjectName name) = name


    // type ProjectCreationInputs = ValidProjectName * ProjectType * Language * ValidatedPath
    type ProjectCreationInputs =
        { ProjectName: ValidProjectName
          ProjectType: ProjectType
          Language: Language
          Path: ValidatedPath }

    let unwrapProjectCreationInputs (inputs: ProjectCreationInputs) =
        let projectType = convertProjectTypeToString inputs.ProjectType
        let projectName = ValidProjectName.value inputs.ProjectName
        let (ValidatedPath path) = inputs.Path
        let language = convertLanguageToString inputs.Language
        (projectName, projectType, language, path)

    let tryToCreateOutputDirectory (unvalidatedPath: string) : Result<ValidatedPath, ApplicationError> =
        try
            let path = Path.GetFullPath(unvalidatedPath)
            let output = Directory.CreateDirectory(path)
            output.FullName |> ValidatedPath |> Ok
        with e ->
            Error(CantCreateOutputDirectory e.Message)

    let tryToCreateDotnetProject (projectCreationInputs: ProjectCreationInputs) : Result<unit, ApplicationError> =
        try
            let name, projectType, lang, path =
                unwrapProjectCreationInputs projectCreationInputs

            Process.Start("dotnet", $"new %s{projectType} --name %s{name} --output %s{path} --language %s{lang}")
            |> ignore
            |> Ok
        with e ->
            Error(CantCreateDotnetProject e.Message)

    let createDotnetProject
        (rawProjectType: string)
        (rawName: string)
        (rawPath: string)
        (rawLanguage: string)
        : Result<unit, ApplicationError list> =

        let tryValidatingInputs =
            validation {
                let! projectName = ValidProjectName.create rawName
                and! projectType = tryConvertToProjectType rawProjectType
                and! language = tryConvertToLanguage rawLanguage
                and! path = tryToCreateOutputDirectory rawPath

                return
                    { ProjectName = projectName
                      ProjectType = projectType
                      Language = language
                      Path = path }
            }

        match tryValidatingInputs with
        | Error e -> Error e
        | Ok inputs -> inputs |> tryToCreateDotnetProject |> Result.mapError (fun e -> [ e ])
