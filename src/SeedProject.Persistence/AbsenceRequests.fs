namespace SeedProject.Persistence

open FSharp.Control.Tasks

open Microsoft.EntityFrameworkCore

open SeedProject.Infrastructure.Common
open SeedProject.Infrastructure
open SeedProject.Domain.AbsenceRequests.Types
open SeedProject.Persistence.Model
open SeedProject.Domain.Constructors
open SeedProject.Architecture.Common.Constants

open Structurizr.Annotations

[<Component(Description = "Reads and writes domain objects from/to storage", Technology = "F#")>]
[<UsesContainer(DatabaseName, Description = "Reads/writes data from/to", Technology = "TCP/IP")>]
[<RequireQualifiedAccess>]
module AbsenceRequestStore =
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
                                          Description = Description.create r.Description }
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
                        |> OperationResult.mapList (fun r ->
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
                                                  Description = Description.create r.Description }
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
                        Description = (d |> Option.defaultValue null),
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

            entityUpdate |> db.Update |> ignore

            return Success request
        }

    let addEntity (db: DatabaseContext) request =
        task {
            let requestContent = request |> unwrapRequest

            let newEntity =
                match requestContent with
                | HolidayRequest { Description = Description d
                                   Start = s
                                   End = e } ->
                    let (startDate, isStartHalf) = s |> HolidayDate.extract
                    let (endDate, isEndHalf) = e |> HolidayDate.extract

                    new AbsenceRequest(
                        Description = (d |> Option.defaultValue null),
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

            newEntity |> db.Add |> ignore

            return Success newEntity
        }

    let deleteEntity (db: DatabaseContext) id =
        task {
            let deleteEntity = new AbsenceRequest(Id = id)
            deleteEntity |> db.Remove |> ignore

            return Success ()
        }
