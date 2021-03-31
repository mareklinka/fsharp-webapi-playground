namespace SeedProject.Persistence

open FSharp.Control.Tasks

open Microsoft.EntityFrameworkCore

open SeedProject.Infrastructure.Common
open SeedProject.Infrastructure
open SeedProject.Domain.AbsenceRequests.Types
open SeedProject.Persistence.Model
open SeedProject.Domain.Constructors

[<RequireQualifiedAccess>]
module AbsenceRequestPersistence =
    let private unwrapRequest request =
        match request with
        | NewRequest (New r) -> r
        | ApprovedRequest (Approved r) -> r
        | RejectedRequest (Rejected r) -> r

    let getSingleRequest (db: DatabaseContext) ct (Id id) =
        task {
            let! request =
                db
                    .AbsenceRequests
                    .AsNoTracking()
                    .SingleOrDefaultAsync((fun r -> r.Id = id), ct)

            return
                operation {
                    match request with
                    | null ->
                        return!
                            OperationError (
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
                            OperationError (
                                InvariantBroken HolidayRequestMustHaveEndDate,
                                OperationMessage "Invariant broken on the retrieved entity - "
                            )
                }
        }

    let getAllRequests (db: DatabaseContext) ct =
        task {
            let! requests =
                db
                    .AbsenceRequests
                    .AsNoTracking()
                    .ToListAsync(ct)

            return
                operation {
                    let mapped =
                        requests
                        |> List.ofSeq
                        |> OperationResult.map (fun r ->
                            match r with
                            | r when r.EndDate.HasValue ->
                                let startDate =
                                    HolidayDate.create r.StartDate r.IsHalfDayStart

                                let endDate =
                                    HolidayDate.create r.EndDate.Value r.IsHalfDayEnd

                                Success
                                    (NewRequest(
                                        New(
                                            HolidayRequest
                                                { Id = Id r.Id
                                                  Start = startDate
                                                  End = endDate
                                                  Description = Description r.Description }
                                        ))
                                )
                            | _ ->
                                OperationError (
                                    InvariantBroken HolidayRequestMustHaveEndDate,
                                    OperationMessage "Invariant broken on the retrieved entity - "
                                )
                            )

                    return! mapped
                }
        }

    let updateEntity (db: DatabaseContext) request =
        task {
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

            return Success request
        }

    let addEntity (db: DatabaseContext) request =
        task {
            let requestContent = request |> unwrapRequest

            let entityUpdate =
                match requestContent with
                | HolidayRequest { Description = Description d
                                   Start = s
                                   End = e } ->
                    let (startDate, isStartHalf) = s |> HolidayDate.extract
                    let (endDate, isEndHalf) = e |> HolidayDate.extract

                    new AbsenceRequest(
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

            entityUpdate |> db.Add |> ignore

            return Success request
        }
