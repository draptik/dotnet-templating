# Creating a customized dotnet project w/ `Directory.{Build|Packages}.props`

- Since this is an opinionated dotnet project setup, I assume dotnet is installed.
- I'll start with dotnet8 (!).

## TODOs

- ~~create a published artifact using `dotnet publish`~~
    - see script `publish.sh`
- ~~add local `dotnet publish` script with single executable (linux) and resource folder~~
- add github actions for
    - ~~testing~~
    - creating an online artifact (see `dotnet publish`)
- improve output project validation (maybe use `Verify`?)
- add tests for f# project setup
- maybe add Husky.NET fantomas hooks to git?
- If everything works: maybe create a nuget package?
