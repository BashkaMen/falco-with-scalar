[<AutoOpen>]
module Prelude

open System.Threading.Tasks
open Thoth.Json.Net

let inline (^) f x = f x

module Encode =
    let listOf encoder = List.map encoder >> Encode.list


module Option =
    
    let mapAsync aMap opt = task {
        match opt with
        | None -> return None
        | Some x ->
            let! mapped = aMap x
            return Some mapped
    }
    

module Task =
    let ignore tsk = task {
        let! res = tsk
        return ()
    }