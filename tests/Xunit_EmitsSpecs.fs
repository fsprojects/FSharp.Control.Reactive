module FSharp.Reactive.Tests.Xunit_EmitsSpecs

open Xunit
open FsCheck
open FSharp.Control.Reactive
open FSharp.Control.Reactive.Testing

#nowarn "988"


type EmitTest () = 
    
    //[<SetUp>]
    //member __.Setup () =
    do    Arb.register<GenTestNotification> () |> ignore

    [<Fact>]
    member __.``Notifications gets passed through`` () =
        Check.QuickThrowOnFailure <| fun ms ->
                TestSchedule.usage <| fun sch ->
                    TestSchedule.coldObservable sch ms
                    |> TestSchedule.subscribeTestObserverStart sch
                    |> TestObserver.messages = ms

    [<Fact>]
    member __.``Functor Law of Observables`` () =
        Check.QuickThrowOnFailure <| 
            fun ms (f : int -> int) (g : int -> int) ->
                TestSchedule.usage <| fun sch ->
                    TestSchedule.hotObservable sch ms
                    |> Observable.retry
                    |> Observable.map f
                    |> Observable.map g
                    |> TestSchedule.subscribeTestObserverStart sch
                    |> TestObserver.nexts = TestNotification.mapNexts (f >> g) ms

    [<Fact>]
    member __.``Applicative Law of Observables`` () =
        Check.QuickThrowOnFailure <| 
            fun ms (f : int -> int) ->
            TestSchedule.usage <| fun sch ->
                TestSchedule.hotObservable sch ms
                |> Observable.retry
                |> Observable.apply (Observable.single f)
                |> TestSchedule.subscribeTestObserverStart sch
                |> TestObserver.nexts = TestNotification.mapNexts f ms

    [<Fact>]
    member __.``Monadic Law of Observables`` () =
        Check.QuickThrowOnFailure <|
            fun ms (f : int -> int) ->
                TestSchedule.usage <| fun sch ->
                    TestSchedule.hotObservable sch ms
                    |> Observable.retry
                    |> Observable.bind (f >> Observable.single)
                    |> TestSchedule.subscribeTestObserverStart sch
                    |> TestObserver.nexts = TestNotification.mapNexts f ms


