namespace SeedProject.Host

open System.Threading.Tasks

open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Http
open Microsoft.Identity.Web

open Giraffe.Core
open Giraffe.EndpointRouting
open Giraffe.Auth

open SeedProject.Host.Handlers.AbsenceRequests
open SeedProject.Infrastructure
open System.Security.Claims
open Microsoft.AspNetCore.Authentication.JwtBearer

module Authentication =
    type TestingAuthenticationHandler(optionsMonitor, logger, encoder, clock) =
        inherit AuthenticationHandler<AuthenticationSchemeOptions>(optionsMonitor, logger, encoder, clock)

        override __.HandleAuthenticateAsync() =
            let defaultClaims = [| new Claim("Name", "Test user"); new Claim(ClaimConstants.Scp, "rmt_client") |]
            let defaultPrincipal = new ClaimsPrincipal(new ClaimsIdentity(defaultClaims, JwtBearerDefaults.AuthenticationScheme, "Name", "Role"))
            let ticket = new AuthenticationTicket(defaultPrincipal, null, JwtBearerDefaults.AuthenticationScheme)

            Task.FromResult(AuthenticateResult.Success(ticket))


module Authorization =
    let clientScopePredicate (context: HttpContext) =
        let scopeClaim =
            context.User.FindFirst(ClaimConstants.Scp)
            |?? lazy (context.User.FindFirst(ClaimConstants.Scope))

        match scopeClaim with
        | Some scope ->
            scope.Value.Split(" ")
            |> Array.contains "rmt_client"
        | _ -> false

[<RequireQualifiedAccess>]
module Routing =
    let requriesProperScope =
        authorizeRequest Authorization.clientScopePredicate (setStatusCode 401)

    let routes =
        [ subRoute
              "/api"
              [ subRoute
                    "/absencerequest"
                    [ GET [ routef "/%i" (fun id -> requriesProperScope >=> GetRequest.handler id) ]
                      GET [ route "" (requriesProperScope >=> GetAllRequests.handler) ]
                      PUT [ route
                                ""
                                (requriesProperScope
                                 >=> (CreateRequest.handler
                                      |> bindJson<Types.CreateRequestInputModel>)) ]
                      PATCH [ routef
                                  "/%i"
                                  (fun id ->
                                      requriesProperScope
                                      >=> (UpdateRequest.handler id
                                           |> bindJson<Types.UpdateRequestInputModel>)) ] ] ] ]
