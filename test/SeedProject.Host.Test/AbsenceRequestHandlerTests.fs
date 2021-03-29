namespace SeedProject.Host.Tests

open FsCheck.Xunit
open SeedProject.Infrastructure.Common
open SeedProject.Infrastructure
open SeedProject.Domain.Constructors

module HelperMethods =
    [<Property>]
    let ``Converting integer to database ID wraps the given value`` value =
        let result = value |> DatabaseId.createAsync |> Async.RunSynchronously
        match result with
        | OperationResult.Success (Id id) -> id = value
        | _ -> value < 1
