namespace SeedProject.Functional

open System.Threading.Tasks
open FSharp.Control.Tasks

[<AutoOpen>]
module Combinators =
    let ( |- ) (t: Task<'a>) (f: 'a -> Task<'b>) =
        task {
            let! tValue = t
            return! f tValue
        }

    let ( |= ) (t: Task<'a>) (f: 'a -> 'b) =
        task {
            let! tValue = t
            return f tValue
        }