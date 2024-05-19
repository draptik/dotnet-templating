namespace TemplatingLib

open System
open System.Diagnostics
open System.IO
open FsToolkit.ErrorHandling

module Io =

    type ConfigType =
        | GitIgnore
        | EditorConfig
        | GlobalJson

    type ApplicationError =
        | InvalidProjectName of string
        | UnknownProjectType of string
        | UnknownLanguage of string
        | CantCreateOutputDirectory of string
        | CantCreateDotnetProject of string
        | CantCreateConfigFile of string * ConfigType
        | CantCopyResource of src: string * target: string * error: string
        | CantCreateSolution of string

    type ValidatedPath = ValidatedPath of string

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

    type ValidSolutionName = private ValidSolutionName of string

    module ValidSolutionName =
        let create (name: string) =
            if name.Length > 0 then
                ValidSolutionName name |> Ok
            else
                Error(InvalidProjectName "Project name must not be empty")

        let value (ValidSolutionName name) = name

        let appendTo (ValidSolutionName name) (s: string) = $"{name}.{s}" |> ValidSolutionName

    type ProjectCreationInputs =
        { ProjectName: ValidSolutionName
          ProjectType: ProjectType
          Language: Language
          Path: ValidatedPath }

    let unwrapProjectCreationInputs (inputs: ProjectCreationInputs) =
        let projectType = convertProjectTypeToString inputs.ProjectType
        let projectName = ValidSolutionName.value inputs.ProjectName
        let (ValidatedPath path) = inputs.Path
        let language = convertLanguageToString inputs.Language
        (projectName, projectType, language, path)

    let tryToCreateOutputDirectory (unvalidatedPath: string) : Result<ValidatedPath, ApplicationError> =
        try
            // dotnet can't handle linux '~', so we need to replace it with the user's home directory
            let sanitizedPath =
                unvalidatedPath.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))

            let path = Path.GetFullPath(sanitizedPath)
            let output = Directory.CreateDirectory(path)
            output.FullName |> ValidatedPath |> Ok
        with e ->
            Error(CantCreateOutputDirectory e.Message)

    let tryToCreateDotnetProjectWithoutRestore
        (projectCreationInputs: ProjectCreationInputs)
        : Result<unit, ApplicationError> =
        try
            let name, projectType, lang, path =
                unwrapProjectCreationInputs projectCreationInputs

            Process.Start(
                "dotnet",
                $"new %s{projectType} --name %s{name} --output %s{path} --language %s{lang} --no-restore"
            )
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
                let! projectName = ValidSolutionName.create rawName
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
        | Ok inputs ->
            inputs
            |> tryToCreateDotnetProjectWithoutRestore
            |> Result.mapError (fun e -> [ e ])

    let configTypeToString =
        function
        | GitIgnore -> "gitignore"
        | EditorConfig -> "editorconfig"
        | GlobalJson -> "globaljson"

    let tryCreateConfigFile (configType: ConfigType) (path: string) =
        try
            let latestLts = "8.0.0"
            let rollForwardPolicy = "latestMajor"
            let config = configTypeToString configType

            match configType with
            | GlobalJson ->
                Process.Start(
                    "dotnet",
                    $"new %s{config} --sdk-version %s{latestLts} --roll-forward %s{rollForwardPolicy} --output %s{path}"
                )
                |> ignore
                |> Ok
            | _ -> Process.Start("dotnet", $"new %s{config} --output %s{path}") |> ignore |> Ok
        with e ->
            Error(CantCreateConfigFile(e.Message, configType))

    let tryCopy (source: string) (target: string) =
        try
            File.Copy(source, target, overwrite = true) |> Ok
        with e ->
            Error(CantCopyResource(source, target, e.Message))

    let tryCreateSolution (solutionName: string) (path: string) =
        try
            Process.Start("dotnet", $"new sln --name %s{solutionName} --output %s{path}")
            |> ignore
            |> Ok
        with e ->
            Error(CantCreateSolution e.Message)
