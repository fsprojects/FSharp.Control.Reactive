module FSharp.Reactive.Tests.EmitsSpecs

open NUnit.Framework
open FsCheck
open FSharp.Control.Reactive
open FSharp.Control.Reactive.Testing

[<TestFixture>]
type EmitTest () = 
    
    [<SetUp>]
    member __.Setup () =
        Arb.register<GenTestNotification> () |> ignore

    [<Test>]
    member __.``Notifications gets passed through`` () =
        Check.QuickThrowOnFailure <| fun ms ->
                TestSchedule.usage <| fun sch ->
                    TestSchedule.coldObservable sch ms
                    |> TestSchedule.subscribeTestObserverStart sch
                    |> TestObserver.all = ms

    [<Test>]
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

    [<Test>]
    member __.``Applicative Law of Observables`` () =
        Check.QuickThrowOnFailure <| 
            fun ms (f : int -> int) ->
            TestSchedule.usage <| fun sch ->
                TestSchedule.hotObservable sch ms
                |> Observable.retry
                |> Observable.apply (Observable.single f)
                |> TestSchedule.subscribeTestObserverStart sch
                |> TestObserver.nexts = TestNotification.mapNexts f ms

    [<Test>]
    member __.``Monadic Law of Observables`` () =
        Check.QuickThrowOnFailure <|
            fun ms (f : int -> int) ->
                TestSchedule.usage <| fun sch ->
                    TestSchedule.hotObservable sch ms
                    |> Observable.retry
                    |> Observable.bind (f >> Observable.single)
                    |> TestSchedule.subscribeTestObserverStart sch
                    |> TestObserver.nexts = TestNotification.mapNexts f ms

        
