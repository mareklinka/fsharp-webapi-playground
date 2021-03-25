open SeedProject.Persistence.Model
open SeedProject.Domain.Person
open SeedProject.Persistence.Person
open Microsoft.EntityFrameworkCore
open System.Threading

let CreateContextOptions =
    let builder = new DbContextOptionsBuilder<DatabaseContext>()
    builder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;Database=FSharpSeedDatabase") |> ignore
    builder.Options

let CreateContext (options: DbContextOptions<DatabaseContext>) =
    new DatabaseContext(options)

let MigrateDatabase (context: DatabaseContext) =
    async {
        do! context.Database.MigrateAsync() |> Async.AwaitTask
    }

[<EntryPoint>]
let main argv =
    use context = CreateContext CreateContextOptions
    let createPerson = AddPerson context
    let getPeople = GetPeople context

    async {
        do! context |> MigrateDatabase

        { FirstName = FirstName "Marek"; LastName = LastName "Linka" } |> createPerson

        do! context.SaveChangesAsync(CancellationToken.None) |> Async.AwaitTask |> Async.Ignore
        let! people = getPeople CancellationToken.None

        printf "%A" people
    } |> Async.RunSynchronously

    0
