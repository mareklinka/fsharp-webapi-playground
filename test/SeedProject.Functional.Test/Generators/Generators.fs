namespace SeedProject.Functional

open FsCheck

open SeedProject.Host.Handlers.AbsenceRequests.Types

module Generators =
    let createRequestInputModelGenerator =
        let values =
            Arb.generate<System.DateTime * bool * bool * string>

        let timeSpanGen = Gen.choose(0, 24 * 100) |> Gen.map (fun x -> System.TimeSpan.FromHours(x |> float))

        let combinedGen = Gen.map2 (fun (s, sHalf, eHalf, desc) timeDelta -> (s, sHalf, timeDelta, eHalf, desc)) values timeSpanGen

        combinedGen
        |> Gen.map
            (fun (s, sHalf, timeDelta, eHalf, desc) ->
                let description =
                    match desc with
                    | null -> None
                    | d -> Some d
                { CreateRequestInputModel.Type = RequestType.Holiday
                  StartDate = Some s
                  EndDate = Some (s.Add(timeDelta))
                  HalfDayStart = Some sHalf
                  HalfDayEnd = Some eHalf
                  Description = description
                  Duration = None
                  PersonalDayType = None })

    type ValidAbsenceRequestCreationModel =
        static member CreateRequestInputModel() = Arb.fromGen(createRequestInputModelGenerator)