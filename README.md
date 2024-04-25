# Templating dotnet projects and solutions

Why?

I want a simple way of setting up new dotnet solutions using [CPM - Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/Central-Package-Management).

Using CPM solves many pain points I have with dotnet development:

- projects using different versions of the same package
- simplify the process of updating packages
- simplify project configs (`*.csproj` / `*fsproj` files)
- harmonize `src` and `test` projects

Idea: 

- I'll probably start with a simple bash script.

## Projects

### Library and Tests

- `.editorconfig`
- `.gitignore`
- `.gitattributes`
- `Directory.Build.props`
- `Directory.Packages.props`
- `global.json`
- `README.md`
- `src`
  - simple library project
- `tests`
  - xunit test project for library project
