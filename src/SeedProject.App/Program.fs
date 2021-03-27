open System
open System.Threading

open SeedProject.Persistence
open SeedProject.Persistence.AbsenceRequests
open SeedProject.Domain.Common
open SeedProject.Domain
open SeedProject.Domain.Operators

open SeedProject.App
open SeedProject.Domain.AbsenceRequests.Types
open SeedProject.WebApi.AbsenceRequests.Types

let private operation =
    OperationResult.OperationResultBuilder.Instance

let handleRequestUpdate (context: Request.RequestContext<UpdateData, AbsenceRequest>) =
    printfn "Handling request"

    async {
        let loadById = context.Database |> getSingleRequest

        let! operationTask =
            context.Input.Id
            |> loadById
            >>= SeedProject.WebApi.AbsenceRequests.Operations.updateRequest context.Input
            >>= Request.saveAndCommit context

        return
            operation {
                let! result = operationTask
                return { context with Result = Some result }
            }
    }

let migrateDatabase =
    async {
        use context =
            DependencyInjection.createContextOptions ()
            |> DependencyInjection.createContext

        do!
            context
            |> Db.migrateDatabase CancellationToken.None
    }


let readPayload () : Async<OperationResult.OperationResult<UpdateData>> =
    async {
        return
            operation {
                //return! OperationResult.validationError (IncompleteData, ValidationMessage "some error message")
                return
                    { SeedProject.WebApi.AbsenceRequests.Types.UpdateData.Id = Id 1
                      SeedProject.WebApi.AbsenceRequests.Types.UpdateData.StartDate = DateTime.Now.Date
                      SeedProject.WebApi.AbsenceRequests.Types.UpdateData.EndDate = Some(DateTime.Now.Date.AddDays(1.))
                      SeedProject.WebApi.AbsenceRequests.Types.UpdateData.HalfDayStart = Some true
                      SeedProject.WebApi.AbsenceRequests.Types.UpdateData.HalfDayEnd = Some false
                      SeedProject.WebApi.AbsenceRequests.Types.UpdateData.Description = "This is my description"
                      SeedProject.WebApi.AbsenceRequests.Types.UpdateData.Duration = None
                      SeedProject.WebApi.AbsenceRequests.Types.UpdateData.PersonalDayType = None }
            }
    }

[<EntryPoint>]
let main argv =
    async {
        do! migrateDatabase

        do!
            Request.run
                (fun () -> Request.buildRequestContext readPayload)
                (fun context -> context |> handleRequestUpdate |> Request.respond)
    }
    |> Async.RunSynchronously

    printfn "'s all good"

    0