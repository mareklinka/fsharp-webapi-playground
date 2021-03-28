namespace SeedProject.Persistence

open SeedProject.Domain
open SeedProject.Domain.Common
open SeedProject.Domain.AbsenceRequests.Types
open SeedProject.Persistence.Model
open Microsoft.EntityFrameworkCore
open SeedProject.Domain.Constructors

module AbsenceRequests =
    let private operation =
        OperationResult.OperationResultBuilder.Instance

    let private unwrapRequest request =
        match request with
        | NewRequest (New r) -> r
        | ApprovedRequest (Approved r) -> r
        | RejectedRequest (Rejected r) -> r

    let getSingleRequest
        (db: DatabaseContext)
        (Id id)
        : Async<OperationResult.OperationResult<AbsenceRequests.Types.AbsenceRequest>> =

        async {
            printfn "DB: Loading from storage"
            let! request =
                db
                    .AbsenceRequests
                    .AsNoTracking()
                    .SingleOrDefaultAsync(fun r -> r.Id = id)
                |> Async.AwaitTask

            return
                operation {
                    match request with
                    | null ->
                        return!
                            OperationResult.operationError (
                                NotFound(Id id),
                                OperationMessage "The specified request was not found"
                            )
                    | r when r.EndDate.HasValue ->
                        let startDate =
                            HolidayDate.create r.StartDate r.IsHalfDayStart

                        let endDate =
                            HolidayDate.create r.EndDate.Value r.IsHalfDayEnd

                        return
                            NewRequest(
                                New(
                                    HolidayRequest
                                        { Id = Id r.Id
                                          Start = startDate
                                          End = endDate
                                          Description = Description r.Description }
                                )
                            )
                    | _ ->
                        return!
                            OperationResult.operationError (
                                InvariantBroken HolidayRequestMustHaveEndDate,
                                OperationMessage "Invariant broken on the retrieved entity - "
                            )
                }
        }

    let updateRequestEntity
        (db: DatabaseContext)
        (request)
        : Async<OperationResult.OperationResult<AbsenceRequests.Types.AbsenceRequest>> =
        async {
            printfn "DB: Storing updated request"
            let requestContent = request |> unwrapRequest

            let entityUpdate =
                match requestContent with
                | HolidayRequest { Id = Id id
                                   Description = Description d
                                   Start = s
                                   End = e } ->
                    let (startDate, isStartHalf) = s |> HolidayDate.extract
                    let (endDate, isEndHalf) = e |> HolidayDate.extract

                    new AbsenceRequest(
                        Id = id,
                        Description = d,
                        StartDate = startDate,
                        IsHalfDayStart = isStartHalf,
                        EndDate = endDate,
                        IsHalfDayEnd = isEndHalf
                    )
                | PersonalDayRequest { Id = Id id
                                       Description = Description d
                                       Date = date } -> failwith "Not Implemented"
                | SickdayRequest { Id = Id id
                                   Description = Description d
                                   Date = date
                                   Duration = (hour, minute) } -> failwith "Not Implemented"
                | DoctorVisitRequest { Id = Id id
                                       Description = Description d
                                       Date = date
                                       Duration = (hour, minute) } -> failwith "Not Implemented"
                | DoctorVisitWithFamilyRequest { Id = Id id
                                                 Description = Description d
                                                 Date = date
                                                 Duration = (hour, minute) } -> failwith "Not Implemented"
                | SicknessRequest { Id = Id id
                                    Description = Description d
                                    Start = s
                                    End = e } -> failwith "Not Implemented"
                | PandemicSicknessRequest { Id = Id id
                                            Description = Description d
                                            Start = s
                                            End = e } -> failwith "Not Implemented"

            let e = entityUpdate |> Db.attach db
            e.State <- EntityState.Modified

            return OperationResult.fromResult request
        }
