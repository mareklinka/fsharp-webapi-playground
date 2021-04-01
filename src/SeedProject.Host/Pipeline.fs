namespace SeedProject.Host

open System.Threading.Tasks

open FSharp.Control.Tasks

open Giraffe

open SeedProject.Persistence

open SeedProject.Infrastructure
open SeedProject.Infrastructure.Common

[<RequireQualifiedAccess>]
module Pipeline =
    module Operators =
        type private M<'a> = Task<OperationResult.OperationResult<'a>>
        type private OperationStep<'a, 'b> = 'a -> M<'b>

        let private bind (f : OperationStep<_, _>) (a : M<_>) : M<_> = task {
            let! r = a
            match r with
            | Success value ->
                let next = f value
                return! next
            | ValidationError (code, message) -> return ValidationError (code, message)
            | OperationError (code, message) -> return OperationError (code, message)
        }

        let private compose (f : OperationStep<'a, 'b>) (g : OperationStep<'b, 'c>) : 'a -> M<'c> =
            fun x -> bind g (f x)

        let private combine (f : OperationStep<'a, 'b>) (g : OperationStep<'c, 'd>) =
            fun ((a, c): 'a * 'c) ->
                task {
                    let! fResult = f a
                    let! gResult = g c

                    return
                        operation {
                           let! fValue = fResult
                           let! gValue = gResult

                           return (fValue, gValue)
                        }
                }

        let private terminate (f : OperationStep<'a, 'b>) (g: 'b -> 'c) (h: SerializationModel -> 'c) (i: SerializationModel -> 'c) =
            fun a ->
                task {
                    let! fResult = f a

                    match fResult with
                        | Success value ->
                            return g value
                        | ValidationError ve -> return (ve |> Rendering.Validation |> h)
                        | OperationError oe -> return (oe |> Rendering.Operation |> i)
                }

        let ( &=> ) f g = compose f g
        let ( &=! ) f (g, h, i) = terminate f g h i
        let ( >&< ) f g = combine f g

    let beginTransaction ct db r =
        task {
            do! Db.beginTransaction ct db
            return Success r
        }

    let saveChanges ct db r =
        task {
            do! Db.saveChanges ct db
            return Success r
        }

    let commit ct db r =
        task {
            do! Db.commit ct db
            return Success r
        }

    let sideEffect f r =
        r |> f
        Task.FromResult(Success r)

    let transform f r =
        Task.FromResult(r |> f |> Success)

    let private validationErrorOutput = RequestErrors.NOT_ACCEPTABLE
    let private operationErrorOutput = RequestErrors.BAD_REQUEST

    let response apiOutput =
        (apiOutput, validationErrorOutput, operationErrorOutput)
    let writeJson = (json, validationErrorOutput, operationErrorOutput)