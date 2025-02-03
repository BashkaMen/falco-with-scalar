open Api
open Falco
open Microsoft.AspNetCore.Builder
open Falco.OpenApi.FalcoOpenApiExtensions
open Microsoft.Extensions.DependencyInjection
open Scalar.AspNetCore

let (!) x = ignore x
let builder = WebApplication.CreateBuilder()

!builder.Services
    .AddOpenApi()
    .AddFalcoOpenApi()
    .AddMemoryCache()
        

let wapp = builder.Build()

!wapp.MapOpenApi()
!wapp.MapScalarApiReference()

wapp.UseRouting()
    .UseFalco(Router.endpoints)
    .Run()
