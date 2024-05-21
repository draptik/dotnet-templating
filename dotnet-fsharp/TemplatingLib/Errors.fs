module TemplatingLib.Errors

type ApplicationError =
    | InvalidName of string
    | UnknownProjectType of string
    | UnknownLanguage of string
    | CantCreateOutputDirectory of string
    | CantCreateDotnetProject of string
    | CantCreateConfigFile of error: string * configType: string
    | CantCopyResource of src: string * target: string * error: string
    | CantCreateSolution of string
    | CantCreateDependency of string
    | CantRemovePropertyGroup of string
    | CantRemoveItemGroup of string
