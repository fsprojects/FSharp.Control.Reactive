namespace FSharp.Reactive.Tests


module ReactiveTesting = 
    open Microsoft.Reactive.Testing
    open FSharp.Control.Reactive
    type RxMessage<'a> = 
        | RxNext of 'a
        | RxError of System.Exception
        | RxCompleted
    
    type RxRecordedMessage<'a> = 
        | RxRecording of time : int64 * message : RxMessage<'a>
    
    let onNext t v = RxRecording(t, RxNext v)
    let onError t v = RxRecording(t, RxError v)
    let onCompleted t = RxRecording(t, RxCompleted)

    let OnNext(t, v) = RxRecording(t, RxNext v)
    let OnError(t,v) = RxRecording(t, RxError v)
    let OnCompleted(t) = RxRecording(t, RxCompleted)

//    let OnNext(t,v) = Recorded<_>(t, System.Reactive.Notification.CreateOnNext<_>(v))
//    let OnError(t,v) = Recorded<_>(t, System.Reactive.Notification.CreateOnError<_>(v))
//    let OnCompleted(t) = Recorded<_>(t, System.Reactive.Notification.CreateOnCompleted<_>())
    
    type RxPredicatedRecordedMessage<'a when 'a : equality> = 
        | TestedRecording of recording : RxRecordedMessage<'a>
        | TesterRecording of recording : RxRecordedMessage<'a>
        | RxOnNextPredicatedRecording of time : int64 * predicate : ('a -> bool)
        | RxOnErrorPredicatedRecording of time : int64 * predicate : (System.Exception -> bool)
        
        override x.Equals(y) = 
            // eg to handle predicate ex is timeoutException etc
            match y with
            | :? RxPredicatedRecordedMessage<'a> as r -> 
                match (x, r) with
                | (TestedRecording(lm), TesterRecording(rm)) -> lm = rm
                | (TestedRecording(RxRecording(lt, RxNext lv)), RxOnNextPredicatedRecording(rt, rp)) -> lt = rt && rp lv
                | (TestedRecording(RxRecording(lt, RxError lv)), RxOnErrorPredicatedRecording(rt, rp)) -> lt = rt && rp lv
                | _ -> false
            | _ -> false
        
        override x.GetHashCode() = 0 //todo
    
    type RxSuscription = 
        | CompletedSubscription of startTime : int64 * endTime : int64
        | RunningSubscription of startTime : int64
    
    type ITestableRxObservable<'a> = 
        inherit System.IObservable<'a>
        abstract Messages : ResizeArray<RxRecordedMessage<'a>>
        abstract Subscriptions : ResizeArray<RxSuscription>
    
    type private ColdObservable<'a>(scheduler : TestScheduler, messages : RxRecordedMessage<'a> seq) = 
        let messages = ResizeArray<_>(messages)
        let subscriptions = ResizeArray<_>()
        
        interface ITestableRxObservable<'a> with
            member x.Messages = messages
            member x.Subscriptions = subscriptions
        
        interface System.IObservable<'a> with
            member x.Subscribe observer = 
                let subscriptionIndex = subscriptions.Count
                let subscriptionTime = scheduler.Clock
                subscriptions.Add(RunningSubscription(subscriptionTime))
                let d = 
                    messages
                    |> Seq.map (fun (RxRecording(t, m)) -> 
                           scheduler.ScheduleRelative(null, t, 
                                                      System.Func<_, _, _>(fun s t -> 
                                                          match m with
                                                          | RxNext(value) -> observer.OnNext value
                                                          | RxError(value) -> observer.OnError value
                                                          | RxCompleted -> observer.OnCompleted()
                                                          System.Reactive.Disposables.Disposable.Empty)))
                    |> Seq.toList
                { new System.IDisposable with
                      member x.Dispose() = 
                          subscriptions.[subscriptionIndex] <- CompletedSubscription(subscriptionTime, scheduler.Clock)
                          d |> List.iter (fun d -> d.Dispose()) }
    
    type private HotObservable<'a>(scheduler : TestScheduler, messages : RxRecordedMessage<'a> seq) = 
        let observers = ResizeArray<System.IObserver<'a>>()
        let messages = ResizeArray<_>(messages)
        let subscriptions = ResizeArray<_>()
        
        do 
            messages |> Seq.iter (fun (RxRecording(t, m)) -> 
                            scheduler.ScheduleRelative(null, t, 
                                                       System.Func<_, _, _>(fun s t -> 
                                                           observers
                                                           |> Seq.toArray
                                                           |> Seq.iter (fun observer -> 
                                                                  match m with
                                                                  | RxNext(value) -> observer.OnNext value
                                                                  | RxError(value) -> observer.OnError value
                                                                  | RxCompleted -> observer.OnCompleted())
                                                           System.Reactive.Disposables.Disposable.Empty))
                            |> ignore)
        
        interface ITestableRxObservable<'a> with
            member x.Messages = messages
            member x.Subscriptions = subscriptions
        
        interface System.IObservable<'a> with
            member x.Subscribe observer = 
                let subscriptionIndex = subscriptions.Count
                let subscriptionTime = scheduler.Clock
                observers.Add(observer)
                subscriptions.Add(RunningSubscription(subscriptionTime))
                { new System.IDisposable with
                      member x.Dispose() = 
                          observers.Remove(observer) |> ignore
                          subscriptions.[subscriptionIndex] <- CompletedSubscription(subscriptionTime, scheduler.Clock) }
    
    type ITestableObserver<'a> = 
        inherit System.IObserver<'a>
        abstract Messages : System.Collections.Generic.IList<RxRecordedMessage<'a>>
    
    type MockObserver<'a>(scheduler : TestScheduler) = 
        
        do 
            if (scheduler = null) then failwith "scheduler null"
        
        let messages = ResizeArray<RxRecordedMessage<'a>>()
        interface ITestableObserver<'a> with
            member x.Messages = messages :> System.Collections.Generic.IList<_>
            member x.OnNext v = messages.Add(RxRecording(scheduler.Clock, RxNext v))
            member x.OnError v = messages.Add(RxRecording(scheduler.Clock, RxError v))
            member x.OnCompleted() = messages.Add(RxRecording(scheduler.Clock, RxCompleted))
    
    type Microsoft.Reactive.Testing.TestScheduler with
        member x.createColdObservable stream = new ColdObservable<_>(x, stream) :> ITestableRxObservable<_>
        member x.createHotObservable stream = new HotObservable<_>(x, stream) :> ITestableRxObservable<_>
        member x.scheduleAbsoluteWithState<'a> state dueTime f = x.ScheduleAbsolute<'a>(state, dueTime, System.Func<_, _, _> f)
        member x.scheduleAbsolute dueTime f = x.ScheduleAbsolute<obj>(null, dueTime, System.Func<_, _, _>(fun sc s -> f (sc)))
        member x.createObserver<'a>() = MockObserver<'a>(x) :> ITestableObserver<_>
        
        member x.startWithAll (create : unit -> System.IObservable<'a>) created subscribed disposed = 
            //guard
            let source : System.IObservable<'a> ref = ref null
            let subscription : System.IDisposable option ref = ref None
            let observer = x.createObserver<'a>()
            x.ScheduleAbsolute(null, created, 
                               System.Func<_, _, _>(fun scheduler s -> 
                                   source := create()
                                   System.Reactive.Disposables.Disposable.Empty))
            |> ignore
            x.ScheduleAbsolute(null, subscribed, 
                               System.Func<_, _, _>(fun scheduler s -> 
                                   subscription := Some((!source) |> Observable.subscribeObserver observer)
                                   System.Reactive.Disposables.Disposable.Empty))
            |> ignore
            x.ScheduleAbsolute(null, disposed, 
                               System.Func<_, _, _>(fun scheduler s -> 
                                   (!subscription) |> Option.iter (fun v -> v.Dispose())
                                   System.Reactive.Disposables.Disposable.Empty))
            |> ignore
            x.Start()
            observer
        
        member x.startWithDispose create disposed = 
            //guard
            x.startWithAll create ReactiveTest.Created ReactiveTest.Subscribed disposed
        member x.start create = 
            //guard
            x.startWithAll create ReactiveTest.Created ReactiveTest.Subscribed ReactiveTest.Disposed
        
        member x.controlledStartWithAll created subscribed disposed create = 
            let results = x.createObserver()
            Observable.timerSpanWithScheduler x (System.TimeSpan.FromTicks created)
            |> Observable.flatmap (fun _ -> create())
            |> Observable.delaySubscriptionWithScheduler x (System.TimeSpan.FromTicks(subscribed - created))
            |> Observable.takeUntilOther (Observable.timerSpanWithScheduler x (System.TimeSpan.FromTicks disposed))
            |> Observable.subscribeObserver results
            |> ignore
            x.Start()
            results
        
        member x.controlledStartWithDisposed disposed create = x.controlledStartWithAll ReactiveTest.Created ReactiveTest.Subscribed disposed create
        member x.controlledStart create = x.controlledStartWithAll ReactiveTest.Created ReactiveTest.Subscribed ReactiveTest.Disposed create

module Seq = 
    open System.Linq
    let seqEqual (s1 : 'a seq when 'a : equality) (s2 : 'a seq when 'a : equality) = 
       s1.SequenceEqual(s2)

