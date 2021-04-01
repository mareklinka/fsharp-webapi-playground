namespace SeedProject.Infrastructure

[<AutoOpen>]
module Operators =
    let ( |?? ) left (right: Lazy<'a>) = Option.orElseWith (fun () -> right.Value |> Option.ofObj) (left |> Option.ofObj)