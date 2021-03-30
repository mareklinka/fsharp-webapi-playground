namespace SeedProject.Infrastructure

open Common

type private M<'a> = Async<OperationResult.OperationResult<'a>>
type OperationStep<'a, 'b> = 'a -> M<'b>

module Operators =
    let private bind (f : OperationStep<_, _>) (a : M<_>) : M<_> = async {
        let! r = a
        match r with
        | OperationResult.Success value ->
            let next : Async<OperationResult.OperationResult<'b>> = f value
            return! next
        | OperationResult.ValidationError (code, message) -> return OperationResult.ValidationError (code, message)
        | OperationResult.OperationError (code, message) -> return OperationResult.OperationError (code, message)
    }

    let private compose (f : OperationStep<'a, 'b>) (g : OperationStep<'b, 'c>) : 'a -> M<'c> =
        fun x -> bind g (f x)

    let private combine (f : OperationStep<'a, 'b>) (g : OperationStep<'c, 'd>) =
        fun ((a, c): 'a * 'c) ->
            async {
                let! fResult = f a
                let! gResult = g c

                return
                    match (fResult, gResult) with
                    | (OperationResult.Success fVal, OperationResult.Success gVal) ->
                        OperationResult.fromResult ((fVal, gVal))
                    | (OperationResult.ValidationError error, OperationResult.Success _)
                    | (OperationResult.Success _, OperationResult.ValidationError error) ->
                        OperationResult.validationError error
                    | (OperationResult.OperationError error, OperationResult.Success _)
                    | (OperationResult.Success _, OperationResult.OperationError error) ->
                        OperationResult.operationError error
                    | _ ->
                        OperationResult.operationError (AggregateError, OperationMessage "Multiple errors occured")
            }

    let private tap (f : OperationStep<'a, 'b>) (g: 'b -> Async<'c>) =
        fun a ->
            async {
                let! fResult = f a

                match fResult with
                | OperationResult.Success fVal ->
                    do! g fVal |> Async.Ignore
                | _ -> ()

                return fResult
            }

    let private terminate (f : OperationStep<'a, 'b>) (g: 'b -> 'c) (h: SerializationModel -> 'c) =
        fun a ->
            async {
                let! fResult = f a

                match fResult with
                    | OperationResult.Success value ->
                        return g value
                    | OperationResult.ValidationError ve -> return (ve |> Rendering.Validation |> h)
                    | OperationResult.OperationError oe -> return (oe |> Rendering.Operation |> h)
            } |> Async.StartAsTask

    let ( >>= ) a f = bind f a
    let ( &=> ) f g = compose f g
    let ( &== ) f g = tap f g
    let ( &=? ) f (g, h) = terminate f g h
    let ( >&< ) f g = combine f g