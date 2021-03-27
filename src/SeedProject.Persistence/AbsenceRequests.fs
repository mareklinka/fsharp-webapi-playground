namespace SeedProject.Persistence

open SeedProject.Domain
open SeedProject.Domain.Common
open SeedProject.Domain.AbsenceRequests.Types
open SeedProject.Persistence.Model
open Microsoft.EntityFrameworkCore
open SeedProject.Domain.Constructors

module AbsenceRequests =
    let private operation = OperationResult.OperationResultBuilder.Instance

    let getSingleRequest (db: DatabaseContext) (Id id) : Async<OperationResult.OperationResult<AbsenceRequests.Types.AbsenceRequest>> =
        async {
            let! request = db.AbsenceRequests.AsNoTracking().SingleOrDefaultAsync(fun r -> r.Id = id) |> Async.AwaitTask

            return operation {
                match request with
                | null ->
                    return! OperationResult.operationError (NotFound (Id id), OperationMessage "The specified request was not found")
                | r when r.EndDate.HasValue ->
                    let startDate = HolidayDate.create r.StartDate r.IsHalfDayStart
                    let endDate = HolidayDate.create r.EndDate.Value r.IsHalfDayEnd
                    return NewRequest (New (HolidayRequest { Id = Id r.Id; Start = startDate; End = endDate; Description = Description r.Description }))
                | _ ->
                    return! OperationResult.operationError (InvariantBroken HolidayRequestMustHaveEndDate, OperationMessage "Invariant broken on the retrieved entity - ")
            }
        }