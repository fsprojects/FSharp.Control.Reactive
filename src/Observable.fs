namespace FSharp
module Observable =
  open System
  open System.Linq
  open System.Threading

  type 'a observable = IObservable<'a>
  type 'a observer = IObserver<'a>
  
  /// converts a lambda from unit to unit to a System.Action
  let asAction f = Action(f)
  
  /// converts a lambda from a param to unit to a System.Action
  let asParamAction f = Action<_>(f)
  
  /// converts a lambda from an exception to unit to a System.Action
  let asExnAction f = Action<exn>(f)
  
  /// System.Action which does nothing
  let doNothing = Action(fun () -> ())
  
  /// Creates an observable with a subscriber of type IObserver<'a> -> unit -> unit
  let create f =
    let subscriber = Func<_,_>(fun o -> Action(f o))
    Observable.Create(subscriber)

  /// Creates an observable with a subscriber of type IObserver<'a> -> IDisposable
  let createWithDisposable f =
    let subscriber = Func<_,_>(f)
    Observable.CreateWithDisposable(subscriber)
  
  /// Creates an observer
  let createObserver next error completed =
    let onNext = next |> asParamAction
    let onError = error |> asExnAction
    let onCompleted = completed |> asAction
    Observer.Create(onNext, onError, onCompleted)
  
  /// Generates an observable from an IEvent<_>
  let fromEvent (event:IEvent<_,_>) =
    Observable.FromEvent(event.AddHandler, event.RemoveHandler)

  /// Generates an observable from an Async<_>.
  let fromAsync a = 
    { new IObservable<_> with
        member x.Subscribe(o) =
          if o = null then nullArg "observer"
          let cts = new System.Threading.CancellationTokenSource()
          let invoked = ref 0
          let cancelOrDispose cancel =
            if System.Threading.Interlocked.CompareExchange(invoked, 1, 0) = 0 then
              if cancel then cts.Cancel() else cts.Dispose()
          let wrapper = async {
            try
              try
                let! r = a
                o.OnNext(r)
                o.OnCompleted()
              with e -> o.OnError(e)
            finally cancelOrDispose false }
          Async.StartImmediate(wrapper, cts.Token)
          { new IDisposable with member x.Dispose() = cancelOrDispose true } }
  
  /// Generates an empty observable
  let empty<'a> = Observable.Empty<'a>()
  
  /// Takes the head of the elements
  let head = Observable.First
  
  /// Merges the two observables
  let merge (obs1: 'a observable) (obs2: 'a observable) = Observable.Merge(obs1, obs2)
  
  /// Creates a range as an observable
  let range start count = Observable.Range(start, count)
  
  /// Converts a seq into an observable
  let toObservable (seq: 'a seq) = Observable.ToObservable(seq)
  
  /// Converts an observable into a seq
  let toEnumerable (obs: 'a observable) = Observable.ToEnumerable(obs)
  
  /// Subscribes to the observable with all three callbacks
  let subscribeAll next error completed (observable: 'a observable) =
    observable.Subscribe(createObserver next error completed)
  
  /// Subscribes to the Observable with a next and an error-function
  let subscribeWithError next error observable =
    subscribeAll next error (fun () -> ()) observable
  
  /// Subscribes to the Observable with just the next-function
  let subscribe next observable =
    subscribeWithError next (fun exn -> ()) observable
  
  /// Throttles the observable for the given interval
  let throttle (interval:TimeSpan) observable =
    Observable.Throttle(observable, interval)
  
  /// Samples the observable at the given interval
  let sample (interval:TimeSpan) observable =
    Observable.Sample(observable, interval)
  
  /// Returns the observable sequence that reacts first
  let amb obsLeft obsRight = Observable.Amb(obsLeft, obsRight)
  
  /// Returns the observable sequence that reacts first
  let ambFromSeq (obs: 'a observable seq) = Observable.Amb(obs)
  
  /// Returns the observable sequence that reacts first
  let ambFromArray (obs: 'a observable array) = Observable.Amb(obs)
  
  /// Matches when both observable sequences have an available value
  let both obs1 obs2 = Observable.And(obs1, obs2)
  
  /// Merges two observable sequences into one observable sequence
  let zip (obs1: 'a observable) (obs2: 'a observable) =
    Observable.Zip(obs1, obs2, Func<_,_,_>(fun a b -> a, b))
  
  /// Merges two observable sequences into one observable sequence
  /// whenever one of the observable sequences has a new value
  ///   ===> More results than zip
  let combineLatest obs1 obs2 =
    Observable.CombineLatest(obs1, obs2, Func<_,_,_>(fun a b -> a,b))
  
  /// Concats (flattens) an observable of observables into an observable
  ///   ===> Observable.SelectMany(observable, Func<_,_>(fun (x:IObservable<'a>) -> x))
  let concat observable = Observable.Concat(observable)
  
  /// maps the given observable with the given function
  let map f observable = Observable.Select(observable,Func<_,_>(f))  
   
  /// maps the given observable with the given function
  let mapi f observable =
    Observable.Select(observable,Func<_,_,_>(fun x i ->f i x))  
   
  /// Filters all elements where the given predicate is satisfied
  let filter f observable =
    Observable.Where(observable, Func<_,_>(f))
   
  /// Skips n elements
  let skip n observable = Observable.Skip(observable, n)
   
  /// Skips elements while the predicate is satisfied
  let skipWhile f observable = Observable.SkipWhile(observable, Func<_,_>(f))
   
  /// Counts the elements
  let count = Observable.Count
   
  /// Takes n elements
  let take n observable = Observable.Take(observable, n)  
   
  /// Determines whether the given observable is empty 
  let isEmpty obs = obs = Observable.Empty()
   
  /// Determines whether the given observable is not empty 
  let isNotEmpty observable = observable |> isEmpty |> not
   
  /// Determines whether an observable sequence contains a specified value
  /// which satisfies the given predicate
  let exists predicate observable =
    observable
      |> skipWhile (predicate >> not)
      |> isNotEmpty
   
  /// Continues an observable sequence that is terminated
  /// by an exception with the next observable sequence.
  let catch (newObservable: 'a observable) failingObservable =
    Observable.Catch(failingObservable,newObservable) 
   
  /// Takes elements while the predicate is satisfied
  let takeWhile f observable = Observable.TakeWhile(observable, Func<_,_>(f))
   
  /// Iterates through the observable and performs the given side-effect
  let perform f observable = Observable.Do(observable, fun x -> f x)
   
  /// Invokes the finally action after source observable sequence terminates normally or by an exception.
  let performFinally f observable = Observable.Finally(observable, fun _ -> f())
   
  /// Folds the observable
  let fold f seed observable = Observable.Aggregate(observable, seed, Func<_,_,_>(f))  
   
  type IObservable<'a> with
    /// Subscribes to the Observable with just a next-function
    member this.Subscribe(next) = subscribe next this
   
    /// Subscribes to the Observable with a next and an error-function
    member this.Subscribe(next, error) = subscribeWithError next error this
   
    /// Subscribes to the Observable with all 3 callbacks
    member this.Subscribe(next, error, completed) = subscribeAll next error completed this
