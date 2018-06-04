(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../src/FSharp.Control.Reactive/bin/Release/net46"
#I "../../src/FSharp.Control.Reactive.Testing/bin/Release/net46"
#I "../../packages/build/NUnit/lib/net45/"
#I "../../packages/build/FsCheck"
#r "nunit.framework.dll"
#r "FsCheck.dll"
#r "System.Reactive.dll"
#r "FSharp.Control.Reactive.dll"
#r "FSharp.Control.Reactive.Testing.dll"
#r "Microsoft.Reactive.Testing.dll"



(**
Getting Started
===================

The easiest way to get started using FSharp.Control.Reactive is to take a look at the [tests](https://github.com/fsprojects/FSharp.Control.Reactive/blob/master/tests/ObservableSpecs.fs). In general, you can consider the `Observable` module as providing a set of extensions to the built-in `Observable` module.

Observable Module
-------------------

As a first, simple example, the built-in module does not include a `zip` operator, but FSharp.Control.Reactive fills in this gap:
*)
open FSharp.Control.Reactive

let obs1 = Observable.single 1
let obs2 = Observable.single "A"

Observable.zip obs1 obs2
|> Observable.subscribe (printfn "%A")
|> ignore

(**
Computation Expressions
-------------------

The provided computation expressions open new approaches for constructing and combining `Observable` computations. The `observe` computation expression provides a simple workflow using the essential LINQ operators:
*)
open FSharp.Control.Reactive.Builders

let rec generate x =
    observe {
        yield x
        if x < 100000 then
            yield! generate (x + 1) }
generate 5
|> Observable.subscribeWithCallbacks ignore ignore ignore
|> ignore

(**
You can find a few additional examples of both the `observe` and `rxquery` computation expressions [on the source blog post](http://mnajder.blogspot.com/2011/09/when-reactive-framework-meets-f-30.html).

Reactive Extensions
-------------------

For more information on Rx, check out the [Rx Workshop on Channel 9](http://channel9.msdn.com/series/Rx-Workshop). The examples provided in the workshop should be easy to port to FSharp.Control.Reactive. The [Beginner's Guide to the Reactive Extensions](http://msdn.microsoft.com/en-us/data/gg577611.aspx) includes many other links to excellent content.

 *)

(**
Reactive Testing
-------------------

The Rx.NET has several OOP-minded objects to test Reactive Applications. This package contains some wrappers to make your Reactive Tests more functional.

Following test shows how the Broadcast Subject can be tested:
*)
open NUnit.Framework
open FsCheck
open FSharp.Control.Reactive.Testing

[<Test>]
let ``Broadcast Subject broadcast to all observers`` () =
    Check.QuickThrowOnFailure <| fun (xs : int list) ->
        TestSchedule.usage <| fun sch ->
            use s = Subject.broadcast
            let observer = TestSchedule.subscribeTestObserver sch s

            Subject.onNexts xs s 
            |> Subject.onCompleted 
            |> ignore
            
            TestObserver.nexts observer = xs

(**
- The `TestSchedule.usage` call wil make sure we have a virtual time scheduler that we can use to sync our different observables and observers.
- The `subscribeTestObserver` call will subscribe the given `observable` to a `TestObserver`. 
- This `TestObserver` can be used to assert on the emits that the SUT sends to it by using the `TesteObserver` module. (`TestObserver.nexts`, `TestObserver.all`, `TestObserver.errors`)

The testing package also contains generated `TestNotifications` which we can use to simulate a stream of emits.
This values has to be registered first before using.

Following example shows how the Functor Law can be testsd by creating a Hot Observable of simulated emits. *)

[<SetUp>]
member __.Setup () =
    Arb.register<GenTestNotification> () |> ignore

[<Test>]
member __.``Functor Law of Observables`` () =
    Check.QuickThrowOnFailure <| 
        fun (TestNotifications ms : TestNotifications<int>) (f : int -> int) (g : int -> int) ->
            TestSchedule.usage <| fun sch ->
                TestSchedule.hotObservable sch ms
                |> Observable.retry
                |> Observable.map f
                |> Observable.map g
                |> TestSchedule.subscribeTestObserverStart sch
                |> TestObserver.nexts = TestNotification.mapNexts (f >> g) ms

(**
NOTE: the previous test contains a `Observable.retry` call because the simulated emits will possible contain 'OnError' emits as well.
See: https://github.com/fsprojects/FSharp.Control.Reactive/blob/master/src/FSharp.Control.Reactive.Testing/TestNotifications.fs for more info on how the simulated emits are generated.*)