open System
open System.Threading

open SeedProject.Persistence
open SeedProject.Persistence.AbsenceRequests
open SeedProject.Domain.Common
open SeedProject.Domain
open SeedProject.Domain.Operators

open SeedProject.App.DependencyInjection
open SeedProject.App.RequestPipeline
open SeedProject.Domain.AbsenceRequests.Types

let private operation =
    OperationResult.OperationResultBuilder.Instance

let handleRequestUpdate (context: RequestContext<SeedProject.WebApi.AbsenceRequests.Types.UpdateData, AbsenceRequest>) =
    printf "Handling request"
    async {
        let loadById = context.Database |> getSingleRequest

        let! result =
            context.Input.Id
            |> loadById
            >>= SeedProject.WebApi.AbsenceRequests.Operations.updateRequest context.Input
            >>= saveAndCommit context

        let context = { context with Result = Some result }

        return operation {
            return context
        }
    }

let migrateDatabase =
    async {
        let (dbContextProvider, disposer) =
            CreateContextOptions
            >> CreateContext
            |> LifetimeScope

        use disposer = disposer

        do!
            dbContextProvider ()
            |> Db.migrateDatabase CancellationToken.None
    }


let readPayload () : Async<OperationResult.OperationResult<SeedProject.WebApi.AbsenceRequests.Types.UpdateData>> =
    async {
        return
            operation {
                //return! OperationResult.validationError (IncompleteData, ValidationMessage "some error message")
                return
                    { SeedProject.WebApi.AbsenceRequests.Types.UpdateData.Id = Id 1
                      SeedProject.WebApi.AbsenceRequests.Types.UpdateData.StartDate = DateTime.Now.Date
                      SeedProject.WebApi.AbsenceRequests.Types.UpdateData.EndDate = Some(DateTime.Now.Date.AddDays(-1.))
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

        let (dbContextProvider, disposer) =
            CreateContextOptions
            >> CreateContext
            |> LifetimeScope

        use disposer = disposer

        // todo: handle exceptions

        do!
            buildRequestContext readPayload
            >>= handleRequestUpdate
            |> cleanup
            |> respond
    }
    |> Async.RunSynchronously

    0
