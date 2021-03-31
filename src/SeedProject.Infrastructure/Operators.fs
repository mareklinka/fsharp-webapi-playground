namespace SeedProject.Infrastructure

open System.Threading.Tasks
open FSharp.Control.Tasks
open Common

type private M<'a> = Task<OperationResult.OperationResult<'a>>
type OperationStep<'a, 'b> = 'a -> M<'b>

module Operators =
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
                    match (fResult, gResult) with
                    | (Success fVal, Success gVal) ->
                        Success ((fVal, gVal))
                    | (ValidationError error, Success _)
                    | (Success _, ValidationError error) ->
                        ValidationError error
                    | (OperationError error, Success _)
                    | (Success _, OperationError error) ->
                        OperationError error
                    | _ ->
                        OperationError (AggregateError, OperationMessage "Multiple errors occured")
            }

    let private tap (f : OperationStep<'a, 'b>) (g: 'b -> Task) =
        fun a ->
            task {
                let! fResult = f a

                match fResult with
                | OperationResult.Success fVal ->
                    do! g fVal
                | _ -> ()

                return fResult
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

    let ( >>= ) a f = bind f a
    let ( &=> ) f g = compose f g
    let ( &== ) f g = tap f g
    let ( &=! ) f (g, h, i) = terminate f g h i
    let ( >&< ) f g = combine f g