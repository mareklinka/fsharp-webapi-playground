namespace SeedProject.App

open System

open SeedProject.Domain.Operators
open SeedProject.Domain.Common
open SeedProject.WebApi.AbsenceRequests.Types
open SeedProject.WebApi.AbsenceRequests
open SeedProject.Domain.AbsenceRequests.Types
open SeedProject.Persistence.AbsenceRequests
open SeedProject.Domain
open SeedProject.Persistence

open System.Threading

module Handlers =
    let private operation =
        OperationResult.OperationResultBuilder.Instance

    module Database =
        let save db r =
            async {
                printfn "DB: Save changes"

                do!
                    db
                    |> Db.saveChanges CancellationToken.None

                return operation { return r }
            }

        let commit transaction r =
            async {
                printfn "DB: Commit"

                do!
                    transaction
                    |> Db.commit CancellationToken.None

                return operation { return r }
            }

    module AbsenceRequests =
        let readInput () : Async<OperationResult.OperationResult<UpdateData>> =
            async {
                printfn "API: Reading input"

                return
                    operation {
                        //return! OperationResult.validationError (IncompleteData, ValidationMessage "some error message")
                        return
                            { Id = Id 1
                              StartDate = DateTime.Now.Date
                              EndDate = Some(DateTime.Now.Date.AddDays(1.))
                              HalfDayStart = Some true
                              HalfDayEnd = Some false
                              Description = "This is my description"
                              Duration = None
                              PersonalDayType = None }
                    }
            }

        let updateRequest (context: Request.RequestContext<UpdateData, AbsenceRequest>) =
            async {
                printfn "API: Invoking handler"

                let loadById = context.Database |> getSingleRequest
                let update = Operations.updateRequest context.Input
                let store = context.Database |> updateRequestEntity
                let save = Database.save context.Database
                let commit = Database.commit context.Transaction

                let! operationResult =
                    context.Input.Id
                    |> loadById
                    >>= update
                    >>= store
                    >>= save
                    >>= commit

                return
                    operation {
                        let! value = operationResult
                        return { context with Result = Some value }
                    }
            }