(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"
#r "System.Reactive.Core.dll"
#r "System.Reactive.Interfaces.dll"
#r "System.Reactive.Linq.dll"
#r "System.Reactive.Providers.dll"
#r "System.Reactive.PlatformServices.dll"
#r "System.Reactive.Experimental.dll"
#r "FSharp.Control.Reactive.dll"
open System
open System.Reactive.Linq
open FSharp.Control.Reactive
open Builders

(**
Getting Started
===================

The easiest way to get started using FSharp.Control.Reactive is to take a look at the [tests](https://github.com/fsprojects/FSharp.Control.Reactive/blob/master/tests/ObservableSpecs.fs). In general, you can consider the `Observable` module as providing a set of extensions to the built-in `Observable` module.

`Observable` Module
-------------------

As a first, simple example, the built-in module does not include a `zip` operator, but FSharp.Control.Reactive fills in this gap:
*)

let tuple x y = x,y
let obs1 = Observable.Return 1
let obs2 = Observable.Return "A"
Observable.zip obs1 obs2 tuple
|> Observable.subscribe (printfn "%A")

(**
Computation Expressions
-------------------

The provided computation expressions open new approaches for constructing and combining `Observable` computations. The `observe` computation expression provides a simple workflow using the essential LINQ operators:
*)

let rec generate x =
    observe {
        yield x
        if x < 100000 then
            yield! g (x + 1) }
generate 5
|> Observable.subscribeWithCallbacks ignore ignore ignore
|> ignore

(**
You can find a few additional examples of both the `observe` and `rxquery` computation expressions [on the soruce blog post](http://mnajder.blogspot.com/2011/09/when-reactive-framework-meets-f-30.html).

Reactive Extensions
-------------------

For more information on Rx, check out the [Rx Workshop on Channel 9](http://channel9.msdn.com/series/Rx-Workshop). The examples provided in the workshop should be easy to port to FSharp.Control.Reactive. The [Beginner's Guide to the Reactive Extensions](http://msdn.microsoft.com/en-us/data/gg577611.aspx) includes many other links to excellent content.

TODO: Continue tutorial with examples from the above, including use of `rxquery`.
 *)
