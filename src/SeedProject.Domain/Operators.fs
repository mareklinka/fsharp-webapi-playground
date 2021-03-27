namespace SeedProject.Domain

open SeedProject.Domain

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

    let private compose (f : 'a -> Async<OperationResult.OperationResult<'b>>) (g : 'b -> Async<OperationResult.OperationResult<'c>>) : 'a -> Async<OperationResult.OperationResult<'c>> =
        fun x -> bind g (f x)

    let (>>=) a f = bind f a
    let (>=>) f g = compose f g