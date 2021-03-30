namespace SeedProject.Host

open Giraffe.Core
open Giraffe.EndpointRouting
open SeedProject.Host.Handlers.AbsenceRequests

[<RequireQualifiedAccess>]
module Routing =
    let routes =
        [ subRoute
              "/api"
              [ subRoute
                    "/absencerequest"
                    [ GET [ routef "/%i" GetRequest.handler ]
                      PATCH [ routef
                                  "/%i"
                                  (UpdateRequest.handler
                                   >> bindJson<UpdateRequest.UpdateDataInputModel>) ] ] ] ]
