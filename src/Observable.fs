module FSharp.Reactive

open System
open System.Reactive
open System.Reactive.Linq
open System.Reactive.Concurrency

type Observer with
  /// Creates an observer from the specified onNext function.
  static member Create(onNext) =
    Observer.Create(Action<_> onNext)

  /// Creates an observer from the specified onNext and onError functions.
  static member Create(onNext, onError) =
    Observer.Create(Action<_> onNext, Action<_> onError)

  /// Creates an observer from the specified onNext and onCompleted functions.
  static member Create(onNext, onCompleted) =
    Observer.Create(Action<_> onNext, Action onCompleted)

  /// Creates an observer from the specified onNext, onError, and onCompleted functions.
  static member Create(onNext, onError, onCompleted) =
    Observer.Create(Action<_> onNext, Action<_> onError, Action onCompleted)

type Observable with
  /// Creates an observable sequence from the specified Subscribe method implementation.
  static member Create (subscribe:'a IObserver -> unit -> unit) =
    Observable.Create(Func<_,_>(fun o -> Action(subscribe o)))

  /// Creates an observable sequence from the specified Subscribe method implementation.
  static member Create subscribe =
    Observable.Create(Func<_,IDisposable> subscribe)
  
module Observable =
  /// Binds an observable to generate a subsequent observable.
  let bind (f:'a -> IObservable<'b>) (m:IObservable<'a>) = m.SelectMany(Func<_,_> f)

  /// Creates an observable sequence from the specified Subscribe method implementation.
  let create (f:'a IObserver -> unit -> unit) = Observable.Create f

  /// Generates an observable from an IEvent<_> as an EventPattern.
  let fromEventPattern<'a> (target:obj) eventName =
    Observable.FromEventPattern(target, eventName)
  
  /// Generates an empty observable
  let empty<'a> = Observable.Empty<'a>()
  
  /// Takes the head of the elements
  let head = Observable.First
  
  /// Merges the two observables
  let merge (second:'a IObservable) (first:'a IObservable) = Observable.Merge(first, second)
  
  /// Creates a range as an observable
  let range start count = Observable.Range(start, count)
  
  /// Converts a seq into an observable
  let toObservable (source:'a seq) = Observable.ToObservable(source)
  
  /// Converts an observable into a seq
  let toEnumerable (source:'a IObservable) = Observable.ToEnumerable(source)
  
  /// Subscribes to the observable with all three callbacks
  let subscribe onNext onError onCompleted (observable: 'a IObservable) =
    observable.Subscribe(Observer.Create(Action<_> onNext, Action<_> onError, Action onCompleted))
  
  /// Returns the observable sequence that reacts first
  let amb second first = Observable.Amb(first, second)

  /// Matches when both observable sequences have an available value
  let both second first = Observable.And(first, second)

  /// Merges two observable sequences into one observable sequence
  let zip (second:'a IObservable) (first:'a IObservable) =
    let inner a b = a, b
    Observable.Zip(first, second, Func<_,_,_> inner)

  /// Merges two observable sequences into one observable sequence
  /// whenever one of the observable sequences has a new value
  ///   ===> More results than zip
  let combineLatest second first =
    let inner a b = a, b
    Observable.CombineLatest(first, second, Func<_,_,_> inner)
  
  /// Concats (flattens) an observable of observables into an observable
  ///   ===> Observable.SelectMany(observable, Func<_,_>(fun (x:IObservable<'a>) -> x))
  let concat (second:'a IObservable) (first:'a IObservable) = Observable.Concat(first, second)
  
  /// maps the given observable with the given function
  let map f source = Observable.Select(source, Func<_,_>(f))  
   
  /// maps the given observable with the given function
  let mapi f source =
    let inner x i = f i x
    Observable.Select(source, Func<_,_,_> inner)
   
  /// Filters all elements where the given predicate is satisfied
  let filter f source =
    Observable.Where(source, Func<_,_> f)
   
  /// Skips n elements
  let skip n source = Observable.Skip(source, n)
   
  /// Skips elements while the predicate is satisfied
  let skipWhile f source = Observable.SkipWhile(source, Func<_,_> f)
   
  /// Counts the elements
  let count source = Observable.Count(source)
   
  /// Takes n elements
  let take n source = Observable.Take(source, n)  

  /// Determines whether the given observable is empty 
  let isEmpty source = source = Observable.Empty()

  /// Determines whether an observable sequence contains a specified value
  /// which satisfies the given predicate
  let exists f source = source |> skipWhile (not << f) |> (not << isEmpty)

  /// Throttles the observable for the given interval
  let throttle (interval:TimeSpan) source =
    Observable.Throttle(source, interval)
  
  /// Samples the observable at the given interval
  let sample (interval:TimeSpan) source =
    Observable.Sample(source, interval)

  /// Continues an observable sequence that is terminated
  /// by an exception with the next observable sequence.
  let catch (second:'a IObservable) first =
    Observable.Catch(first, second) 
   
  /// Takes elements while the predicate is satisfied
  let takeWhile f source = Observable.TakeWhile(source, Func<_,_> f)
   
  /// Iterates through the observable and performs the given side-effect
  let perform f source =
    let inner x = f x
    Observable.Do(source, inner)
   
  /// Invokes the finally action after source observable sequence terminates normally or by an exception.
  let performFinally f source = Observable.Finally(source, Action f)
   
  /// Folds the observable
  let fold f seed source = Observable.Aggregate(source, seed, Func<_,_,_> f)

  /// Reduces the observable
  let reduce f source = Observable.Aggregate(source, Func<_,_,_> f)

type IObservable<'a> with
  /// Subscribes to the Observable with just a next-function.
  member this.Subscribe(onNext:'a -> unit) =
    this.Subscribe(Action<_> onNext)

  /// Subscribes to the Observable with a next and an error-function.
  member this.Subscribe(onNext:'a -> unit, onError:exn -> unit) =
    this.Subscribe(Action<_> onNext, Action<exn> onError)
 
  /// Subscribes to the Observable with a next and a completion callback.
  member this.Subscribe(onNext:'a -> unit, onCompleted:unit -> unit) =
    this.Subscribe(Action<_> onNext, Action onCompleted)

  /// Subscribes to the Observable with all 3 callbacks.
  member this.Subscribe(onNext, onError, onCompleted) =
    this.Subscribe(Action<_> onNext, Action<_> onError, Action onCompleted)

let inline mreturn x = Observable.Return x
let inline (>>=) m f = Observable.bind f m
let inline ap f m = f >>= fun f' -> m >>= fun m' -> mreturn (f' m')
let inline (<*>) f m = ap f m
let inline lift f m = Observable.map f m
let inline (<!>) f m = lift f m
let inline lift2 f a b = mreturn f <*> a <*> b
let inline ( *>) x y = lift2 (fun _ z -> z) x y
let inline ( <*) x y = lift2 (fun z _ -> z) x y

type ObservableBuilder() =
  member this.Return(x) = mreturn x
  member this.ReturnFrom(m:IObservable<_>) = m
  member this.Bind(m, f) = m >>= f
  member this.Combine(comp1:IObservable<'a>, comp2:IObservable<'a>) = Observable.Concat(comp1, comp2)
  member this.Delay(f) = Observable.Defer f
  member this.Zero() = Observable.Empty()
  member this.TryWith(m:IObservable<_>, h:exn -> IObservable<_>) = Observable.Catch(m, h)
  member this.TryFinally(m:IObservable<_>, compensation: unit -> unit) = Observable.Finally(m, Action(compensation))
  member this.Using(res:#IDisposable, body) = this.TryFinally(body res, fun () -> match res with null -> () | disp -> disp.Dispose())
  member this.While(guard, m) = if not (guard()) then this.Zero() else m >>= fun () -> this.While(guard, m)
  member this.For(sequence, body) = Observable.ForEach(sequence, body)
  member this.Yield(x) = mreturn x
  member this.YieldFrom(m:IObservable<_>) = m

let observe = ObservableBuilder()
