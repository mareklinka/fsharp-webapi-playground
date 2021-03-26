namespace SeedProject.Persistence

open Microsoft.EntityFrameworkCore
open Microsoft.EntityFrameworkCore.Storage

[<RequireQualifiedAccess>]
module public Db =
    let beginTransaction ct (context: DbContext) =
        async {
            return! ct |> context.Database.BeginTransactionAsync |> Async.AwaitTask
        }

    let saveChanges ct (context: DbContext) =
        async {
            do! ct |> context.SaveChangesAsync |> Async.AwaitTask |> Async.Ignore
        }

    let migrateDatabase ct (context: DbContext) =
        async {
            do! context.Database.MigrateAsync(ct) |> Async.AwaitTask
        }

    let commit ct (transaction: IDbContextTransaction) =
        async {
            do! transaction.CommitAsync(ct) |> Async.AwaitTask
        }

    let set<'a when 'a : not struct> (context: DbContext) =
        context.Set<'a>()