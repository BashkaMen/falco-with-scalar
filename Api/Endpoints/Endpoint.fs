module Api.Endpoints.Endpoint

open System.Text
open System.Threading.Tasks
open Falco
open Falco.OpenApi
open Falco.Routing
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Thoth.Json.Net

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

type ErrorResponse = { message: string }

let jsonBody<'req, 'res, 'env> decoder encoder
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
        Return = typeof<ErrorResponse>
        Status = 400
        ContentTypes = [ "application/json" ]
    } 

