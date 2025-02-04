namespace Api.Core

open System

type Role =
    | Admin
    | RegularUser

type Email = Email of string

type AppUser =
    { id: Guid
      email: Email
      roles: Set<Role>
      credits: int
      created_at: DateTimeOffset }

module Role =
    let toString = function
        | Admin -> "admin"
        | RegularUser -> "regular_user"

module Email =
    let fromString = String.toLower >> Email
    let value (Email x) = x

module AppUser =
    let createNew email roles credits =
        { id = Guid.CreateVersion7()
          email = email
          roles = roles
          credits = credits
          created_at = DateTimeOffset.UtcNow }
