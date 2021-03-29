namespace SeedProject.Host.Tests

open FsCheck.Xunit
open SeedProject.Infrastructure.Common
open SeedProject.Infrastructure

module HelperMethods =

    [<Property>]
    let ``Converting integer to database ID wraps the given value`` value =
        let result = value |> SeedProject.Host.Handlers.AbsenceRequests.Private.toDatabaseId |> Async.RunSynchronously
        match result with
        | OperationResult.Success (Id id) -> id = value
        | _ -> value < 1
