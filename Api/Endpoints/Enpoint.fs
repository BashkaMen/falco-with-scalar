module Api.Endpoints.Enpoint

open System.Data
open System.Text
open System.Threading.Tasks
open Falco
open Falco.OpenApi
open Falco.Routing
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Microsoft.FSharp.Core
open Thoth.Json.Net

type AppError =
    | InvalidData of string

type IHandler<'request, 'response> =
    abstract member Handle : req:'request -> Task<'response>
    abstract member Bind : ctx:HttpContext -> Task<'request>
    abstract member Write : res:'response -> ctx:HttpContext -> Task
    
    
let handle<'req, 'res>
    (binder: HttpContext -> Task<'req>)
    (writer: 'res -> HttpContext -> Task)
    : HttpHandler = fun ctx -> task {
        let handler = ctx.Plug<IHandler<'req, 'res>>()
        let! req = binder ctx
        let! res = handler.Handle(req)
        do! writer res ctx
    }

let bodyBinder<'body> (decoder: Decoder<'body>) = fun ctx -> task {
    let! json = Request.getBodyString ctx
    return Decode.unsafeFromString decoder json
}

let bodyWriter (encoder: Encoder<_>) body ctx : Task = task {
    let json = Encode.toString 1 (encoder body)
    return! Response.ofString Encoding.UTF8 json ctx
}

let jsonEndpoint<'req, 'res> path decoder encoder =
    let binder = bodyBinder<'req> decoder
    let writer = bodyWriter encoder
    
    post path (handle<'req, 'res> binder writer)
    |> OpenApi.name $"POST {path}"
    |> OpenApi.acceptsType typeof<'req>
    |> OpenApi.returnType typeof<'res>
    |> OpenApi.summary "{super json }"

[<AbstractClass>]
type JsonHandler<'req, 'res>(decoder, encoder) =
    abstract Bind : ctx:HttpContext -> Task<'req>
    abstract Handle : req:'req -> Task<'res>
    abstract Write : res:'res -> ctx:HttpContext -> Task
    
    default this.Bind(ctx)= bodyBinder decoder ctx
    default this.Write res ctx = bodyWriter encoder res ctx
    
    interface IHandler<'req, 'res> with
        member this.Bind(ctx) = this.Bind ctx
        member this.Handle(req) = this.Handle req
        member this.Write res ctx = this.Write res ctx

let inline (^) f x= f x

module AuthByGoogle =
    
    module Encode =
        let listOf encoder = List.map encoder >> Encode.list
    
    type Request = { google_token: string }
    type Response = { jwt_token: string; roles: string list }
    
    let decoder: Decoder<Request> = Decode.object ^ fun x -> {
        google_token = x.Required.Field "google_token" Decode.string
    }
    
    let encoder : Encoder<Response> = fun x -> Encode.object [
        "jwt_token", Encode.string x.jwt_token 
        "roles",     Encode.listOf Encode.string x.roles 
    ]
    
    let endpoint = jsonEndpoint "/auth/google" decoder encoder
    
    type Handler() =
        inherit JsonHandler<Request, Response>(decoder, encoder)

        override this.Handle(req) = task {
            return { jwt_token = "token"; roles = ["admin"; "user"] }
        }
        
        override this.Bind(ctx : HttpContext) = task {
            let token = ctx.Request.Headers["x-token"]
            return { google_token = string token }
        }