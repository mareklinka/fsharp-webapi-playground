namespace SeedProject.Persistence

open Microsoft.EntityFrameworkCore
open System.Threading.Tasks

[<RequireQualifiedAccess>]
module public Db =
    let beginTransaction ct (context: DbContext) =
        context.Database.BeginTransactionAsync(ct) :> Task

    let saveChanges ct (context: DbContext) =
        context.SaveChangesAsync(ct) :> Task

    let migrateDatabase ct (context: DbContext) =
        context.Database.MigrateAsync(ct)

    let commit ct (context: DbContext) =
        context.Database.CommitTransactionAsync(ct)

