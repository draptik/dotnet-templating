# CI Notes

- Current CI system: Github w/ Github Actions.

## TODOs

- [x] create a published artifact using `dotnet publish`
  - [x] see script `publish.sh`
- [x] add local `dotnet publish` script with single executable (linux) and resource folder
- [ ] ci / github actions:
  - [x] testing
  - [x] creating an online artifact (see `dotnet publish`)
  - [x] build for all plattforms
  - [x] add sha sums for generated artifacts
  - [x] add main `.editorconfig` (for `yml`, `md`, etc)
  - [ ] cleanup/improve workflows
  - [ ] linting (fantomas), Husky? git-hook? Best practices?
  - [ ] research:
    - automate releases?
    - prevent pushing to main branch?
    - create changelogs automatically?
    - add a 'version' flag to program? How to update assebmly during release? Best practices?
- If everything works: maybe create a nuget package? Maybe a `dotnet tool`?