open System.Threading

open SeedProject.Persistence
open SeedProject.App

let migrateDatabase =
    async {
        use context =
            DependencyInjection.createContextOptions ()
            |> DependencyInjection.createContext

        do!
            context
            |> Db.migrateDatabase CancellationToken.None
    }

[<EntryPoint>]
let main argv =
    async {
        do! migrateDatabase

        do!
            Request.run
                (fun () -> Request.buildRequestContext Handlers.AbsenceRequests.readInput)
                (fun context -> context |> Handlers.AbsenceRequests.updateRequest |> Request.respond)
    }
    |> Async.RunSynchronously

    printfn "'s all good"

    0