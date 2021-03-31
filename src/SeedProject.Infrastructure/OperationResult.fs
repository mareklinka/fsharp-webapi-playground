namespace SeedProject.Infrastructure

[<AutoOpen>]
module OperationResult =
    type OperationResult<'a> =
        | Success of 'a
        | ValidationError of Common.ValidationError
        | OperationError of Common.OperationError

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