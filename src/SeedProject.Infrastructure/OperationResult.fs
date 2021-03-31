namespace SeedProject.Infrastructure

[<AutoOpen>]
module OperationResult =
    type OperationResult<'a> =
        | Success of 'a
        | ValidationError of Common.ValidationError
        | OperationError of Common.OperationError

    [<RequireQualifiedAccess>]
    module OperationResult =
        let map f a =
            match a with
            | Success x -> Success (f x)
            | ValidationError e -> ValidationError e
            | OperationError e -> OperationError e

        let rtrn = Success

        let apply (f : OperationResult<'a -> 'b>) (m : OperationResult<'a>) : OperationResult<'b> =
            match (f, m) with
            | (Success f, Success mResult) -> Success (f mResult)
            | (_, ValidationError e) -> ValidationError e
            | (_, OperationError e) -> OperationError e
            | (ValidationError e, _) -> ValidationError e
            | (OperationError e, _) -> OperationError e

        let private (<*>) = apply
        let private cons head tail = head :: tail

        let rec mapList (f: 'a -> OperationResult<'b>) (list: 'a list) : OperationResult<'b list> =
            match list with
            | [] ->
                Success []
            | head::tail ->
                Success cons <*> (f head) <*> (mapList f tail)

        module Operators =
            let (<!>) = map
            let (<*>) = apply

    [<RequireQualifiedAccess>]
    module Builder =
        [<System.Diagnostics.DebuggerStepThroughAttribute>]
        type OperationResultBuilder() =
            [<System.Diagnostics.DebuggerStepThroughAttribute>]
            let bind m f =
                match m with
                    | Success value ->
                        f value
                    | ValidationError (code, message) ->
                        ValidationError (code, message)
                    | OperationError (code, message) ->
                        OperationError (code, message)

            static member Instance = new OperationResultBuilder()

            member __.Bind(m, f) = bind m f

            member __.Return(x) = Success x

            member __.ReturnFrom(m) = m

            member __.Delay(funcToDelay) = funcToDelay

            member __.Run(funcToRun) = funcToRun()

    let operation = new Builder.OperationResultBuilder()