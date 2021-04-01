namespace SeedProject.Host.Tests

open FsCheck.Xunit

open SeedProject.Infrastructure.Common
open SeedProject.Infrastructure
open SeedProject.Domain.Constructors

open SeedProject.Tests.Infrastructure
module HelperMethods =
    [<Property>]
    let ``Converting integer to database ID wraps the given value`` value =
        let result = value |> DatabaseId.createAsync |> Task.result

        match result with
        | Success (Id id) -> id = value
        | _ -> value < 1

