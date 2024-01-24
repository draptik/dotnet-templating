# Templating dotnet projects

Idea: 

- I often need to setup dotnet solutions, but don't want to dive into the official 'templating' way of doing this.
- I'll probably start with simple bash scripts.
- If simple scripts work I might consider writing a simple TUI.

## Projects

### WebAPI, Library and Tests

- `.editorconfig`
- `.gitignore`
- `.gitattributes`
- `Directory.Build.props`
- `Directory.Packages.props`
- `global.json`
- `README.md`
- `src`
  - simple WebAPI project
  - simple library project
- `tests`
  - xunit test project for WebAPI project
  - xunit test project for library project
