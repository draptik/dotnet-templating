namespace TemplatingLib

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
                CantStartProcess
                    $"Process.Start() failed. Given executable: %s{executable} - Given arguments: %s{arguments} - Error message:  %s{e.Message}"
            )

    let startDotnetProcess (arguments: string) = processStart "dotnet" arguments

    let tryToCreateOutputDirectory (unvalidatedPath: string) : Result<ValidatedPath, ApplicationError> =
        printfn $"Creating output directory: %s{unvalidatedPath}..."

        try
            let sanitizedPath = sanitizePath unvalidatedPath
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

        printfn $"Creating project with args: %s{args}"

        startDotnetProcess args
        |> Result.mapError id
        |> Result.map (fun _ -> Path.Combine(path, name) |> ValidatedPath)

    let tryCreateConfigFile
        (configType: ConfigType)
        (path: string)
        (forceOverwrite: bool)
        : Result<string, ApplicationError> =
        let config = configTypeToString configType

        match configType with
        | GlobalJson ->
            let mutable args =
                $"new %s{config} --sdk-version %s{Constants.latestLts} --roll-forward %s{Constants.rollForwardPolicy} --output %s{path}"

            if forceOverwrite then
                args <- args + " --force"

            startDotnetProcess args
        | _ ->
            let mutable args = $"new %s{config} --output %s{path}"

            if forceOverwrite then
                args <- args + " --force"

            startDotnetProcess args

    let tryCopy (source: string) (target: string) =
        try
            File.Copy(source, target, overwrite = true) |> Ok
        with e ->
            Error(CantCopyResource(source, target, e.Message))

    let tryCreateSolution (solutionName: string) (path: string) =
        printfn $"Creating solution: %s{solutionName} at %s{path}..."
        startDotnetProcess $"new sln --name %s{solutionName} --output %s{path}" |> Ok

    let tryAddProjectToSolution (solutionPath: ValidatedPath) (projectPath: ValidatedPath) =
        printfn $"Adding project: %s{projectPath} to solution %s{solutionPath}..."
        startDotnetProcess $"sln %s{solutionPath} add %s{projectPath}" |> Ok

    let tryAddProjectDependency (addTo: ValidatedPath) (validDependentPath: ValidatedPath) =
        printfn $"Creating dependency: %s{addTo} depends on %s{validDependentPath} ..."
        startDotnetProcess $"add %s{addTo} reference %s{validDependentPath}" |> Ok

    let removeFromXml (xml: string) (element: string) =
        let doc = System.Xml.Linq.XDocument.Parse xml
        let head = doc.Descendants(element) |> Seq.head
        head.Remove()
        doc.ToString()

    let removeFirstPropertyGroupFromXml (xml: string) = removeFromXml xml "PropertyGroup"

    let removeFirstItemGroupFromXml (xml: string) = removeFromXml xml "ItemGroup"

    let getConfigFile (language: Language) (path: ValidatedPath) =
        let ext = languageToConfigExtension language
        $"{path}.{ext}"

    let tryRemoveFirstXmlProp language path fn errType =
        let file = getConfigFile language path

        try
            let xmlString = File.ReadAllText file
            let newXml = fn xmlString
            File.WriteAllText(file, newXml) |> Ok
        with e ->
            Error(errType e.Message)

    let tryRemovePropertyGroupFromFile (language: Language) (path: ValidatedPath) =
        tryRemoveFirstXmlProp language path removeFirstPropertyGroupFromXml CantRemovePropertyGroup

    let tryRemoveItemGroupFromFile (language: Language) (path: ValidatedPath) =
        tryRemoveFirstXmlProp language path removeFirstItemGroupFromXml CantRemoveItemGroup

    let workflow solutionName outputDirectory (templates: Templates) =

        let rootBuildPropsTemplate = templates.RootBuildProps
        let srcDirBuildPropsTemplate = templates.SrcDirBuildProps
        let testsDirBuildPropsTemplate = templates.TestDirBuildProps
        let rootPackagesTemplate = templates.RootPackagesProps
        let gitAttributesTemplate = templates.GitAttributes
        let forceOverWrite = templates.ForceOverwrite

        let selectedLanguage = Language.CSharp

        // short hands for constants
        let srcFolder = Constants.src
        let testsFolder = Constants.tests
        let directoryBuildProps = Constants.DirectoryBuildProps
        let directoryPackagesProps = Constants.DirectoryPackagesProps
        let gitAttributes = Constants.gitAttributes
        let defaultLibName = Constants.defaultLibName
        let defaultLibTestName = Constants.defaultLibTestName

        result {
            let! validSolutionName = ValidName.create solutionName
            let! outputPath = tryToCreateOutputDirectory outputDirectory

            let! _ = tryToCreateOutputDirectory (Path.Combine(outputPath, srcFolder))
            let! _ = tryToCreateOutputDirectory (Path.Combine(outputPath, testsFolder))

            let! _ = tryCreateConfigFile GitIgnore outputPath forceOverWrite
            let! _ = tryCreateConfigFile EditorConfig outputPath forceOverWrite
            let! _ = tryCreateConfigFile GlobalJson outputPath forceOverWrite

            let! _ = tryCopy rootBuildPropsTemplate (Path.Combine(outputPath, directoryBuildProps))
            let! _ = tryCopy rootPackagesTemplate (Path.Combine(outputPath, directoryPackagesProps))
            let! _ = tryCopy gitAttributesTemplate (Path.Combine(outputPath, gitAttributes))
            let! _ = tryCopy srcDirBuildPropsTemplate (Path.Combine(outputPath, srcFolder, directoryBuildProps))
            let! _ = tryCopy testsDirBuildPropsTemplate (Path.Combine(outputPath, testsFolder, directoryBuildProps))

            let! _ = tryCreateSolution solutionName outputPath

            let libProjectName = ValidName.appendTo validSolutionName defaultLibName

            let libPath =
                ValidatedPath(Path.Combine(outputPath, srcFolder, ValidName.value libProjectName))

            let! libProject =
                tryToCreateDotnetProjectWithoutRestore
                    { ProjectCreationInputs.ProjectType = ProjectType.ClassLib
                      ProjectCreationInputs.ProjectName = libProjectName
                      ProjectCreationInputs.Language = selectedLanguage
                      ProjectCreationInputs.Path = libPath
                      ProjectCreationInputs.ForceOverWrite = forceOverWrite }

            let! _ = libProject |> tryRemovePropertyGroupFromFile selectedLanguage

            let testProjectName = ValidName.appendTo validSolutionName defaultLibTestName

            let testPath =
                ValidatedPath(Path.Combine(outputPath, testsFolder, ValidName.value testProjectName))

            let! testProject =
                tryToCreateDotnetProjectWithoutRestore
                    { ProjectCreationInputs.ProjectType = ProjectType.XUnit
                      ProjectCreationInputs.ProjectName = testProjectName
                      ProjectCreationInputs.Language = selectedLanguage
                      ProjectCreationInputs.Path = testPath
                      ProjectCreationInputs.ForceOverWrite = forceOverWrite }

            let! _ = tryAddProjectDependency testPath libPath

            let! _ = testProject |> tryRemovePropertyGroupFromFile selectedLanguage
            let! _ = testProject |> tryRemoveItemGroupFromFile selectedLanguage

            let! _ = tryAddProjectToSolution outputPath libPath
            let! _ = tryAddProjectToSolution outputPath testPath
            return ()
        }
