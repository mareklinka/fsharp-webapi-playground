namespace SeedProject.Persistence

open Microsoft.EntityFrameworkCore
open SeedProject.Persistence.Model
open SeedProject.Domain.Person
open System.Threading

module Person =
    let GetPersonById (context: DatabaseContext) (ct: CancellationToken) id =
        async {
            let! person =
                context.People.SingleOrDefaultAsync((fun p -> p.Id = id), ct)
                |> Async.AwaitTask

            return
                match person with
                | null -> None
                | p -> p |> ConstructPerson |> Some
        }

    let GetPeople (context: DatabaseContext) (ct: CancellationToken) =
        async {
            let! people = context.People.ToListAsync(ct) |> Async.AwaitTask

            return
                people
                |> Seq.map (fun p -> p |> ConstructPerson)
                |> List.ofSeq
        }

    let AddPerson (context: DatabaseContext) { FirstName = FirstName fn; LastName = LastName ln } =
        let entity = new PersonEntity()
        entity.FirstName <- fn
        entity.LastName <- ln

        entity |> context.People.Add |> ignore
        ()
