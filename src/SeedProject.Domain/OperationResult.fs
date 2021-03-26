namespace SeedProject.Domain

[<RequireQualifiedAccess>]
module OperationResult =
    type OperationResult<'a> =
        | Success of 'a
        | ValidationError of Common.ValidationError
        | OperationError of Common.OperationError

    type OperationResultBuilder() =
        static member Instance = new OperationResultBuilder()

        member __.Bind(m, f) =
            match m with
            | Success value ->
                f value
            | ValidationError (code, message) ->
                ValidationError (code, message)
            | OperationError (code, message) ->
                OperationError (code, message)

        member __.Return(x) = Success x

        member __.ReturnFrom(m) = m

    let fromResult result = Success result
    let validationError error = ValidationError error
    let operationError error = OperationError error