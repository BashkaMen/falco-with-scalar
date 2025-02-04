module Api.Endpoints.AuthByGoogle

open System
open Api.Core
open Api.Storage
open Google.Apis.Auth
open Microsoft.Extensions.Logging
open Thoth.Json.Net

type Request = { google_token: string }
type Response = { jwt_token: string; roles: Role Set }

let decoder: Decoder<Request> = Decode.object ^ fun x -> {
    google_token = x.Required.Field "google_token" Decode.string
}

let rolesEncoder =
    Seq.map Role.toString
    >> Encode.seqOf Encode.string

let encoder : Encoder<Response> = fun x -> Encode.object [
    "jwt_token", x.jwt_token |> Encode.string  
    "roles",     x.roles |> rolesEncoder
]

let private mkJwt (x: AppUser) : string = $"todo token - {x.id}"



let handler
    (env: {| logger: ILogger<Request * Response>
             storage: IUserStorage |})
    (req: Request) = task {
    let! res = GoogleJsonWebSignature.ValidateAsync(req.google_token)
    
    let email = res.Email |> Email.fromString
    
    match! env.storage.FindByEmail(email) with
    | Some user -> return { jwt_token = mkJwt user; roles = user.roles }
    | None ->
        let roles = Set.ofList [ RegularUser ]
        let user = AppUser.createNew email roles 3
        env.logger.LogInformation("New user created {@User}", user)
        
        do! env.storage.Save(user)
        do! env.storage.Commit()
        return { jwt_token = mkJwt user; roles = user.roles }
}

let endpoint = Endpoint.jsonBody decoder encoder handler