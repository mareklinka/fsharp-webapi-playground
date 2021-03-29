namespace SeedProject.Infrastructure

[<RequireQualifiedAccess>]
module OperationResult =
    type OperationResult<'a> =
        | Success of 'a
        | ValidationError of Common.ValidationError
        | OperationError of Common.OperationError

    [<System.Diagnostics.DebuggerStepThroughAttribute>]
    let bind m f =
        match m with
            | Success value ->
                f value
            | ValidationError (code, message) ->
                ValidationError (code, message)
            | OperationError (code, message) ->
                OperationError (code, message)

    [<System.Diagnostics.DebuggerStepThroughAttribute>]
    type OperationResultBuilder() =
        static member Instance = new OperationResultBuilder()

        member __.Bind(m, f) = bind m f

        member __.Return(x) = Success x

        member __.ReturnFrom(m) = m

        member __.Delay(funcToDelay) = funcToDelay

        member __.Run(funcToRun) = funcToRun()

    [<System.Diagnostics.DebuggerStepThroughAttribute>]
    let fromResult result = Success result

    [<System.Diagnostics.DebuggerStepThroughAttribute>]
    let validationError error = ValidationError error

    [<System.Diagnostics.DebuggerStepThroughAttribute>]
    let operationError error = OperationError error
