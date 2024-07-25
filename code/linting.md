# Linting

Use `fantomas` a F# linter.

Homepage: https://fsprojects.github.io/fantomas/

## Usage - CLI

```sh
dotnet fantomas .
```

## Usage - Rider

See `Settings` -> `Editor` -> `Code Style` -> `F#`: Tab `Fantomas`

Then, use the default `Code Cleanup` feature of Rider (`Ctrl-e Ctrl-c`).

## Usage - Other Editors/IDEs

- [Visual Studio](https://fsprojects.github.io/fantomas/docs/end-users/VisualStudio.html)
- [Visual Studio Code](https://fsprojects.github.io/fantomas/docs/end-users/VSCode.html)

## Configuration

`fantomas` uses `.editorconfig` as configuration file. See [here](https://fsprojects.github.io/fantomas/docs/end-users/Configuration.html) for further details.

## Git hooks

I currently don't use git hooks.

From the [docs](https://fsprojects.github.io/fantomas/docs/end-users/GitHooks.html): "Please use with caution as Fantomas is not without bugs."

## CI

- [ ] TODO research: maybe add to build pipeline?