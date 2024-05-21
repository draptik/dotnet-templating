namespace TemplatingLib

open System
open System.Diagnostics
open System.IO
open FsToolkit.ErrorHandling
open XmlLib

module Io =

    type ConfigType =
        | GitIgnore
        | EditorConfig
        | GlobalJson

    type ApplicationError =
        | InvalidName of string
        | UnknownProjectType of string
        | UnknownLanguage of string
        | CantCreateOutputDirectory of string
        | CantCreateDotnetProject of string
        | CantCreateConfigFile of string * ConfigType
        | CantCopyResource of src: string * target: string * error: string
        | CantCreateSolution of string
        | CantCreateDependency of string
        | CantReplacePropertyGroup of string

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

    type ValidName = private ValidName of string

    module ValidName =
        let create (name: string) =
            if name.Length > 0 then
                ValidName name |> Ok
            else
                Error(InvalidName "Name must not be empty")

        let value (ValidName name) = name

        let appendTo (ValidName name) (s: string) = $"{name}.{s}" |> ValidName

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

    type ProjectCreationInputs =
        { ProjectName: ValidName
          ProjectType: ProjectType
          Language: Language
          Path: ValidatedPath }

    let unwrapProjectCreationInputs (inputs: ProjectCreationInputs) =
        let projectType = convertProjectTypeToString inputs.ProjectType
        let projectName = ValidName.value inputs.ProjectName

        let path =
            match inputs.Path with
            | ValidatedPath p -> p

        let language = convertLanguageToString inputs.Language

        printfn
            $"unwrapping project creation inputs:\n\tProjectName: %s{projectName},\n\tProjectType: %s{projectType},\n\tLanguage: %s{language},\n\tPath: %s{path}..."

        (projectName, projectType, language, path)

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
            Error(CantCreateConfigFile(e.Message, configType))

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
        let (ValidatedPath solution) = solutionPath
        let (ValidatedPath project) = projectPath
        printfn $"Adding project: %s{project} to solution %s{solution}..."

        try
            startDotnetProcess $"sln %s{solution} add %s{project}" |> Ok
        with e ->
            Error(CantCreateDependency e.Message)

    let tryAddProjectDependency (addTo: ValidatedPath) (validDependentPath: ValidatedPath) =
        let (ValidatedPath project) = addTo
        let (ValidatedPath dependsOn) = validDependentPath
        printfn $"Creating dependency: %s{project} depends on %s{dependsOn} ..."

        try
            startDotnetProcess $"add %s{project} reference %s{dependsOn}" |> Ok
        with e ->
            Error(CantCreateDependency e.Message)

    let tryReplacePropertyGroupFromFile (path: ValidatedPath) =
        try
            let (ValidatedPath p) = path
            let file = $"{p}.csproj"
            let xmlString = File.ReadAllText file
            let newXml = removeFirstPropertyGroupFromXml xmlString
            File.WriteAllText(file, newXml) |> Ok
        with e ->
            Error(CantReplacePropertyGroup e.Message)

    let tryReplaceItemGroupFromFile (xmlFile: ValidatedPath) =
        try
            let (ValidatedPath xml) = xmlFile
            let file = $"{xml}.csproj"
            let xmlString = File.ReadAllText file
            let newXml = removeFirstItemGroupFromXml xmlString
            File.WriteAllText(file, newXml) |> Ok
        with e ->
            Error(CantReplacePropertyGroup e.Message)
