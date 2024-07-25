# CI Notes

- Current CI system: Github w/ Github Actions.

## Creating a release

I'm still learning the basics of GH-Actions.

- Create git tag on the `main` branch
- Click on "Releases" -> "Create a new release", then select the git tag from the previous step...
- current workflow on local machine:
  - `git commit`..
  - `git tag v0.0.xx`
  - `git push --atomic origin main v0.0.xx`
  - browser: see above

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
  - [x] cleanup/improve workflows
  - [x] linting (fantomas) I'll skip Husky / git-hook for now
  - [ ] research:
    - automate releases?
    - prevent pushing to main branch?
    - create changelogs automatically?
    - add a 'version' flag to program? How to update assebmly during release? Best practices?
- If everything works: maybe create a nuget package? Maybe a `dotnet tool`?