namespace SeedProject.Persistence

open FSharp.Control.Tasks

open Microsoft.EntityFrameworkCore
open Microsoft.EntityFrameworkCore.Storage
open System.Threading.Tasks

[<RequireQualifiedAccess>]
module public Db =
    let beginTransaction ct (context: DbContext) =
        context.Database.BeginTransactionAsync(ct) :> Task

    let saveChanges ct (context: DbContext) =
        context.SaveChangesAsync(ct) :> Task

    let migrateDatabase ct (context: DbContext) =
        context.Database.MigrateAsync(ct)

    let commit ct (transaction: IDbContextTransaction) =
        transaction.CommitAsync(ct)

    let set<'a when 'a: not struct> (context: DbContext) = context.Set<'a>()

    let attach<'a when 'a: not struct> (context: DbContext) entity = entity |> context.Set<'a>().Attach
