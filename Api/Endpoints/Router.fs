module Api.Endpoints.Router

open System
open Falco
open Falco.Routing
open Microsoft.AspNetCore.Builder


let endpoints =
    [
        get "/" (Response.ofPlainText $"App started at {DateTime.Now}")
        FpEndpoint.AuthByGoogle.endpoint "/api/auth/by-google"
        FpEndpoint.CreateTrigger.endpoint "/api/trigger/create"
    ]