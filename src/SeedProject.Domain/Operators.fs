namespace SeedProject.Domain

open SeedProject.Domain
open Common

module Operators =
    let private bind (f : 'a -> Async<OperationResult.OperationResult<'b>>) (a : Async<OperationResult.OperationResult<'a>>)  : Async<OperationResult.OperationResult<'b>> = async {
        let! r = a
        match r with
        | OperationResult.Success value ->
            let next : Async<OperationResult.OperationResult<'b>> = f value
            return! next
        | OperationResult.ValidationError (code, message) -> return OperationResult.ValidationError (code, message)
        | OperationResult.OperationError (code, message) -> return OperationResult.OperationError (code, message)
    }

    let compose (f : 'a -> Async<OperationResult.OperationResult<'b>>) (g : 'b -> Async<OperationResult.OperationResult<'c>>) : 'a -> Async<OperationResult.OperationResult<'c>> =
        fun x -> bind g (f x)

    let combine (f : 'a -> Async<OperationResult.OperationResult<'b>>) (g : 'a -> Async<OperationResult.OperationResult<'c>>) (a: 'a) =
        async {

            let! fResult = f a
            let! gResult = g a

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

    let ( >>= ) a f = bind f a
    let ( >=> ) f g = compose f g
    let ( >=< ) f g = combine f g