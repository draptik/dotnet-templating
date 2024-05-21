﻿namespace TemplatingLib

open System
open System.Diagnostics
open System.IO
open FsToolkit.ErrorHandling
open Errors
open Types

module Io =

    let processStart (fileName: string) (arguments: string) =
        let startInfo = ProcessStartInfo(fileName, arguments)
        startInfo.UseShellExecute <- false
        startInfo.RedirectStandardOutput <- true
        startInfo.RedirectStandardError <- true
        startInfo.CreateNoWindow <- true
        startInfo.RedirectStandardOutput <- true
        startInfo.RedirectStandardError <- true
        let proc = new Process()
        proc.StartInfo <- startInfo
        proc.Start() |> ignore

        let stderr = proc.StandardError.ReadToEnd()

        if stderr.Length > 0 then
            printfn $"stderr: %s{stderr}"
        else
            printfn "no errors"

        let stdout = proc.StandardOutput.ReadToEnd()
        printfn $"stdout: %s{stdout}"

        proc.WaitForExit()

    let startDotnetProcess (arguments: string) = processStart "dotnet" arguments


    let tryToCreateOutputDirectory (unvalidatedPath: string) : Result<ValidatedPath, ApplicationError> =
        printfn $"Creating output directory: %s{unvalidatedPath}..."

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
        : Result<ValidatedPath, ApplicationError> =
        try
            let name, projectType, lang, path =
                unwrapProjectCreationInputs projectCreationInputs

            printfn $"Creating project: %s{name}..."
            startDotnetProcess $"new %s{projectType} --name %s{name} --output %s{path} --language %s{lang} --no-restore"
            printfn $"Created project: %s{name}..."
            Ok(Path.Combine(path, name) |> ValidatedPath)
        with e ->
            Error(CantCreateDotnetProject e.Message)

    let createDotnetProject
        (rawProjectType: string)
        (rawName: string)
        (rawPath: string)
        (rawLanguage: string)
        : Result<ValidatedPath, ApplicationError list> =

        let tryValidatingInputs =
            validation {
                let! projectName = ValidName.create rawName
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
                startDotnetProcess
                    $"new %s{config} --sdk-version %s{latestLts} --roll-forward %s{rollForwardPolicy} --output %s{path}"
                |> Ok
            | _ -> startDotnetProcess $"new %s{config} --output %s{path}" |> Ok
        with e ->
            Error(CantCreateConfigFile(e.Message, configTypeToString configType))

    let tryCopy (source: string) (target: string) =
        try
            File.Copy(source, target, overwrite = true) |> Ok
        with e ->
            Error(CantCopyResource(source, target, e.Message))

    let tryCreateSolution (solutionName: string) (path: string) =
        try
            startDotnetProcess $"new sln --name %s{solutionName} --output %s{path}" |> Ok
        with e ->
            Error(CantCreateSolution e.Message)

    let tryAddProjectToSolution (solutionPath: ValidatedPath) (projectPath: ValidatedPath) =
        printfn $"Adding project: %s{projectPath} to solution %s{solutionPath}..."

        try
            startDotnetProcess $"sln %s{solutionPath} add %s{projectPath}" |> Ok
        with e ->
            Error(CantCreateDependency e.Message)

    let tryAddProjectDependency (addTo: ValidatedPath) (validDependentPath: ValidatedPath) =
        let project = addTo
        let dependsOn = validDependentPath
        printfn $"Creating dependency: %s{project} depends on %s{dependsOn} ..."

        try
            startDotnetProcess $"add %s{project} reference %s{dependsOn}" |> Ok
        with e ->
            Error(CantCreateDependency e.Message)

    let languageToConfigExtension =
        function
        | CSharp -> "csproj"
        | FSharp -> "fsproj"

    let removeFromXml (xml: string) (element: string) =
        let doc = System.Xml.Linq.XDocument.Parse xml
        let head = doc.Descendants(element) |> Seq.head
        head.Remove()
        doc.ToString()

    let removeFirstPropertyGroupFromXml (xml: string) = removeFromXml xml "PropertyGroup"

    let removeFirstItemGroupFromXml (xml: string) = removeFromXml xml "ItemGroup"

    let tryReplacePropertyGroupFromFile (language: Language) (path: ValidatedPath) =
        let ext = languageToConfigExtension language
        let file = $"{path}.{ext}"

        try
            let xmlString = File.ReadAllText file
            let newXml = removeFirstPropertyGroupFromXml xmlString
            File.WriteAllText(file, newXml) |> Ok
        with e ->
            Error(CantRemovePropertyGroup e.Message)

    let tryReplaceItemGroupFromFile (language: Language) (path: ValidatedPath) =
        let ext = languageToConfigExtension language
        let file = $"{path}.{ext}"

        try
            let xmlString = File.ReadAllText file
            let newXml = removeFirstItemGroupFromXml xmlString
            File.WriteAllText(file, newXml) |> Ok
        with e ->
            Error(CantRemoveItemGroup e.Message)