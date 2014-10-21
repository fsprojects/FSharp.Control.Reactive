module FSharp.Reactive.Tests.ObservableSpecs

open System
open System.Reactive.Linq
open FSharp.Control.Reactive
open Builders
open NUnit.Framework
open Microsoft.Reactive.Testing
open System.Reactive.Subjects

let ``should be`` expectedNext expectedError expectedCompleted (observable:'a IObservable) =
    let next = ref 0
    let error = ref false
    let completed = ref false

    let subscription = observable |> Observable.subscribeWithCallbacks (fun _ -> incr next) (fun _ -> error := true) (fun () -> completed := true)

    Assert.That(!next, Is.EqualTo expectedNext)
    Assert.That(!error, Is.EqualTo expectedError)
    Assert.That(!completed, Is.EqualTo expectedCompleted)

let tuple x y = x,y

[<Test>]
let ``When subscribing to a single value observable, OnNext and OnCompleted should be fired``() =
    Observable.Return(1) |> ``should be`` 1 false true

[<Test>]
let ``When subscribing to an empty observable, only OnCompleted should be fired``() =
    Observable.Empty() |> ``should be`` 0 false true

[<Test>]
let ``When subscribing to an observable that fires an exception, only OnError should be fired``() =
    Observable.Throw(Exception()) |> ``should be`` 0 true false

[<Test>]
let ``When subscribing to an F# event, only OnNext should be called``() =
    let next = ref 0
    let error = ref false
    let completed = ref false
    let testEvent = Event<EventHandler, EventArgs>()

    let subscription = testEvent.Publish.Subscribe((fun _ -> incr next), (fun _ -> error := true), (fun () -> completed := true))
    testEvent.Trigger(null, EventArgs())

    Assert.That(!next, Is.EqualTo 1)
    Assert.That(!error, Is.False)
    Assert.That(!completed, Is.False)

type TestType() =
    let testEvent = Event<EventHandler, EventArgs>()
    [<CLIEvent>] member this.TestEvent = testEvent.Publish
    member this.Trigger() = testEvent.Trigger(this, EventArgs())

[<Test>]
let ``When subscribing to an event, only OnNext should be fired once.``() =
    let next = ref 0
    let error = ref false
    let completed = ref false

    let tester = TestType()
//    let subscription = tester.TestEvent.Subscribe((fun _ -> incr next), (fun _ -> error := true), (fun () -> completed := true))
    let observable = Observable.fromEventPattern "TestEvent" tester
    let subscription = observable.Subscribe((fun _ -> incr next), (fun _ -> error := true), (fun () -> completed := true))
    tester.Trigger()

    Assert.That(!next, Is.EqualTo 1)
    Assert.That(!error, Is.False)
    Assert.That(!completed, Is.False)

[<Test>]
let ``When subscribing to an observable that fires an exception using the ObservableBuilder, only OnError should be fired``() =
    let builder = observe {
        failwith "Test"
        return 1 }
    builder |> ``should be`` 0 true false

[<Test>]
let ``When zip is defined with the applicative, it should match the result of Observable.zip``() =
    let inline (<*>) f m = Observable.apply f m
    let inline (<!>) f m = Observable.map f m
    let a = Observable.Return 1
    let b = Observable.Return 2
    let zip a b = tuple <!> a <*> b

    let actual = ref (0,0)
    let expected = ref (0,0)

    (zip a b).Subscribe(fun x -> actual := x) |> ignore
    (Observable.zip a b tuple).Subscribe(fun x -> expected := x) |> ignore

    Assert.That(!actual, Is.EqualTo (!expected))

[<Test>]
let ``Test should show the stack overflow is fixed with Rx 2 beta``() =
    let test() =
        let rec g x = observe {
            yield x
            if x < 100000 then
                yield! g (x + 1) }
        g 5 |> Observable.subscribeWithCallbacks ignore ignore ignore |> ignore
    Assert.DoesNotThrow(TestDelegate(fun () -> test()))

[<Test>]
let ``Zipping two observable sequences of different types creates a single zipped observable``() =
    let obs1 = Observable.Return 1
    let obs2 = Observable.Return "A"
    let zipped = Observable.zip obs1 obs2 tuple
    let result = zipped |> Observable.First
    let expected = ( 1, "A" )

    Assert.That(result, Is.EqualTo expected)

[<Test>]
let ``distinctKey uses the key function to decide whether an element has been seen before``() =
    let result   = ResizeArray()
    let obs      = Observable.ofSeq [ (1,3); (2,5); (2,7); (1,6); (3,5) ]
    let expected = [ (1,3); (2,5); (3,5) ]
    obs |> Observable.distinctKey fst
        |> Observable.subscribe(result.Add) 
        |> ignore

    Assert.That(result, Is.EqualTo expected)

[<Test>]
let ``distinctKeyCompare uses the key function and the comparer to decide whether an element has been seen before``() =
    let comparer = { new System.Collections.Generic.IEqualityComparer<int> with
                        member x.Equals(a,b)    = a % 2 = b % 2
                        member x.GetHashCode(a) = a % 2 } // Equivalence based on odd or even
    let result   = ResizeArray()
    let obs      = Observable.ofSeq [ (1,3); (2,5); (2,7); (1,6); (3,5) ]
    let expected = [ (1,3); (2,5) ]
    obs |> Observable.distinctKeyCompare fst comparer
        |> Observable.subscribe(result.Add) 
        |> ignore

    Assert.That(result, Is.EqualTo expected)

[<Test>]
let ``distinctUntilChangedKey uses the key function to decide whether an element has been seen before``() =
    let result   = ResizeArray()
    let obs      = Observable.ofSeq [ (1,3); (2,5); (2,7); (1,6); (3,5) ]
    let expected = [ (1,3); (2,5); (1,6); (3,5) ]
    obs |> Observable.distinctUntilChangedKey fst
        |> Observable.subscribe(result.Add) 
        |> ignore

    Assert.That(result, Is.EqualTo expected)

[<Test>]
let ``distinctUntilChangedKeyCompare uses the key function and the comparer to decide whether an element has been seen before``() =
    let comparer = { new System.Collections.Generic.IEqualityComparer<int> with
                        member x.Equals(a,b)    = a % 2 = b % 2
                        member x.GetHashCode(a) = a % 2 }  // Equivalence based on odd or even
    let result   = ResizeArray()
    let obs      = Observable.ofSeq [ (1,3); (2,5); (2,7); (1,6); (3,5) ]
    let expected = [ (1,3); (2,5); (1,6) ]
    obs |> Observable.distinctUntilChangedKeyCompare fst comparer
        |> Observable.subscribe(result.Add) 
        |> ignore

    Assert.That(result, Is.EqualTo expected)

[<Test>]
let ``ofSeqOn enumerates its sequence on the specified scheduler``() =
    let result    = ResizeArray()
    let items     = [ 1; 2; 3]
    let scheduler = TestScheduler()

    let obs       = items |> Observable.ofSeqOn scheduler
    obs |> Observable.subscribe(result.Add) 
        |> ignore

    Assert.That(result, Is.Empty)
    scheduler.Start()
    Assert.That(result, Is.EqualTo items)

[<Test>]
let ``combineLatest calls map function with pairs of latest values``() =
    let result   = ResizeArray()
    use obs1     = new Subject<int>()
    use obs2     = new Subject<int>()
    let map x y  = x + (y / 2)
    Observable.combineLatest map obs1 obs2
        |> Observable.subscribe(result.Add) 
        |> ignore

    Assert.That(result, Is.Empty)
    obs1.OnNext 1
    Assert.That(result, Is.Empty)
    obs1.OnNext 2
    Assert.That(result, Is.Empty)
    obs2.OnNext 10
    Assert.That(result, Is.EqualTo [ 7 ] )
    obs2.OnNext 20                
    Assert.That(result, Is.EqualTo [ 7; 12 ] )
    obs1.OnNext 3                 
    Assert.That(result, Is.EqualTo [ 7; 12; 13 ] )

[<Test>]
let ``combineLatestArray produces arrays of latest values``() =
    let result   = ResizeArray()
    use obs1     = new Subject<int>()
    use obs2     = new Subject<int>()
    Observable.combineLatestArray [| obs1; obs2 |]
        |> Observable.subscribe(result.Add) 
        |> ignore

    Assert.That(result, Is.Empty)
    obs1.OnNext 1
    Assert.That(result, Is.Empty)
    obs1.OnNext 2
    Assert.That(result, Is.Empty)
    obs2.OnNext 10
    Assert.That(result, Is.EqualTo [ [| 2; 10 |] ] )
    obs2.OnNext 20                
    Assert.That(result, Is.EqualTo [ [| 2; 10 |]; [| 2; 20 |] ] )
    obs1.OnNext 3                 
    Assert.That(result, Is.EqualTo [ [| 2; 10 |]; [| 2; 20 |]; [| 3; 20 |] ] )

[<Test>]
let ``combineLatestSeq produces lists of latest values``() =
    let result   = ResizeArray()
    use obs1     = new Subject<int>()
    use obs2     = new Subject<int>()
    let sources  = [ obs1 :> IObservable<_>; obs2 :> IObservable<_>] |> Seq.ofList
    Observable.combineLatestSeq sources
        |> Observable.subscribe(result.Add) 
        |> ignore

    Assert.That(result, Is.Empty)
    obs1.OnNext 1
    Assert.That(result, Is.Empty)
    obs1.OnNext 2
    Assert.That(result, Is.Empty)
    obs2.OnNext 10
    Assert.That(result, Is.EqualTo [ [2; 10] ] )
    obs2.OnNext 20                
    Assert.That(result, Is.EqualTo [ [2; 10]; [2; 20] ] )
    obs1.OnNext 3                 
    Assert.That(result, Is.EqualTo [ [2; 10]; [2; 20]; [3; 20] ] )

[<Test>]
let ``combineLatestSeqMap applies map function to latest values``() =
    let result   = ResizeArray()
    use obs1     = new Subject<int>()
    use obs2     = new Subject<int>()
    let sources  = [ obs1 :> IObservable<_>; obs2 :> IObservable<_>] |> Seq.ofList
    let map xs   = xs |> Seq.sum
    Observable.combineLatestSeqMap map sources
        |> Observable.subscribe(result.Add) 
        |> ignore

    Assert.That(result, Is.Empty)
    obs1.OnNext 1
    Assert.That(result, Is.Empty)
    obs1.OnNext 2
    Assert.That(result, Is.Empty)
    obs2.OnNext 10
    Assert.That(result, Is.EqualTo [ 12 ] )
    obs2.OnNext 20                
    Assert.That(result, Is.EqualTo [ 12; 22 ] )
    obs1.OnNext 3                 
    Assert.That(result, Is.EqualTo [ 12; 22; 23 ] )

