open System
open System.Threading
open Microsoft.EntityFrameworkCore
open SeedProject.Persistence
open SeedProject.Persistence.Model
open SeedProject.Domain.Common
open SeedProject.Domain.AbsenceRequests
open SeedProject.Domain

let CreateContextOptions =
    let builder = new DbContextOptionsBuilder<DatabaseContext>()
    builder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;Database=FSharpSeedDatabase")
    |> ignore

    fun () -> builder.Options

let CreateContext (options: DbContextOptions<DatabaseContext>) = new DatabaseContext(options)

let LifetimeScope<'a when 'a : not struct> (factory: unit -> 'a) =
    let instance = factory()

    match box instance with
    | :? System.IDisposable as disposable -> (fun () -> instance), disposable
    | _ -> (fun () -> instance), { new System.IDisposable with member __.Dispose() = () }


[<EntryPoint>]
let main argv =
    let (dbContextProvider, disposer) = CreateContextOptions >> CreateContext |> LifetimeScope
    use disposer = disposer

    async {
        do!
            dbContextProvider()
            |> Db.migrateDatabase CancellationToken.None

        let! t =
            dbContextProvider()
            |> Db.beginTransaction CancellationToken.None

        use t = t

        do! t |> Db.commit CancellationToken.None

        let request : AbsenceRequestType =
            HolidayRequest
                { HolidayRequest.Id = Id 1
                  Start = FullDay DateTime.Today
                  End = HalfDay(DateTime.Today.AddDays(5.).Date)
                  Description = Description "My test description" }

        request |> Formatter.FormatAbsenceRequest |> printf "%s"
    }
    |> Async.RunSynchronously

    0
