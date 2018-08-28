module FSharp.Reactive.Tests.Xunit_SubjectSpecs

open Xunit
open FsCheck
open FSharp.Control.Reactive
open FSharp.Control.Reactive.Testing

[<Fact>]
let ``Broadcast Subject broadcast to all observers`` () =
    Check.QuickThrowOnFailure <| fun (xs : int list) ->
        TestSchedule.usage <| fun sch ->
            use s = Subject.broadcast
            let observer = TestSchedule.subscribeTestObserver sch s

            Subject.onNexts xs s 
            |> Subject.onCompleted 
            |> ignore

            TestObserver.nexts observer = xs

[<Fact>]
let ``Async Subject emits only last value of asnychronous operation`` () =
    Check.QuickThrowOnFailure <| fun (NonEmptyArray xs : NonEmptyArray<int>) ->
        TestSchedule.usage <| fun sch ->
            use s = Subject.async
            Subject.onNexts xs s
            |> Subject.onCompleted
            |> TestSchedule.subscribeTestObserver sch
            |> TestObserver.nexts
            |> (=) [Array.last xs]

[<Fact>]
let ``Behavior Subject remembers last emited value for next observers`` () =
    Check.QuickThrowOnFailure <| fun (x : int) (y : int) ->
        TestSchedule.usage <| fun sch ->
            use s = Subject.behavior x
            let before, after = 
                TestSchedule.subscribeBeforeAfter 
                    sch s (Subject.onNext y)
            
            Subject.onCompleted s |> ignore
            (TestObserver.nexts before = [x; y]) |@ "Subscribe before 'OnNexts'" .&.
            (TestObserver.nexts after = [y]) |@ "Subscribe after 'OnNexts'"

[<Fact>]
let ``Replay Subject re-emits notificatiosn to future observers`` () =
    Check.QuickThrowOnFailure <| fun (xs : int list) ->
        TestSchedule.usage <| fun sch ->
            use s = Subject.replay
            let before, after =
                TestSchedule.subscribeBeforeAfter
                    sch s (Subject.onNexts xs >> Subject.onCompleted)
        
            TestObserver.nexts before = TestObserver.nexts after
