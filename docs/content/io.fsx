(*** hide ***)
#I "../../src/FSharp.Control.Reactive.IO/bin/Release/net46"
#r "System.Reactive.dll"
#r "FSharp.Control.Reactive.dll"
#r "FSharp.Control.Reactive.IO.dll"
open System.IO
open FSharp.Control.Reactive
open FSharp.Control.Reactive.IO


(** 
# Reactive IO

The `FSharp.Control.Reactive.IO` package contains wrappers for common .NET IO classes so it can be used in an reactive manner.
An example of this is the `System.IO.File` class for reading/writing to files. This IO package wraps some of its API so it can be used in a more reactive and fluent manner.

## Reactive Files

Some exmples of how changes in the files can be identified:
- `File.created` : exposes an `IObservable<_>` that emits notifications whether a new file is created.
- ``File.modified` : exposes an `IObservable<_>` that emits notifications whether a existing file is modified.

When subscribing to an observable, the IO package also has functionality to access files in a more fluent manner:
- `File.create` : subscribes to the given `IObservable<_>` and creates a new file (or overrides) on each emit.
- `File.append` : subscribes to the given `IObservable<_>` and appends to a exsiting file on each emit.

Together, they can be used to make the input and output of observable sequences more easy to set up. *)

let input = @".\in"
let output = @".\out"

File.createdAny input
|> Observable.log "file: " 
|> File.create 
    (fun x -> Path.Combine (output, x.Name))
    (fun x -> File.OpenRead x.FullPath)

(** 
## Reactive Http

Exposing an HTTP endpoint in a reactive manner is also available in the IO package. The only thing you need to specify is the host enpdoint: *)

Http.receive "http://localhost:9090/"
|> Observable.log "http receive: "
|> Observable.subscribe (fun ctx ->
    ctx.Response.StatusCode <- 200
    ctx.Response.Close ())

(**
Sending HTTP requests in a reactive manner can also be done easily: *)

Http.get "http://localhost:9090/"
|> Observable.log "http get: "
|> Observable.subscribe (fun r ->
    printfn "Status Code %A" r.StatusCode
    r.Close ())