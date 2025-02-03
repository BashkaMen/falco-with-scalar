module Api.Router

open System
open Falco
open Falco.Routing
open Api.Endpoints

let endpoints =
    [
        get "/" (Response.ofPlainText $"App started at {DateTime.Now}")
        AuthByGoogle.endpoint "/api/auth/by-google"
    ]