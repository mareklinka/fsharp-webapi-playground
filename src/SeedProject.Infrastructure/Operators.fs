namespace SeedProject.Infrastructure

open Common

module Operators =
    let private bind (f : 'a -> Async<OperationResult.OperationResult<'b>>) (a : Async<OperationResult.OperationResult<'a>>) : Async<OperationResult.OperationResult<'b>> = async {
        let! r = a
        match r with
        | OperationResult.Success value ->
            let next : Async<OperationResult.OperationResult<'b>> = f value
            return! next
        | OperationResult.ValidationError (code, message) -> return OperationResult.ValidationError (code, message)
        | OperationResult.OperationError (code, message) -> return OperationResult.OperationError (code, message)
    }

    let private compose (f : 'a -> Async<OperationResult.OperationResult<'b>>) (g : 'b -> Async<OperationResult.OperationResult<'c>>) : 'a -> Async<OperationResult.OperationResult<'c>> =
        fun x -> bind g (f x)

    let private combine (f : 'a -> Async<OperationResult.OperationResult<'b>>) (g : 'c -> Async<OperationResult.OperationResult<'d>>) ((a, c): 'a * 'c) =
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

    let private passthrough (f : 'a -> Async<OperationResult.OperationResult<'b>>) (g: 'b -> Async<'c>) (a: 'a) =
        async {
            let! fResult = f a

            match fResult with
            | OperationResult.Success fVal ->
                do! g fVal |> Async.Ignore
            | _ -> ()

            return fResult
        }

    let private terminate (f : 'a -> Async<OperationResult.OperationResult<'b>>) (g: obj -> 'c) (h: obj -> 'c) (a: 'a) =
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
    let ( &== ) f g = passthrough f g
    let ( &=? ) f (g, h) = terminate f g h
    let ( >&< ) f g = combine f g