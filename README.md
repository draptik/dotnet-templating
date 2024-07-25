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
