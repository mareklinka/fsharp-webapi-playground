open SeedProject.Performance.Test

open BenchmarkDotNet.Running

[<EntryPoint>]
let main argv =
    match argv |> Seq.contains "-baseline" with
    | true ->
        BenchmarkRunner.Run<AbsenceRequestApiBenchmark>(new BenchmarkConfig())
        |> ignore

        0
    | false ->
        let (totalDegradations, setDifference) =
            BenchmarkComparison.runBenchmarkWithComparison
                (fun () ->
                    BenchmarkRunner.Run<AbsenceRequestApiBenchmark>(new BenchmarkConfig())
                    |> ignore)

        match (totalDegradations, setDifference) with
        | 0, false -> 0
        | 0, true -> -1
        | _ -> totalDegradations
