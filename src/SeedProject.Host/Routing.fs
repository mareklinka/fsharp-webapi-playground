namespace SeedProject.Host
open Giraffe

[<RequireQualifiedAccess>]
module Routing =
    let webApp : HttpHandler =
      choose [
          subRouteCi "/api"
            (choose [
              subRouteCi "/AbsenceRequest"
                (choose [
                  GET >=> routef "/%i" (fun id -> Handlers.AbsenceRequests.GetRequest.handler id)
                  PATCH >=> routef "/%i" (fun id ->
                    bindJson<Handlers.AbsenceRequests.UpdateRequest.UpdateDataInputModel> (fun model ->
                      Handlers.AbsenceRequests.UpdateRequest.handler id model))
                ])
            ])
      ]
