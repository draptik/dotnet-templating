namespace TemplatingLib

open System
open System.Diagnostics
open System.IO
open FsToolkit.ErrorHandling
open Errors
open Types

module Io =

    let processStart (executable: string) (arguments: string) : Result<string, ApplicationError> =
        let startInfo = ProcessStartInfo(executable, arguments)
        startInfo.UseShellExecute <- false
        startInfo.RedirectStandardOutput <- true
        startInfo.RedirectStandardError <- true
        startInfo.CreateNoWindow <- true
        startInfo.RedirectStandardOutput <- true
        startInfo.RedirectStandardError <- true

        let proc = new Process()
        proc.StartInfo <- startInfo

        try
            proc.Start() |> ignore // NOTE This can throw an exception if the executable is not found

            let stdout = proc.StandardOutput.ReadToEnd()
            let stderr = proc.StandardError.ReadToEnd()

            proc.WaitForExit()

            if stderr.Length > 0 then
                Error(DotNetProcessError $"%s{stderr}")
            else
                Ok $"%s{stdout}"
        with e ->
            Error(
                ProcessStartError
                    $"Process.Start() failed. Given executable: %s{executable} - Given arguments: %s{arguments} - Error message:  %s{e.Message}"
            )

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
        let name, projectType, lang, path, forceOverwrite =
            unwrapProjectCreationInputs projectCreationInputs

        let mutable args =
            $"new %s{projectType} --name %s{name} --output %s{path} --language %s{lang} --no-restore"

        if forceOverwrite then
            args <- args + " --force"

        startDotnetProcess args
        |> Result.mapError id
        |> Result.map (fun _ -> Path.Combine(path, name) |> ValidatedPath)

    let createDotnetProject
        (rawProjectType: string)
        (rawName: string)
        (rawPath: string)
        (rawLanguage: string)
        (forceOverwrite: bool)
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
                      Path = path
                      ForceOverWrite = forceOverwrite }
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

    let tryCreateConfigFile (configType: ConfigType) (path: string) : Result<string, ApplicationError> =
        let latestLts = "8.0.0"
        let rollForwardPolicy = "latestMajor"
        let config = configTypeToString configType

        match configType with
        | GlobalJson ->
            startDotnetProcess
                $"new %s{config} --sdk-version %s{latestLts} --roll-forward %s{rollForwardPolicy} --output %s{path}"
        | _ -> startDotnetProcess $"new %s{config} --output %s{path}"

    let tryCopy (source: string) (target: string) =
        try
            File.Copy(source, target, overwrite = true) |> Ok
        with e ->
            Error(CantCopyResource(source, target, e.Message))

    let tryCreateSolution (solutionName: string) (path: string) =
        startDotnetProcess $"new sln --name %s{solutionName} --output %s{path}" |> Ok

    let tryAddProjectToSolution (solutionPath: ValidatedPath) (projectPath: ValidatedPath) =
        printfn $"Adding project: %s{projectPath} to solution %s{solutionPath}..."
        startDotnetProcess $"sln %s{solutionPath} add %s{projectPath}" |> Ok

    let tryAddProjectDependency (addTo: ValidatedPath) (validDependentPath: ValidatedPath) =
        let project = addTo
        let dependsOn = validDependentPath
        printfn $"Creating dependency: %s{project} depends on %s{dependsOn} ..."
        startDotnetProcess $"add %s{project} reference %s{dependsOn}" |> Ok

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

    let workflow solutionName outputDirectory templates =
        let (rootBuildPropsTemplate,
             srcDirBuildPropsTemplate,
             testsDirBuildPropsTemplate,
             rootPackagesTemplate,
             gitAttributesTemplate,
             forceOverWrite) =
            templates

        let selectedLanguage = Language.CSharp

        result {
            let! validSolutionName = ValidName.create solutionName

            let! outputPath = tryToCreateOutputDirectory outputDirectory

            let! _ = tryToCreateOutputDirectory (Path.Combine(outputPath, Constants.src))
            let! _ = tryToCreateOutputDirectory (Path.Combine(outputPath, Constants.tests))

            let! _ = tryCreateConfigFile GitIgnore outputPath
            let! _ = tryCreateConfigFile EditorConfig outputPath
            let! _ = tryCreateConfigFile GlobalJson outputPath

            let! _ = tryCopy rootBuildPropsTemplate (Path.Combine(outputPath, Constants.DirectoryBuildProps))

            let! _ = tryCopy rootPackagesTemplate (Path.Combine(outputPath, Constants.DirectoryPackagesProps))

            let! _ = tryCopy gitAttributesTemplate (Path.Combine(outputPath, Constants.gitAttributes))

            let! _ =
                tryCopy
                    srcDirBuildPropsTemplate
                    (Path.Combine(outputPath, Constants.src, Constants.DirectoryBuildProps))

            let! _ =
                tryCopy
                    testsDirBuildPropsTemplate
                    (Path.Combine(outputPath, Constants.tests, Constants.DirectoryBuildProps))

            printfn $"Creating solution: %s{solutionName}"
            let! _ = tryCreateSolution solutionName outputPath

            let lib = ValidName.appendTo validSolutionName Constants.defaultLibName
            let libName = ValidName.value lib
            let libPath = ValidatedPath(Path.Combine(outputPath, Constants.src, libName))

            printfn $"Creating lib project: %s{libPath}"

            let! libProject =
                tryToCreateDotnetProjectWithoutRestore
                    { ProjectCreationInputs.ProjectType = ProjectType.ClassLib
                      ProjectCreationInputs.ProjectName = lib
                      ProjectCreationInputs.Language = selectedLanguage
                      ProjectCreationInputs.Path = libPath
                      ProjectCreationInputs.ForceOverWrite = forceOverWrite }

            printfn "Patching lib project files 1/1..."
            let! _ = libProject |> tryReplacePropertyGroupFromFile selectedLanguage

            let test = ValidName.appendTo validSolutionName Constants.defaultLibTestName
            let testName = ValidName.value test
            let testPath = ValidatedPath(Path.Combine(outputPath, Constants.tests, testName))

            printfn $"Creating test project: %s{testName}"

            let! testProject =
                tryToCreateDotnetProjectWithoutRestore
                    { ProjectCreationInputs.ProjectType = ProjectType.XUnit
                      ProjectCreationInputs.ProjectName = test
                      ProjectCreationInputs.Language = selectedLanguage
                      ProjectCreationInputs.Path = testPath
                      ProjectCreationInputs.ForceOverWrite = forceOverWrite }

            let! _ = tryAddProjectDependency testPath libPath

            printfn "Patching test project files 1/2..."
            let! _ = testProject |> tryReplacePropertyGroupFromFile selectedLanguage
            printfn "Patching test project files 2/2..."
            let! _ = testProject |> tryReplaceItemGroupFromFile selectedLanguage

            let! _ = tryAddProjectToSolution outputPath libPath
            let! _ = tryAddProjectToSolution outputPath testPath

            printfn "Done"
            return ()
        }
