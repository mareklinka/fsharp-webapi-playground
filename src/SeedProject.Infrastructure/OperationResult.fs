namespace SeedProject.Infrastructure

[<AutoOpen>]
module OperationResult =
    type OperationResult<'a> =
        | Success of 'a
        | ValidationError of Common.ValidationError
        | OperationError of Common.OperationError

    [<RequireQualifiedAccess>]
    module OperationResult =

        let apply (fM : OperationResult<'a -> 'b>) (m : OperationResult<'a>) : OperationResult<'b> =
            let fmResult = fM
            let mResult = m

            match (fmResult, mResult) with
            | (Success f, Success mResult) -> Success (f mResult)
            | (_, ValidationError (code, message)) -> ValidationError (code, message)
            | (_, OperationError (code, message)) -> OperationError (code, message)
            | (ValidationError (code, message), _) -> ValidationError (code, message)
            | (OperationError (code, message), _) -> OperationError (code, message)

        let rec map (f: 'a -> OperationResult<'b>) (list: 'a list) : OperationResult<'b list> =
            let (<*>) = apply
            let cons head tail = head :: tail

            match list with
            | [] ->
                Success []
            | head::tail ->
                Success cons <*> (f head) <*> (map f tail)

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