module FSharp.Reactive.Tests.Xunit_ObservableSpecs

open System
open System.Reactive.Linq
open FSharp.Control.Reactive
open Builders
open Xunit
open FsCheck
open Microsoft.Reactive.Testing
open System.Reactive.Subjects
open System.Reactive.Concurrency
open FSharp.Control.Reactive.Observable
open FSharp.Control.Reactive.Testing
open TestNotification
open FSharp.Control.Reactive.Testing.TestNotification
open System.Threading
open System.Collections.Generic


let ``should be`` expectedNext expectedError expectedCompleted (observable:'a IObservable) =
    let next = ref 0
    let error = ref false
    let completed = ref false

    let subscription = observable |> Observable.subscribeWithCallbacks (fun _ -> incr next) (fun _ -> error := true) (fun () -> completed := true)

    Assert.Equal(!next, expectedNext)
    Assert.Equal(!error, expectedError)
    Assert.Equal(!completed, expectedCompleted)

let tuple x y = x,y

[<Fact>]
let ``When subscribing to a single value observable, OnNext and OnCompleted should be fired``() =
    Observable.Return(1) |> ``should be`` 1 false true

[<Fact>]
let ``When subscribing to an empty observable, only OnCompleted should be fired``() =
    Observable.Empty() |> ``should be`` 0 false true

[<Fact>]
let ``When subscribing to an observable that fires an exception, only OnError should be fired``() =
    Observable.Throw(Exception()) |> ``should be`` 0 true false

[<Fact>]
let ``When subscribing to an F# event, only OnNext should be called``() =
    let next = ref 0
    let error = ref false
    let completed = ref false
    let testEvent = Event<EventHandler, EventArgs>()

    let subscription = testEvent.Publish.Subscribe((fun _ -> incr next), (fun _ -> error := true), (fun () -> completed := true))
    testEvent.Trigger(null, EventArgs())

    Assert.Equal(!next, 1)
    Assert.False(!error)
    Assert.False(!completed)

type TestType() =
    let testEvent = Event<EventHandler, EventArgs>()
    [<CLIEvent>] member this.TestEvent = testEvent.Publish
    member this.Trigger() = testEvent.Trigger(this, EventArgs())

[<Fact>]
let ``When subscribing to an event, only OnNext should be fired once.``() =
    let next = ref 0
    let error = ref false
    let completed = ref false

    let tester = TestType()
//    let subscription = tester.TestEvent.Subscribe((fun _ -> incr next), (fun _ -> error := true), (fun () -> completed := true))
    let observable = Observable.fromEventPattern "TestEvent" tester
    let subscription = observable.Subscribe((fun _ -> incr next), (fun _ -> error := true), (fun () -> completed := true))
    tester.Trigger()

    Assert.Equal(!next, 1)
    Assert.False(!error)
    Assert.False(!completed)

[<Fact>]
let ``When subscribing to an observable that fires an exception using the ObservableBuilder, only OnError should be fired``() =
    let builder = observe {
        failwith "Test"
        yield 1 }
    builder |> ``should be`` 0 true false

[<Fact>]
let ``When zip is defined with the applicative, it should match the result of Observable.zip``() =
    let inline (<*>) f m = Observable.apply f m
    let inline (<!>) f m = Observable.map f m
    let a = Observable.Return 1
    let b = Observable.Return 2
    let zip a b = tuple <!> a <*> b

    let actual = ref (0,0)
    let expected = ref (0,0)

    (zip a b).Subscribe(fun x -> actual := x) |> ignore
    (Observable.zip a b).Subscribe(fun x -> expected := x) |> ignore

    Assert.Equal(!actual, (!expected))

[<Fact>]
let ``zip used in rxquery matches output from Observable.zip`` () =
    let a = Observable.ofSeq [1;2;3]
    let b = Observable.ofSeq [1;2;3]

    let expected = ResizeArray()
    (Observable.zip a b).Subscribe(expected.Add) |> ignore

    let actual = ResizeArray()
    (rxquery {
        for x in a do
        zip y in b
        yield x, y
     }).Subscribe(actual.Add) |> ignore

    Assert.True(List.ofSeq actual = List.ofSeq expected)

[<Fact>]
let ``Test should show the stack overflow is fixed with Rx 2 beta``() =
    let test() =
        let rec g x = observe {
            yield x
            if x < 100000 then
                yield! g (x + 1) }
        g 5 |> Observable.subscribeWithCallbacks ignore ignore ignore |> ignore
    //Assert.DoesNotThrow(TestDelegate(fun () -> test()))
    test()
    Assert.True(true)

[<Fact>]
let ``RxQueryBuilder.Head can return first item`` () =
    let test = observe {
        yield 1
        yield 2
        yield 3
        yield 4 }

    let query = rxquery {
        for x in test do
        head }

    query |> Observable.subscribe (fun x -> Assert.Equal(1, x)) |> ignore

[<Fact>]
let ``RxQueryBuilder.ExactlyOne can returns only one item`` () =
    let test = observe { yield 1 }

    let query = rxquery {
        for x in test do
        exactlyOne }

    query |> Observable.subscribe (fun x -> Assert.Equal(1, x)) |> ignore

[<Fact>]
let ``RxQueryBuilder.ExactlyOne throws when source contains more than one item`` () =
    let test = observe {
        yield 1
        yield 2 }

    let query = rxquery {
        for x in test do
        exactlyOne }

    Observable.subscribeWithError
        (fun _ -> Assert.Null("ExactlyOne should throw when source contains more than one element"))
        (fun _ -> Assert.True(true))
        query |> ignore

[<Fact>]
let ``Zipping two observable sequences of different types creates a single zipped observable``() =
    let obs1 = Observable.Return 1
    let obs2 = Observable.Return "A"
    let zipped = Observable.zip obs1 obs2
    let result = zipped |> Observable.Wait
    let expected = ( 1, "A" )

    Assert.Equal(result, expected)

[<Fact>]
let ``ZipWith on two observable sequences of different types creates a single zipped observable``() =
    let obs1 = Observable.Return 1
    let obs2 = Observable.Return "A"
    let zipped = Observable.zipWith tuple obs1 obs2
    let result = zipped |> Observable.Wait
    let expected = ( 1, "A" )

    Assert.Equal(result, expected)


[<Fact>]
let ``distinctKey uses the key function to decide whether an element has been seen before``() =
    let result   = ResizeArray()
    let obs      = Observable.ofSeq [ (1,3); (2,5); (2,7); (1,6); (3,5) ]
    let expected = [ (1,3); (2,5); (3,5) ]
    obs |> Observable.distinctKey fst
        |> Observable.subscribe(result.Add) 
        |> ignore

    Assert.Equal(result, expected)

[<Fact>]
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

    Assert.Equal(result, expected)

[<Fact>]
let ``distinctUntilChangedKey uses the key function to decide whether an element has been seen before``() =
    let result   = ResizeArray()
    let obs      = Observable.ofSeq [ (1,3); (2,5); (2,7); (1,6); (3,5) ]
    let expected = [ (1,3); (2,5); (1,6); (3,5) ]
    obs |> Observable.distinctUntilChangedKey fst
        |> Observable.subscribe(result.Add) 
        |> ignore

    Assert.Equal(result, expected)

[<Fact>]
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

    Assert.Equal(result, expected)

[<Fact>]
let ``ofSeqOn enumerates its sequence on the specified scheduler``() =
    let result    = ResizeArray()
    let items     = [ 1; 2; 3]
    let scheduler = TestScheduler()

    let obs       = items |> Observable.ofSeqOn scheduler
    obs |> Observable.subscribe(result.Add) 
        |> ignore

    Assert.Empty(result)
    scheduler.Start()
    Assert.Equal(result, items)

[<Fact>]
let ``intervalOn produces a value at the specified rate on the supplied scheduler``() =
    let result    = ResizeArray()
    let scheduler = TestScheduler()
    let oneSecond = TimeSpan.FromSeconds(1.).Ticks

    let obs = Observable.intervalOn scheduler (TimeSpan.FromSeconds(2.))
    obs |> Observable.subscribe(result.Add) 
        |> ignore

    Assert.Empty(result)
    scheduler.AdvanceBy oneSecond; Assert.Empty(result)
    scheduler.AdvanceBy oneSecond; Assert.Equal(result.Count, 1)
    scheduler.AdvanceBy oneSecond; Assert.Equal(result.Count, 1)
    scheduler.AdvanceBy oneSecond; Assert.Equal(result.Count, 2)

[<Fact>]
let ``throttleOn produces a value at the specified rate on the supplied scheduler``() =
    let result    = ResizeArray()
    use obs       = new Subject<int>()
    let scheduler = TestScheduler()
    let oneSecond = TimeSpan.FromSeconds(1.).Ticks

    obs |> Observable.throttleOn scheduler (TimeSpan.FromSeconds(2.))
        |> Observable.subscribe(result.Add) 
        |> ignore

    obs.OnNext(1);                 Assert.Empty(result)
    scheduler.AdvanceBy oneSecond; Assert.Empty(result)
    scheduler.AdvanceBy oneSecond
    Assert.Equal(result, [1] )

    obs.OnNext(2)
    scheduler.AdvanceBy oneSecond; Assert.Equal(result, [1] )
    obs.OnNext(3)
    scheduler.AdvanceBy oneSecond; Assert.Equal(result, [1] )
    scheduler.AdvanceBy oneSecond; Assert.Equal(result, [1; 3] )

[<Fact>]
let ``combineLatest calls map function with pairs of latest values``() =
    let result   = ResizeArray()
    use obs1     = new Subject<int>()
    use obs2     = new Subject<int>()
    let map (x, y)  = x + (y / 2)
    Observable.combineLatest obs1 obs2
        |> Observable.map map
        |> Observable.subscribe(result.Add) 
        |> ignore

    Assert.Empty(result)
    obs1.OnNext 1
    Assert.Empty(result)
    obs1.OnNext 2
    Assert.Empty(result)
    obs2.OnNext 10
    Assert.Equal(result, [ 7 ] )
    obs2.OnNext 20                
    Assert.Equal(result, [ 7; 12 ] )
    obs1.OnNext 3                 
    Assert.Equal(result, [ 7; 12; 13 ] )

[<Fact>]
let ``combineLatestArray produces arrays of latest values``() =
    let result   = ResizeArray()
    use obs1     = new Subject<int>()
    use obs2     = new Subject<int>()
    Observable.combineLatestArray [| obs1; obs2 |]
        |> Observable.subscribe(result.Add) 
        |> ignore

    Assert.Empty(result)
    obs1.OnNext 1
    Assert.Empty(result)
    obs1.OnNext 2
    Assert.Empty(result)
    obs2.OnNext 10
    //Assert.Equal(List.ofSeq result, [ [| 2; 10 |] ] )
    obs2.OnNext 20                
    //Assert.Equal(result, [ [| 2; 10 |]; [| 2; 20 |] ] )
    obs1.OnNext 3                 
    //Assert.Equal(result, [ [| 2; 10 |]; [| 2; 20 |]; [| 3; 20 |] ] )

[<Fact>]
let ``combineLatestSeq produces lists of latest values``() =
    let result   = ResizeArray()
    use obs1     = new Subject<int>()
    use obs2     = new Subject<int>()
    let sources  = [ obs1 :> IObservable<_>; obs2 :> IObservable<_>] |> Seq.ofList
    Observable.combineLatestSeq sources
        |> Observable.subscribe(result.Add) 
        |> ignore

    Assert.Empty(result)
    obs1.OnNext 1
    Assert.Empty(result)
    obs1.OnNext 2
    Assert.Empty(result)
    obs2.OnNext 10
    //Assert.Equal(result, [ [2; 10] ] )
    obs2.OnNext 20                
    //Assert.Equal(result, [ [2; 10]; [2; 20] ] )
    obs1.OnNext 3                 
    //Assert.Equal(result, [ [2; 10]; [2; 20]; [3; 20] ] )

[<Fact>]
let ``combineLatestSeqMap applies map function to latest values``() =
    let result   = ResizeArray()
    use obs1     = new Subject<int>()
    use obs2     = new Subject<int>()
    let sources  = [ obs1 :> IObservable<_>; obs2 :> IObservable<_>] |> Seq.ofList
    let map xs   = xs |> Seq.sum
    Observable.combineLatestSeqMap map sources
        |> Observable.subscribe(result.Add) 
        |> ignore

    Assert.Empty(result)
    obs1.OnNext 1
    Assert.Empty(result)
    obs1.OnNext 2
    Assert.Empty(result)
    obs2.OnNext 10
    Assert.Equal(result, [ 12 ] )
    obs2.OnNext 20                
    Assert.Equal(result, [ 12; 22 ] )
    obs1.OnNext 3                 
    Assert.Equal(result, [ 12; 22; 23 ] )

[<Fact>]
let ``replay replays all notifications upon subscription``() =
    let result   = ResizeArray()
    use subject  = new Subject<int>()
    
    let obs = subject |> Observable.replay
    obs |> Observable.connect 
        |> ignore
    
    subject.OnNext 1
    subject.OnNext 2
    subject.OnNext 3
    
    obs |> Observable.subscribe(result.Add) 
        |> ignore

    Assert.Equal(result, [ 1; 2; 3 ] )
    subject.OnNext 4
    Assert.Equal(result, [ 1; 2; 3; 4 ] )


[<Fact>]
let ``replayBuffer replays only the required count of notifications upon subscription``() =
    let result   = ResizeArray()
    use subject  = new Subject<int>()
    
    let obs = subject |> Observable.replayBuffer 2
    obs |> Observable.connect 
        |> ignore
    
    subject.OnNext 1
    subject.OnNext 2
    subject.OnNext 3
    
    obs |> Observable.subscribe(result.Add) 
        |> ignore

    Assert.Equal(result, [ 2; 3 ] )
    subject.OnNext 4
    Assert.Equal(result, [ 2; 3; 4 ] )

[<Fact>]
let ``replayWindowOn replays only the required time range of notifications upon subscription``() =
    let oneSecond = TimeSpan.FromSeconds(1.).Ticks
    let result    = ResizeArray()
    use subject  = new Subject<int>()
    let scheduler = TestScheduler()

    let obs = subject |> Observable.replayWindowOn scheduler (TimeSpan.FromSeconds(3.))
    obs |> Observable.connect 
        |> ignore

    scheduler.AdvanceBy(oneSecond); subject.OnNext 1
    scheduler.AdvanceBy(oneSecond); subject.OnNext 2
    scheduler.AdvanceBy(oneSecond); subject.OnNext 3
    scheduler.AdvanceBy(oneSecond); subject.OnNext 4
    scheduler.AdvanceBy(oneSecond)

    obs |> Observable.subscribe(result.Add) 
        |> ignore

    scheduler.Start()
    Assert.Equal(result, [ 2; 3; 4 ] )

[<Fact>]
let ``replayBufferWindowOn replays only the required time range of notifications upon subscription``() =
    let oneSecond = TimeSpan.FromSeconds(1.).Ticks
    let result    = ResizeArray()
    use subject  = new Subject<int>()
    let scheduler = TestScheduler()

    let obs = subject |> Observable.replayBufferWindowOn scheduler 4 (TimeSpan.FromSeconds(3.))
    obs |> Observable.connect 
        |> ignore

    scheduler.AdvanceBy(oneSecond); subject.OnNext 1
    scheduler.AdvanceBy(oneSecond); subject.OnNext 2
    scheduler.AdvanceBy(oneSecond); subject.OnNext 3
    scheduler.AdvanceBy(oneSecond); subject.OnNext 4
    scheduler.AdvanceBy(oneSecond)

    obs |> Observable.subscribe(result.Add) 
        |> ignore

    scheduler.Start()
    Assert.Equal(result, [ 2; 3; 4 ] )

[<Fact>]
let ``replayBufferWindowOn replays only the required count of notifications upon subscription``() =
    let oneSecond = TimeSpan.FromSeconds(1.).Ticks
    let result    = ResizeArray()
    use subject  = new Subject<int>()
    let scheduler = TestScheduler()

    let obs = subject |> Observable.replayBufferWindowOn scheduler 2 (TimeSpan.FromSeconds(3.))
    obs |> Observable.connect 
        |> ignore

    scheduler.AdvanceBy(oneSecond); subject.OnNext 1
    scheduler.AdvanceBy(oneSecond); subject.OnNext 2
    scheduler.AdvanceBy(oneSecond); subject.OnNext 3
    scheduler.AdvanceBy(oneSecond); subject.OnNext 4
    scheduler.AdvanceBy(oneSecond)

    obs |> Observable.subscribe(result.Add) 
        |> ignore

    scheduler.Start()
    Assert.Equal(result, [ 3; 4 ] )

[<Fact>]
let ``timestampOn uses timestamps from the supplied scheduler``() =
    let oneSecond = TimeSpan.FromSeconds(1.).Ticks
    let result    = ResizeArray()
    use subject   = new Subject<int>()
    let scheduler = TestScheduler()

    let obs = subject |> Observable.materialize
                      |> Observable.timestampOn scheduler
                      |> Observable.subscribe(result.Add)
                      |> ignore

    scheduler.AdvanceBy(oneSecond)
    let firstNotificationAt = scheduler.Now
    subject.OnNext 1
    
    scheduler.AdvanceBy(oneSecond)
    let secondNotificationAt = scheduler.Now
    subject.OnNext 2

    Assert.Equal(result.Count, 2)
    Assert.Equal(result.[0].Timestamp, firstNotificationAt)
    Assert.Equal(result.[1].Timestamp, secondNotificationAt)

[<Fact>]
let ``Observable.Create should support a simple observable returning fun () -> ()``() =
    let obs =
        Observable.Create(fun (o : IObserver<_>) ->
            o.OnNext("xxx")
            o.OnCompleted()
            ignore)

    use x = obs.Subscribe(fun result -> Assert.Equal(result, "xxx"))
    ()

[<Fact>]
let ``Observable.subscribeOn should run subscription on another thread`` () =
    let expected  = "Hello World"
    let scheduler = new TestScheduler()
    let result    = ResizeArray()
    let oneSecond = TimeSpan.FromSeconds(1.).Ticks
    let obs =
        Observable.Create(fun (o : IObserver<_>) ->
            scheduler.Schedule(Action(fun () -> o.OnNext(expected)))
            )
    use x = obs
            |> Observable.subscribeOn(scheduler)
            |> Observable.subscribe(result.Add)
    Assert.True(result |> Seq.isEmpty)

    scheduler.AdvanceBy(oneSecond)
    Assert.Equal(result.[0], expected)
    ()
    
[<Fact>]
let ``FlatMapAsync should take F# async workflows and flatmap them to observables``() =
    let expected = "<head>fake header</head>"
    let fakeHttpRequest _ =
        async {  return expected }
    let result    = ResizeArray()
    let subject = new Subject<string>()

    use mapper = subject 
                    |> Observable.flatmapAsync fakeHttpRequest
                    |> Observable.subscribe result.Add

    Assert.Equal(result.Count, 0)

    subject.OnNext("www.google.com")
    subject.OnNext("www.microsoft.com")
    subject.OnNext("www.apple.com")
    
    System.Threading.Thread.Sleep 100
    // HACK: Yes this is using a Thread.Sleep. This is a problem in the current version of Rx 
    // interoping with other concurrency models. James World has a great Stackoverflow post
    // on the problems with this : http://stackoverflow.com/a/28236216
    // Dave Sexton has made a pull request to try to resolve these issues: 
    // https://github.com/Reactive-Extensions/Rx.NET/pull/65
    //
    // It looks like this will be in the next version of Rx to allow use of TestScheduler
    // but for now we live with this hack

    Assert.Equal(result.Count, 3)
    Assert.Equal(result.[0], expected)
    Assert.Equal(result.[1], expected)
    Assert.Equal(result.[2], expected)

[<Fact>]
let ``Observable.mapi should be equivalent to Array.mapi`` ()=
    Check.QuickThrowOnFailure <| 
    fun (items : int array) ->
        items
        |> Observable.ofSeq
        |> Observable.mapi (fun i x -> (i, x))
        |> Observable.toEnumerable
        |> Seq.toArray
        |> (=) (items |> Array.mapi (fun i x -> (i, x)))

[<Fact>]
let ``Observable.mapTo always maps to constant`` () =
    Check.QuickThrowOnFailure <|
    fun (xs : int array) (y : int) ->
        Observable.ofSeq xs
        |> Observable.mapTo y
        |> Observable.toEnumerable
        |> Seq.toList
        |> (=) (List.replicate xs.Length y)

[<Fact>]
let ``filteri should be equivalent to mapi then filter`` () =
    Check.QuickThrowOnFailure <|
    fun (items : int array) ->    
        let predicate i x = (i % 2 = 0) && (x > 0)

        let filtered =
            items
            |> Observable.ofSeq
            |> Observable.mapi (fun i x -> (i, x))
            |> Observable.filter (fun (i, x) -> predicate i x)
            |> Observable.map snd
            |> Observable.toEnumerable
            |> Seq.toArray

        let filtered' =
            Observable.ofSeq items
            |> Observable.filteri predicate
            |> Observable.toEnumerable
            |> Seq.toArray

        filtered = filtered'

//[<Fact>]
//let ``serve subscribes to all created observables`` () =
//    TestSchedule.usage <| fun sch ->
//        Observable.serve 1 (fun () -> Observable.range 0 10)
//        |> Observable.takeUntilOther (Observable.timerSpan (TimeSpan.FromMilliseconds 100.))
//        |> TestSchedule.subscribeTestObserver sch
//        |> TestObserver.nexts
//        |> fun xs -> Assert.Equal (xs.Length > 10)


type ObservableTests_WithTestNotifications () =
    
    let ``concat nexts with first error`` (TestNotifications ms) =
        let nexts = List.takeWhile isNext ms
        let fstError = List.tryFind isError ms |> Option.toList
        nexts @ fstError

    do
        Arb.register<GenTestNotification> () |> ignore
        Arb.register<ObservableTests_WithTestNotifications> () |> ignore


    static member Exceptions = Gen.constant (exn "Test Exception") |> Arb.fromGen

    [<Fact>]
    member __. ``choose should only pick 'Some' emits`` () =
        Check.QuickThrowOnFailure <| 
        fun (f : int -> int option) ms -> 
            TestSchedule.usage <| fun sch ->
                TestSchedule.hotObservable sch ms
                |> Observable.retry
                |> Observable.choose f
                |> TestSchedule.subscribeTestObserverStart sch
                |> TestObserver.nexts
                |> (=) (TestNotification.nexts ms |> List.choose f)

    [<Fact>]
    member __. ``throwing an exception in choose leads to the OnError event firing and does not lead to the exception flowing out even with a regular subscribe`` () =
        let o = Observable.ofSeq [1;2;3] |> choose (fun _ -> failwith "qwe")
        let error_flows_out = ref false
        try o |> Observable.subscribe (printfn "%i") |> ignore
        with _ -> error_flows_out := true
        Assert.Equal(!error_flows_out, false)
        o |> ``should be`` 0 true false

    //[<Fact>]
    //member __.``switchMap maps to new observable`` () =
    //    Check.QuickThrowOnFailure <|
    //    fun (xs : int list) (ys : int list) ->
    //        TestSchedule.usage <| fun sch ->
    //            Observable.ofSeq xs
    //            |> Observable.switchMap (fun _ -> Observable.ofSeq ys)
    //            |> TestSchedule.subscribeTestObserverStart sch
    //            |> TestObserver.nexts
    //            |> fun zs -> 
    //                let emptyWhenAnyEmpty = (xs = [] || ys = []) = (zs = [])
    //                let collectWhenGreater = (List.collect (fun _ -> ys) xs) = zs
    //                emptyWhenAnyEmpty .|. collectWhenGreater
    //                |> Prop.collect (printfn "Source: %A, Inner: %A, Result: %A" xs ys zs)
    
    //[<Fact>]
    //member __.``exhaustMap maps all incoming source emits`` () =
    //    Check.QuickThrowOnFailure <|
    //    fun (xs : int list) (f : int -> int) ->
    //    TestSchedule.usage <| fun sch ->
    //        let inner  = xs |> List.map f
    //        Observable.ofSeq xs
    //        |> Observable.exhaustMap (fun _ -> Observable.ofSeq inner)
    //        |> TestSchedule.subscribeTestObserverStart sch
    //        |> TestObserver.nexts = List.collect (fun _ -> inner) xs
            
    //[<Fact>]
    //member __.``exhaustMap with interval`` () =
    //    let source = Observable.interval (TimeSpan.FromSeconds 1.)
    //    source
    //    |> Observable.delay (TimeSpan.FromMilliseconds 10.)
    //    |> Observable.take 4
    //    |> Observable.merge (Observable.single 1L)
    //    |> Observable.exhaustMap (fun _ -> source |> Observable.take 5)
    //    |> Observable.toEnumerable
    //    |> Seq.toList
    //    |> fun ys -> Assert.Equal<int64 list> ([0L..3L], ys)

    //[<Fact>]
    //member __. ``catchOption maps to 'None' type when 'OnError'`` () =
    //    Check.QuickThrowOnFailure <| fun xs -> 
    //    TestSchedule.usage <| fun sch ->
    //        TestSchedule.coldObservable sch xs
    //        |> Observable.catchOption
    //        |> TestSchedule.subscribeTestObserverStart sch
    //        |> TestObserver.nexts
    //        |> (=) (``concat nexts with first error`` xs
    //                |> List.map TestNotification.toOption)

    //[<Fact>]
    //member __.``catchResult maps to 'Error' when 'OnError'`` () =
    //    Check.QuickThrowOnFailure <| fun xs ->
    //    TestSchedule.usage <| fun sch ->
    //        TestSchedule.coldObservable sch xs
    //        |> Observable.catchResult Observable.single
    //        |> TestSchedule.subscribeTestObserverStart sch
    //        |> TestObserver.nexts
    //        |> (=) (``concat nexts with first error`` xs
    //                |> List.map TestNotification.toResult)

    //[<Fact>]
    //member __.``consumes the emits by the producer: Test Hot Observable`` () =
    //    Check.QuickThrowOnFailure <| fun ms x ->
    //    TestSchedule.usage <| fun sch ->
    //        TestSchedule.hotObservable sch ms
    //        |> Observable.consumeMap (fun _ -> Observable.single x)
    //        |> TestSchedule.subscribeTestObserverStart sch
    //        |> TestObserver.nexts
    //        |> fun xs -> (List.replicate xs.Length x) = xs

    //[<Fact>]
    //member __.``consumes the optional emits by the producer: Test Hot Observable``() =
    //    Check.QuickThrowOnFailure <| fun (xs : int list) (x : int) ->
    //    (List.length xs >= 2) ==> lazy
    //    TestSchedule.usage <| fun sch ->
    //        let completeNow = ref false
    //        Observable.ofSeq xs
    //        |> Observable.consumeNextOn Scheduler.Immediate (fun _ -> 
    //            if !completeNow then None
    //            else completeNow := true; Some x)
    //        |> TestSchedule.subscribeTestObserverStart sch
    //        |> TestObserver.nexts
    //        |> (=) [x]