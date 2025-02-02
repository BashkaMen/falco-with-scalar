module Api.Endpoints.FpEndpoint

open System
open System.Data
open System.Reflection
open System.Runtime.InteropServices.ComTypes
open System.Text
open System.Threading.Tasks
open Falco
open Falco.OpenApi
open Falco.Routing
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Thoth.Json.Net

let inline (^) f x= f x

let resolveEnv<'env> (ctx: HttpContext) : 'env =
    let envType = typeof<'env>
    
    let constructors = envType.GetConstructors()
    let ctor = 
        constructors
        |> Array.sortBy(_.GetParameters().Length)
        |> Array.tryHead
        |> Option.defaultWith (fun () -> 
            failwith $"No suitable constructor found for type {envType.Name}")
    
    let parameters = ctor.GetParameters()
    let args = 
        parameters 
        |> Array.map(fun p -> ctx.RequestServices.GetRequiredService(p.ParameterType))
    
    let envInstance = ctor.Invoke(args)
    
    envInstance :?> 'env

let handle
    (binder: HttpContext -> Task<'req>)
    (writer: 'res -> HttpContext -> Task<unit>)
    (handler: 'env -> 'req -> Task<'res>)
    : HttpHandler = fun ctx -> task {
        let env = resolveEnv<'env>(ctx)
        let! req = binder ctx
        let! res = handler env req
        return! writer res ctx
    }

let bodyBinder<'body> (decoder: Decoder<'body>) = fun ctx -> task {
    let! json = Request.getBodyString ctx
    return Decode.unsafeFromString decoder json
}

let writeJson str =
    Response.withContentType "application/json"
    >> Response.ofString Encoding.UTF8 str

let bodyWriter<'body> (encoder: Encoder<'body>) body ctx  = task {
    let json = Encode.toString 1 (encoder body)
    return! writeJson json ctx
}

let jsonEndpoint<'req, 'res, 'env> decoder encoder
    (handler: 'env -> 'req -> Task<'res>)
    (path: string)
    : HttpEndpoint =
    let handler : HttpHandler = fun ctx -> task {
        let binder = bodyBinder<'req> decoder
        let writer = bodyWriter<'res> encoder
        
        return! handle binder writer handler ctx
    }
    
    post path handler
    |> OpenApi.acceptsType typeof<'req>
    |> OpenApi.returns {
        Return = typeof<'res>
        Status = 200
        ContentTypes = [ "application/json" ]
    }
    |> OpenApi.returns {
        Return = {| text = "error" |}.GetType()
        Status = 400
        ContentTypes = [ "application/json" ]
    } 


module Encode =
    let listOf encoder = List.map encoder >> Encode.list

module AuthByGoogle =
    type Request = {  google_token: string }
    type Response = {  jwt_token: string; roles: string list }
    
    let decoder: Decoder<Request> = Decode.object ^ fun x -> {
        google_token = x.Required.Field "google_token" Decode.string
    }
    
    let encoder : Encoder<Response> = fun x -> Encode.object [
        "jwt_token", Encode.string x.jwt_token 
        "roles",     Encode.listOf Encode.string x.roles 
    ]
    
    let handler (env: {| logger: ILogger<Request> |}) (req: Request) = task {
        env.logger.LogInformation("Google token: {google_token}", req.google_token)
        return { jwt_token = "token"; roles = ["admin"; "user"] }
    } 
    
    let endpoint = jsonEndpoint decoder encoder handler

module CreateTrigger =
    type Request = { name: string }
    type Response =
        | Text of {| text: string |}
        | Button of {| text: string; url: string; icon: string |}
    
    let decoder: Decoder<Request> = Decode.object ^ fun x -> {
        name = x.Required.Field "name" Decode.string 
    }
    
    let encoder : Encoder<Response> = function
        | Button x -> Encode.object [
            "type", Encode.string "button"
            "text", Encode.string x.text 
            "url", Encode.string x.url
            "icon", Encode.string x.icon
            ]
        | Text x -> Encode.object [
            "type", Encode.string "text"
            "text", Encode.string x.text 
            ]
        
    let handler (env: {| logger: ILogger<Request> |}) (req: Request) = task {
        env.logger.LogInformation("Trigger name: {name}", req.name)
        return Button {|  text = "text"; url = "url"; icon = "icon"|}
    } 
    
    let endpoint = jsonEndpoint decoder encoder handler
        