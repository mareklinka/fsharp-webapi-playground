namespace SeedProject.Functional

open FsCheck

open SeedProject.Host.Handlers.AbsenceRequests.Types

module Generators =
    let createRequestInputModelGenerator =
        let values =
            Arb.generate<System.DateTime * bool * bool>

        let timeSpanGen = Gen.choose(0, 24 * 100) |> Gen.map (fun x -> System.TimeSpan.FromHours(x |> float))
        let stringGen = Arb.generate<string> |> Gen.filter (fun s -> s <> null)

        let combinedGen = Gen.map3 (fun (s, sHalf, eHalf) timeDelta desc -> (s, sHalf, timeDelta, eHalf, desc)) values timeSpanGen stringGen

        combinedGen
        |> Gen.map
            (fun (s, sHalf, timeDelta, eHalf, desc) ->
                { CreateRequestInputModel.Type = RequestType.Holiday
                  StartDate = Some s
                  EndDate = Some (s.Add(timeDelta))
                  HalfDayStart = Some sHalf
                  HalfDayEnd = Some eHalf
                  Description = Some desc
                  Duration = None
                  PersonalDayType = None })

    type GeneratorRegistry =
        static member CreateRequestInputModel() = Arb.fromGen(createRequestInputModelGenerator)