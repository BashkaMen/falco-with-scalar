module Api.Endpoints.AuthByGoogle

open System
open Api.Storage
open Falco
open Microsoft.Extensions.Logging
open Thoth.Json.Net

type Request = {  google_token: string }
type Response = {  jwt_token: string; roles: string list }

let decoder: Decoder<Request> = Decode.object ^ fun x -> {
    google_token = x.Required.Field "google_token" Decode.string
}

let encoder : Encoder<Response> = fun x -> Encode.object [
    "jwt_token", Encode.string x.jwt_token 
    "roles",     Encode.listOf Encode.string x.roles 
]

let handler
    (env: {| logger: ILogger<Request * Response>
             storage: IUserStorage |})
    (req: Request) = task {
    env.logger.LogInformation("Google token: {google_token}", req.google_token)
    let email = "sbmbash@gmail.com"
    match! env.storage.FindByEmail(email) with
    | Some user -> return { jwt_token = $"token for {user.id}"; roles = ["admin"; "user"] }
    | None ->
        let user = { id = Guid.CreateVersion7()
                     email = email
                     credits = 3
                     created_at = DateTimeOffset.Now }
        
        do! env.storage.Save(user)
        do! env.storage.Commit()
        return { jwt_token = $"token for new {user.id}"; roles = ["admin"; "user"] }
} 

let endpoint = Endpoint.jsonBody decoder encoder handler