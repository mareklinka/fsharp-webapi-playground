namespace SeedProject.Domain.Constructors

[<RequireQualifiedAccess>]
module RequestDuration =
    open SeedProject.Infrastructure.Common
    open SeedProject.Infrastructure
    open SeedProject.Domain.AbsenceRequests.Types

    let create (decimal: decimal) =
        let shifted = decimal * 10M
        let whole = System.Math.Truncate(shifted)
        let remainder = whole % 10M

        operation {
            match (shifted = whole, remainder) with
            | (true, 0M) -> return (Hour(System.Math.Truncate(decimal) |> int), Full)
            | (true, 5M) -> return (Hour(System.Math.Truncate(decimal) |> int), Half)
            | _ -> return! ValidationError (OutOfRange, ValidationMessage "Invalid duration specified")
        }

[<RequireQualifiedAccess>]
module HolidayDate =
    open SeedProject.Domain.AbsenceRequests.Types

    let create date isHalfDay =
        if isHalfDay then
            HalfDay date
        else
            FullDay date

    let extract date =
        match date with
            | FullDay date -> date, false
            | HalfDay date -> date, true

[<RequireQualifiedAccess>]
module HolidayDatePair =
    open SeedProject.Infrastructure.Common
    open SeedProject.Infrastructure
    open SeedProject.Domain.AbsenceRequests.Types

    let create startDate isHalfStart endDate isHalfEnd =
        operation {
            match (startDate, endDate) with
            | s,e when s <= e ->
                return { HolidayDatePair.Start = (HolidayDate.create s isHalfStart); End = (HolidayDate.create e isHalfEnd) }
            | _ -> return! ValidationError (OutOfRange, ValidationMessage "Invalid date pair specified")
        }

[<RequireQualifiedAccess>]
module DatabaseId =
    open FSharp.Control.Tasks

    open SeedProject.Infrastructure.Common
    open SeedProject.Infrastructure

    let create (id: int) =
        operation {
            match id with
            | i when i > 0 -> return Id i
            | _ -> return! ValidationError (OutOfRange, ValidationMessage "Invalid database id specified")
        }

    let createAsync (id: int) =
        task {
            return create id
        }

[<RequireQualifiedAccess>]
module Description =
    let private T = SeedProject.Domain.AbsenceRequests.Types.Description

    let create (value: string) =
        match value with
        | null -> T (None)
        | _ -> T (Some value)