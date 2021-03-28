namespace SeedProject.Host

[<RequireQualifiedAccess>]
module Routing =
    let routes =
        [ ("/api/absencerequest/{id:int}", [ "GET" ], Handlers.AbsenceRequests.getRequest);
          ("/api/absencerequest/{id:int}", [ "PATCH" ], Handlers.AbsenceRequests.updateRequest) ]
