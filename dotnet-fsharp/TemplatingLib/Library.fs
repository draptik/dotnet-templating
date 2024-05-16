namespace TemplatingLib

open System.Diagnostics
open System.IO

module Io =

    type ApplicationError =
        | CantCreateOutputDirectory of string
        | CantCreateDotnetProject of string

    type ValidatedPath = ValidatedPath of string

    let tryToCreateOutputDirectory (unvalidatedPath: string) : Result<ValidatedPath, ApplicationError> =
        try
            let path = Path.GetFullPath(unvalidatedPath)
            let output = Directory.CreateDirectory(path)
            output.FullName |> ValidatedPath |> Ok
        with e ->
            Error(CantCreateOutputDirectory e.Message)

    let tryToCreateDotnetProject
        (projectType: string)
        (name: string)
        (validatedPath: ValidatedPath)
        (language: string)
        : Result<unit, ApplicationError> =
        try
            let (ValidatedPath path) = validatedPath
            let pt = projectType.ToString().ToLower()

            Process.Start("dotnet", $"new %s{pt} --name %s{name} --output %s{path} --language %s{language}")
            |> ignore
            |> Ok
        with e ->
            Error(CantCreateDotnetProject e.Message)

    let createDotnetProject
        (projectType: string)
        (name: string)
        (path: string)
        (language: string)
        : Result<unit, ApplicationError> =
        path
        |> tryToCreateOutputDirectory
        |> Result.bind (fun outputDirectory -> tryToCreateDotnetProject projectType name outputDirectory language)
