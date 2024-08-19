# Templating a dotnet solution

Why?

I want a simple way of setting up new dotnet solution using [CPM - Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/Central-Package-Management).

Using CPM solves many pain points I have with dotnet development:

- projects using different versions of the same package
- simplify the process of updating packages
- simplify project configs (`*.csproj` / `*fsproj` files)
- harmonize `src` and `test` projects

I want to fire up a solution from the command line before I start the IDE. This way I have "my setup".
So what does my setup look like?

## My template - what I care about

- `.editorconfig`
- `.gitignore`
- `.gitattributes`
- `Directory.Build.props`
- `Directory.Packages.props`
- `global.json`
- `README.md`
- `src`
  - dedicated `Directory.Build.props`
  - simple library project
- `tests`
  - dedicated `Directory.Build.props` (for test projects)
  - xunit test project referencing the library project from `src`

## My template - with this setup

```txt
tree -L 3 -a          
.
├── Demo.sln
├── Directory.Build.props
├── Directory.Packages.props
├── .editorconfig
├── .gitattributes
├── .gitignore
├── global.json
├── src
│   ├── Demo.MyLib
│   │   ├── Class1.cs
│   │   ├── Demo.MyLib.csproj
│   └── Directory.Build.props
└── tests
    ├── Demo.MyLib.Tests
    │   ├── Demo.MyLib.Tests.csproj
    │   ├── GlobalUsings.cs
    │   └── UnitTest1.cs
    └── Directory.Build.props
```

## Prerequistises

- dotnet 8

## How to install

Download the current version for your OS from the "Release" section: https://github.com/draptik/dotnet-templating/releases

After unpacking, you should have

- a file `TemplatingConsole` (macOS, linux) or `TemplatingConsole.exe` (windows): This is the executable
- a folder `resources`: It contains some of the templates - Feel free to modify them

## How to use

The executable file is not in your path.
Navigate to the folder where things where unpacked.

```sh
./TemplatingConsole --help

USAGE: TemplatingConsole [--help] --solution-name <name> --output-directory <path> [--language <csharp|fsharp>]
                         [--resource-directory <path>] [--force <bool>]

OPTIONS:

    --solution-name, -n <name>
                          The name of the solution to create
    --output-directory, -o <path>
                          The directory where the project will be created
    --language, -l <csharp|fsharp>
                          The language used. Options: csharp, fsharp (defaults to csharp)
    --resource-directory, -r <path>
                          The directory where the resources are located (defaults to location of executable + './resources')
    --force, -f <bool>    Force overwrite of existing files (defaults to true)
    --help                display this list of options.

```

Example usage (C# is the default):

```sh
mkdir ~/tmp/mydemo
./TemplatingConsole -n MyDemo -o "~/tmp/mydemo"
```

Example usage (F#):

```sh
mkdir ~/tmp/mydemofsharp
./TemplatingConsole -n MyDemo -o "~/tmp/mydemofsharp" -l fsharp
```

Verify the created template:

- navigate to the output folder (for example `~/tmp/mydemo` in the c# example above)
- running `dotnet test` in that folder should build, and test, and be happy.