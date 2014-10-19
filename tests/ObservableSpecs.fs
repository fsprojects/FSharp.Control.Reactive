module FSharp.Reactive.Tests.ObservableSpecs

open System
open System.Reactive.Linq
open FSharp.Control.Reactive
open Builders
open NUnit.Framework

open Microsoft.Reactive.Testing
open FSharp.Reactive.Tests.ReactiveTesting
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
let ``Zipping two observable sequences of different types creates a single zipped observable`` =
    let obs1 = Observable.Return 1
    let obs2 = Observable.Return "A"
    let zipped = Observable.zip obs1 obs2 tuple
    let result = zipped |> Observable.First
    let expected = ( 1, "A" )

    Assert.That(result, Is.EqualTo expected)


[<Test>]
let ``Select Select1``() = 
    let scheduler = new TestScheduler()
    
    let xs = 
        [| RxRecording(180L, RxNext 5)
           RxRecording(210L, RxNext 4)
           RxRecording(240L, RxNext 3)
           RxRecording(290L, RxNext 2)
           RxRecording(350L, RxNext 1)
           RxRecording(400L, RxCompleted) |]
        |> scheduler.createHotObservable
    
    let res = 
        scheduler.start (fun () -> 
            xs
            |> Observable.map (fun x -> x + 1)
            |> Observable.map (fun x -> x - 2))
    
    res.Messages.AssertEqual( 
                              RxRecording(210L, RxNext(4 + 1 - 2)), RxRecording(240L, RxNext(3 + 1 - 2)), RxRecording(290L, RxNext(2 + 1 - 2)), RxRecording(350L, RxNext(1 + 1 - 2)), RxRecording(400L, RxCompleted))
    xs.Subscriptions.AssertEqual( 
                                  CompletedSubscription(200L, 400L))
[<Test>]
let ``Select Select1-2``() = 
    let scheduler = new TestScheduler()
    
    let xs = 
        [| RxRecording(180L, RxNext 5)
           RxRecording(210L, RxNext 4)
           RxRecording(240L, RxNext 3)
           RxRecording(290L, RxNext 2)
           RxRecording(350L, RxNext 1)
           RxRecording(400L, RxCompleted) |]
        |> scheduler.createHotObservable
    
    let res =  scheduler.controlledStart (fun () -> 
        xs
        |> Observable.map (fun x -> x + 1)
        |> Observable.map (fun x -> x - 2))

    res.Messages.AssertEqual( 
                              RxRecording(210L, RxNext(4 + 1 - 2)), 
                              RxRecording(240L, RxNext(3 + 1 - 2)), 
                              RxRecording(290L, RxNext(2 + 1 - 2)), 
                              RxRecording(350L, RxNext(1 + 1 - 2)), 
                              RxRecording(400L, RxCompleted))
    xs.Subscriptions.AssertEqual( CompletedSubscription(200L, 400L)) // to do remove extension method


[<Test>]
let OneShotTimer_TimeSpan_Zero() = 
    let scheduler = new TestScheduler()
    let res = scheduler.start (fun () -> Observable.timerSpanWithScheduler scheduler (TimeSpan.FromTicks(0L)))
    res.Messages.AssertEqual([| onNext 201L 0L
                                onCompleted 201L |])

[<Test>]
let Where_Where4() = 
    let scheduler = new TestScheduler()
    
    let xs = 
        [| onNext 110L 1
           onNext 180L 2
           onNext 230L 3
           onNext 270L 4
           onNext 340L 5
           onNext 380L 6
           onNext 390L 7
           onCompleted 400L |]
        |> scheduler.createHotObservable
    
    let res = 
        scheduler.start (fun () -> 
            xs
            |> Observable.filteri (fun i x -> i >= 1)
            |> Observable.filteri (fun i x -> i < 2))
    
    res.Messages
    |> Seq.seqEqual [| onNext 270L 4
                       onNext 340L 5
                       onCompleted 400L |]
    |> Assert.IsTrue
    res.Messages
    |> Seq.seqEqual [| onNext 270L 4
                       onNext 340L 5
                       onCompleted 400L |]
    |> Assert.IsTrue
    xs.Subscriptions.AssertEqual(CompletedSubscription(200L, 400L))

[<Test>]
let Where_Where42() = 
    let scheduler = new TestScheduler()
    
    let test a (b:RxMessage<_>) = RxNext(b)


    let z = test 0 (RxNext  4)
        
    let xs = 
        [| RxRecording(110L, (RxNext 1))
           RxRecording(180L, (RxNext 2))
           RxRecording(230L, (RxNext 3))
           RxRecording(270L, (RxNext 4))
           RxRecording(340L, (RxNext 5))
           RxRecording(380L, (RxNext 6))
           RxRecording(390L, (RxNext 7))
           RxRecording(400L, RxCompleted) |]
        |> scheduler.createHotObservable
    
    let res = 
        scheduler.start (fun () -> 
            xs
            |> Observable.filteri (fun i x -> i >= 1)
            |> Observable.filteri (fun i x -> i < 2))
    
    res.Messages
    |> Seq.seqEqual [| RxRecording( 270L, (RxNext  4))
                       RxRecording( 340L, (RxNext  5))
                       RxRecording( 400L , RxCompleted)|]
    |> Assert.IsTrue
    res.Messages
    |> Seq.seqEqual [| RxRecording(  270L, (RxNext  4))
                       RxRecording(  340L, (RxNext  5))
                       RxRecording(  400L, RxCompleted)|]
    |> Assert.IsTrue
    xs.Subscriptions.AssertEqual(CompletedSubscription(200L, 400L))


[<Test>]
let Select_SelectorThrows() = 
    let scheduler = new TestScheduler()
    let invoked = ref 0
    
    let xs = 
        [| OnNext(180L, 1)
           OnNext(210L, 2)
           OnNext(240L, 3)
           OnNext(290L, 4)
           OnNext(350L, 5)
           OnCompleted(400L)
           OnNext(410L, -1)
           OnCompleted(420L)
           OnError(430L, new Exception()) |]
        |> scheduler.createHotObservable
    
    let ex = new Exception()
    let doneFinally = ref false
    
    let res = 
        scheduler.start (fun () -> 
            xs
            |> Observable.finallyDo (fun () -> doneFinally := true)
            |> Observable.map (fun x -> 
                   incr invoked
                   if (!invoked = 3) then raise (ex)
                   x + 1))
    res.Messages
    |> Seq.seqEqual [| OnNext(210L, 3)
                       OnNext(240L, 4)
                       OnError(290L, ex) |]
    |> Assert.IsTrue
    Assert.IsTrue(!doneFinally)
    xs.Subscriptions.AssertEqual(CompletedSubscription(200L, 290L))
    Assert.AreEqual(3, !invoked)

[<Test>]
let Select_SelectorThrowsCS() = 
    let scheduler = new TestScheduler()
    let invoked = ref 0
    
    let xs = 
        [| OnNext(180L, 1)
           OnNext(210L, 2)
           OnNext(240L, 3)
           OnNext(290L, 4)
           OnNext(350L, 5)
           OnCompleted(400L)
           OnNext(410L, -1)
           OnCompleted(420L)
           OnError(430L, new Exception()) |]
        |> scheduler.createHotObservable
    
    let ex = new Exception()
    
    let res = 
        scheduler.start (fun () -> 
            xs |> Observable.map (fun x -> 
                      invoked := (!invoked) + 1
                      if (!invoked = 3) then raise (ex)
                      x + 1))

    res.Messages
    |> Seq.seqEqual [| OnNext(210L, 3)
                       OnNext(240L, 4)
                       OnError(290L, ex) |]
    |> Assert.IsTrue
    xs.Subscriptions.AssertEqual(CompletedSubscription(200L, 290L))
    Assert.AreEqual(3, !invoked)

[<Test>]
let SelectWithIndex_Completed() = 
    let scheduler = new TestScheduler()
    let invoked = ref 0
    
    let xs = 
        scheduler.createHotObservable ([| OnNext(180L, 5)
                                          OnNext(210L, 4)
                                          OnNext(240L, 3)
                                          OnNext(290L, 2)
                                          OnNext(350L, 1)
                                          OnCompleted(400L)
                                          OnNext(410L, -1)
                                          OnCompleted(420L)
                                          OnError(430L, new System.Exception()) |])
    
    let res = 
        scheduler.start (fun () -> 
            xs |> Observable.mapi (fun index x -> 
                      incr invoked
                      (x + 1) + (index * 10)))
    
    res.Messages
    |> Seq.seqEqual [| OnNext(210L, 5)
                       OnNext(240L, 14)
                       OnNext(290L, 23)
                       OnNext(350L, 32)
                       OnCompleted(400L) |]
    |> Assert.IsTrue
    xs.Subscriptions.AssertEqual(CompletedSubscription(200L, 400L))
    Assert.AreEqual(4, !invoked)

[<Test>]
let ``Sequence equal tests``() = 
    let (||>>) f g (x, y) = g (f x y)
    let testSeqEquality expectedResult = Seq.forall (Seq.seqEqual ||>> ((=) expectedResult)) >> Assert.IsTrue
    let a = [| 1; 2 |]
    let a1 = [| 1; 2 |]
    let b = [| 1; 2; 3 |]
    let b1 = [| 1; 2; 3 |]
    [| (a, b)
       (b, a) |]
    |> testSeqEquality false
    [| (a, a)
       (a, a1)
       (a1, a)
       (b, b)
       (b, b1)
       (b1, b) |]
    |> testSeqEquality true


