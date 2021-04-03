open SeedProject.Performance.Test

open BenchmarkDotNet.Running
open System.Reflection
open System.IO
open System
open System.Globalization

type CommandLineArgs =
    { DurationThreshold: decimal
      AllocationThreshold: decimal
      IsBaseline: bool }

let (|CIEquals|_|) (str: string) arg =
    if String.Equals(str, arg, StringComparison.OrdinalIgnoreCase) then
        Some()
    else
        None

let parseArguments args =
    let rec parseArgumentsRec args acc unknown =
        match args with
        | [] -> acc, unknown
        | CIEquals "-b"::tail
        | CIEquals "--baseline"::tail ->
            parseArgumentsRec tail { acc with IsBaseline = true } unknown
        | CIEquals "-at" :: value :: tail
        | CIEquals "--allocationThreshold" :: value :: tail ->
            match Decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture) with
            | true, value -> parseArgumentsRec tail { acc with AllocationThreshold = value } unknown
            | _ ->
                printfn "Value %s requires an unsigned decimal number in international format" value
                parseArgumentsRec tail acc (unknown @ [ value ])
        | CIEquals "-dt" :: value :: tail
        | CIEquals "--durationThreshold" :: value :: tail ->
            match Decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture) with
            | true, value -> parseArgumentsRec tail { acc with DurationThreshold = value } unknown
            | _ ->
                printfn "Value %s requires an unsigned decimal number in international format" value
                parseArgumentsRec tail acc (unknown @ [ value ])
        | head :: tail -> parseArgumentsRec tail acc (unknown @ [ head ])

    parseArgumentsRec
        args
        { AllocationThreshold = 1.2M
          DurationThreshold = 1.2M
          IsBaseline = false }
        []

[<EntryPoint>]
let main argv =
    let (settings, unknownArguments) =
        parseArguments (argv |> List.ofArray)

    let artifactsPath =
        Path.Combine(Directory.GetCurrentDirectory(), "benchmark")

    let baselinePath =
        Path.Combine(Directory.GetCurrentDirectory(), "benchmark\\baseline")

    let resultsPath = Path.Combine(artifactsPath, "results")

    match settings.IsBaseline with
    | true ->
        BenchmarkSwitcher
            .FromAssembly(Assembly.GetEntryAssembly())
            .Run(
                unknownArguments |> Array.ofList,
                new BenchmarkConfig(artifactsPath)
            )
        |> ignore

        match Directory.Exists baselinePath with
        | true -> Directory.Delete(baselinePath, recursive = true)
        | false -> ()

        Directory.Move(resultsPath, baselinePath)

        0
    | false ->
        let (totalDegradations, setDifference) =
            BenchmarkComparison.runBenchmarkWithComparison
                baselinePath
                resultsPath
                settings.DurationThreshold
                settings.AllocationThreshold
                (fun () ->
                    BenchmarkRunner.Run(Assembly.GetEntryAssembly(), new BenchmarkConfig(artifactsPath))
                    |> ignore)

        match (totalDegradations, setDifference) with
        | 0, false -> 0
        | 0, true -> -1
        | _ -> totalDegradations
