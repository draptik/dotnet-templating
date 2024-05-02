open System.Diagnostics

let createDotnetProject (projectType: string) (name: string) (path: string) (language: string) : unit =
    Process.Start(
        "dotnet",
        $"new %s{projectType} --name %s{name} --output %s{path} --language %s{language}")
    |> ignore