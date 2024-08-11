namespace TemplatingLib

open System.Diagnostics
open System.IO
open FsToolkit.ErrorHandling
open Errors
open TemplatingLib.Types
open TemplatingLib.Xml

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

    let appendForce b = if b then " --force" else ""

    let tryToCreateDotnetProjectWithoutRestore
        (projectCreationInputs: ProjectCreationInputs)
        : Result<ValidatedPath, ApplicationError> =
        let name, projectType, lang, path, forceOverwrite =
            unwrapProjectCreationInputs projectCreationInputs

        let args =
            $"new %s{projectType} --name %s{name} --output %s{path} --language %s{lang} --no-restore%s{appendForce forceOverwrite}"

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
            let args =
                $"new %s{config} --sdk-version %s{Constants.latestLts} --roll-forward %s{Constants.rollForwardPolicy} --output %s{path}%s{appendForce forceOverwrite}"

            startDotnetProcess args
        | _ ->
            let args = $"new %s{config} --output %s{path}%s{appendForce forceOverwrite}"
            startDotnetProcess args

    let tryCopy (source: string) (target: string) =
        try
            File.Copy(source, target, overwrite = true)
            Ok $"File copied from {source} to {target}."
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

    let workflow solutionName outputDirectory (selectedLanguage: Language) (templates: Templates) =

        let rootBuildPropsTemplate = templates.RootBuildProps
        let srcDirBuildPropsTemplate = templates.SrcDirBuildProps
        let testsDirBuildPropsTemplate = templates.TestDirBuildProps
        let rootPackagesTemplate = templates.RootPackagesProps
        let gitAttributesTemplate = templates.GitAttributes
        let editorConfigFsharpTemplate = templates.EditorConfigFsharp
        let forceOverWrite = templates.ForceOverwrite

        // short hands for constants
        let srcFolder = Constants.src
        let testsFolder = Constants.tests
        let directoryBuildProps = Constants.DirectoryBuildProps
        let directoryPackagesProps = Constants.DirectoryPackagesProps
        let gitAttributes = Constants.gitAttributes
        let editorConfig = Constants.editorConfig
        let defaultLibName = Constants.defaultLibName
        let defaultLibTestName = Constants.defaultLibTestName

        result {
            let! validSolutionName = ValidName.create solutionName
            let! outputPath = tryToCreateOutputDirectory outputDirectory

            let! _ = tryToCreateOutputDirectory (Path.Combine(outputPath, srcFolder))
            let! _ = tryToCreateOutputDirectory (Path.Combine(outputPath, testsFolder))

            let! _ = tryCreateConfigFile GitIgnore outputPath forceOverWrite
            let! _ = tryCreateConfigFile GlobalJson outputPath forceOverWrite

            // editorconfig: F# is very simple compared to C#
            // - C#: Use the `editorconfig` tool (`dotnet new editorconfig`)
            // - F#: Copy simple template
            let! _ =
                match selectedLanguage with
                | CSharp -> tryCreateConfigFile EditorConfig outputPath forceOverWrite
                | FSharp -> tryCopy editorConfigFsharpTemplate (Path.Combine(outputPath, editorConfig))

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
            let! _ = libProject |> tryAddPropertyGroupGenerateDocumentationFile selectedLanguage

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
