module Tests

open Xunit

[<Fact>]
let ``create project`` () =
    TemplatingLib.Io.createDotnetProject "classlib" "Foo" "~/tmp/foo" "c#"
    