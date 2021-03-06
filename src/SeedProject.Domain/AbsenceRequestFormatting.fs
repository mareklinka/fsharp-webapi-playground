namespace SeedProject.Domain

open System
open SeedProject.Domain.AbsenceRequests.Types

module Formatter =
    type private TableRow = string * string * string * string * string

    let private concatCells ((col1, col2, col3, col4, col5): TableRow) =
        seq {col1; col2; col3; col4; col5} |> Seq.map (fun c -> sprintf "<td>%s</td>" c) |> String.concat "" |> sprintf "<tr>%s</tr>"

    let private formatDate (date: DateTime) = date.ToString("dd.MM.yyyy")
    let private formatOptionalDate (date: DateTime option) =
        match date with
        | Some d -> d |> formatDate
        | None -> "-"

    let private formatDuration (Hour hour, minute) =
        match minute with
        | Full _ -> hour |> sprintf "%i"
        | Half _ -> hour |> sprintf "%i,5"

    let private formatHolidayDate date =
        match date with
        | FullDay d -> d |> formatDate
        | HalfDay d -> d |> formatDate |> sprintf "%s (half)"

    let private formatPersonalDayType t =
        match t with
        | Wedding -> "Wedding"
        | Childbirth -> "Childbirth"
        | Funeral -> "Funeral"
        | Moving -> "Moving"
        | BloodDonation -> "Blood donation"

    let formatAbsenceRequest r =
        match r with
        | HolidayRequest { Start = s; End = e; Description = Description d } ->
            (   "Holiday",
                s |> formatHolidayDate,
                e |> formatHolidayDate,
                "",
                d |> Option.defaultValue "") |> concatCells
        | PersonalDayRequest { Date = date; Type = t; Description = Description d } ->
            (   t |> formatPersonalDayType |> sprintf "Personal day (%s)",
                date |> formatDate,
                date |> formatDate,
                "",
                d |> Option.defaultValue "") |> concatCells
        | DoctorVisitRequest { Date = date; Duration = duration; Description = Description d } ->
            (   "Doctor visit",
                date |> formatDate,
                date |> formatDate,
                duration |> formatDuration,
                d |> Option.defaultValue "") |> concatCells
        | DoctorVisitWithFamilyRequest { Date = date; Duration = duration; Description = Description d } ->
            (   "Doctor visit with family",
                date |> formatDate,
                date |> formatDate,
                duration |> formatDuration,
                d |> Option.defaultValue "") |> concatCells
        | SickdayRequest { Date = date; Duration = duration; Description = Description d } ->
            (   "Sickday",
                date |> formatDate,
                date |> formatDate,
                duration |> formatDuration,
                d |> Option.defaultValue "") |> concatCells
        | SicknessRequest { Start = s; End = e; Description = Description d } ->
            (   "Sickness",
                s |> formatDate,
                e |> formatOptionalDate,
                "",
                d |> Option.defaultValue "") |> concatCells
        | PandemicSicknessRequest { Start = s; End = e; Description = Description d } ->
            (   "Pandemic Sickness",
                s |> formatDate,
                e |> formatOptionalDate,
                "",
                d |> Option.defaultValue "") |> concatCells