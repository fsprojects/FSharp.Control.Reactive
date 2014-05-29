namespace FSharp.Reactive

open System
open System.Reactive
open System.Reactive.Linq
open System.Reactive.Concurrency

[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Builders =

    /// An Observable computation builder.
    type ObservableBuilder() =
        member this.Return(x) = Observable.Return(x, Scheduler.CurrentThread)
        member this.ReturnFrom m : IObservable<_> = m
        member this.Bind(m: IObservable<_>, f: _ -> IObservable<_>) = m.SelectMany(f)
        member this.Combine(comp1, comp2) = Observable.Concat(comp1, comp2)
        member this.Delay(f: _ -> IObservable<_>) = Observable.Defer(fun _ -> f())
        member this.Zero() = Observable.Empty(Scheduler.CurrentThread :> IScheduler)
        member this.For(sequence, body) = Observable.For(sequence, body)
        member this.TryWith(m: IObservable<_>, h: #exn -> IObservable<_>) = Observable.Catch(m, h)
        member this.TryFinally(m, compensation) = Observable.Finally(m, Action compensation)
        member this.Using(res: #IDisposable, body) = Observable.Using((fun () -> res), body)
        member this.While(guard, m: IObservable<_>) = Observable.While(guard, m)
        // TODO: Are these the correct implementation? Are they necessary?
        member this.Yield(x) = Observable.Return(x, Scheduler.CurrentThread)
        member this.YieldFrom m : IObservable<_> = m

    let observe = ObservableBuilder()

    /// A reactive query builder.
    /// See http://mnajder.blogspot.com/2011/09/when-reactive-framework-meets-f-30.html
    type RxQueryBuilder() =
        member this.For (s:IObservable<_>, body : _ -> IObservable<_>) = s.SelectMany(body)
        [<CustomOperation("select", AllowIntoPattern=true)>]
        member this.Select (s:IObservable<_>, [<ProjectionParameter>] selector : _ -> _) = s.Select(selector)
        [<CustomOperation("where", MaintainsVariableSpace=true, AllowIntoPattern=true)>]
        member this.Where (s:IObservable<_>, [<ProjectionParameter>] predicate : _ -> bool ) = s.Where(predicate)
        [<CustomOperation("takeWhile", MaintainsVariableSpace=true, AllowIntoPattern=true)>]
        member this.TakeWhile (s:IObservable<_>, [<ProjectionParameter>] predicate : _ -> bool ) = s.TakeWhile(predicate)
        [<CustomOperation("take", MaintainsVariableSpace=true, AllowIntoPattern=true)>]
        member this.Take (s:IObservable<_>, count: int) = s.Take(count)
        [<CustomOperation("skipWhile", MaintainsVariableSpace=true, AllowIntoPattern=true)>]
        member this.SkipWhile (s:IObservable<_>, [<ProjectionParameter>] predicate : _ -> bool ) = s.SkipWhile(predicate)
        [<CustomOperation("skip", MaintainsVariableSpace=true, AllowIntoPattern=true)>]
        member this.Skip (s:IObservable<_>, count: int) = s.Skip(count)
        member this.Zero () = Observable.Empty(Scheduler.CurrentThread :> IScheduler)
        member this.Yield (value) = Observable.Return(value)
        [<CustomOperation("count")>]
        member this.Count (s:IObservable<_>) = Observable.Count(s)
        [<CustomOperation("all")>]
        member this.All (s:IObservable<_>, [<ProjectionParameter>] predicate : _ -> bool ) = s.All(new Func<_,bool>(predicate))
        [<CustomOperation("contains")>]
        member this.Contains (s:IObservable<_>, key) = s.Contains(key)
        [<CustomOperation("distinct", MaintainsVariableSpace=true, AllowIntoPattern=true)>]
        member this.Distinct (s:IObservable<_>) = s.Distinct()
        [<CustomOperation("exactlyOne")>]
        member this.ExactlyOne (s:IObservable<_>) = s.Single()
        [<CustomOperation("exactlyOneOrDefault")>]
        member this.ExactlyOneOrDefault (s:IObservable<_>) = s.SingleOrDefault()
        [<CustomOperation("find")>]
        member this.Find (s:IObservable<_>, [<ProjectionParameter>] predicate : _ -> bool) = s.First(new Func<_,bool>(predicate))
        [<CustomOperation("head")>]
        member this.Head (s:IObservable<_>) = s.First()
        [<CustomOperation("headOrDefault")>]
        member this.HeadOrDefault (s:IObservable<_>) = s.FirstOrDefault()
        [<CustomOperation("last")>]
        member this.Last (s:IObservable<_>) = s.Last()
        [<CustomOperation("lastOrDefault")>]
        member this.LastOrDefault (s:IObservable<_>) = s.LastOrDefault()
        [<CustomOperation("maxBy")>]
        member this.MaxBy (s:IObservable<'a>,  [<ProjectionParameter>] valueSelector : 'a -> 'b) = s.MaxBy(new Func<'a,'b>(valueSelector))
        [<CustomOperation("minBy")>]
        member this.MinBy (s:IObservable<'a>,  [<ProjectionParameter>] valueSelector : 'a -> 'b) = s.MinBy(new Func<'a,'b>(valueSelector))
        [<CustomOperation("nth")>]
        member this.Nth (s:IObservable<'a>,  index ) = s.ElementAt(index)
        [<CustomOperation("sumBy")>]
        member inline this.SumBy (s:IObservable<_>,[<ProjectionParameter>] valueSelector : _ -> _) = s.Select(valueSelector).Aggregate(Unchecked.defaultof<_>, new Func<_,_,_>( fun a b -> a + b)) 
        [<CustomOperation("groupBy", AllowIntoPattern=true)>]
        member this.GroupBy (s:IObservable<_>,[<ProjectionParameter>] keySelector : _ -> _) = s.GroupBy(new Func<_,_>(keySelector))
        [<CustomOperation("groupValBy", AllowIntoPattern=true)>]
        member this.GroupValBy (s:IObservable<_>,[<ProjectionParameter>] resultSelector : _ -> _,[<ProjectionParameter>] keySelector : _ -> _) = s.GroupBy(new Func<_,_>(keySelector),new Func<_,_>(resultSelector))
        [<CustomOperation("join", IsLikeJoin=true)>]
        member this.Join (s1:IObservable<_>,s2:IObservable<_>, [<ProjectionParameter>] s1KeySelector : _ -> _,[<ProjectionParameter>] s2KeySelector : _ -> _,[<ProjectionParameter>] resultSelector : _ -> _) = s1.Join(s2,new Func<_,_>(s1KeySelector),new Func<_,_>(s2KeySelector),new Func<_,_,_>(resultSelector))
        [<CustomOperation("groupJoin", AllowIntoPattern=true)>]
        member this.GroupJoin (s1:IObservable<_>,s2:IObservable<_>, [<ProjectionParameter>] s1KeySelector : _ -> _,[<ProjectionParameter>] s2KeySelector : _ -> _,[<ProjectionParameter>] resultSelector : _ -> _) = s1.GroupJoin(s2,new Func<_,_>(s1KeySelector),new Func<_,_>(s2KeySelector),new Func<_,_,_>(resultSelector))
        [<CustomOperation("zip", IsLikeZip=true)>]
        member this.Zip (s1:IObservable<_>,s2:IObservable<_>,[<ProjectionParameter>] resultSelector : _ -> _) = s1.Zip(s2,new Func<_,_,_>(resultSelector))
        [<CustomOperation("forkJoin", IsLikeZip=true)>]
        member this.ForkJoin (s1:IObservable<_>,s2:IObservable<_>,[<ProjectionParameter>] resultSelector : _ -> _) = s1.ForkJoin(s2,new Func<_,_,_>(resultSelector))
        [<CustomOperation("iter")>]
        member this.Iter(s:IObservable<_>, [<ProjectionParameter>] selector : _ -> _) = s.Do(selector)

    let rxquery = RxQueryBuilder()

/// The Observable module provides operators for working with IObservable<_> in F#.
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Observable =

    let private flipApply f x i = f i x

    /// Binds an observable to generate a subsequent observable.
    let bind (f: 'T -> IObservable<'TNext>) (m: IObservable<'T>) = m.SelectMany(f)

    /// Creates an observable sequence from the specified Subscribe method implementation.
    let create (f: IObserver<'T> -> (unit -> unit)) = Observable.Create(Func<_,_>(fun o -> Action(f o)))

    /// Generates an observable from an IEvent<_> as an EventPattern.
    let fromEventPattern<'T> (target:obj) eventName =
        Observable.FromEventPattern(target, eventName)
    
    /// Generates an empty observable
    let empty<'T> = Observable.Empty<'T>()
    
    /// Takes the head of the elements
    let head obs = Observable.FirstAsync(obs)
    
    /// Merges the two observables
    let merge (second: IObservable<'T>) (first: IObservable<'T>) = Observable.Merge(first, second)
    
    /// Creates a range as an observable
    let range start count = Observable.Range(start, count)
    
    /// Converts a seq into an observable
    let toObservable (source: seq<'T>) = Observable.ToObservable(source)
    
    /// Converts an observable into a seq
    let toEnumerable (source: IObservable<'T>) = Observable.ToEnumerable(source)
    
    /// Subscribes to the observable with all three callbacks
    let subscribe onNext onError onCompleted (observable: IObservable<'T>) =
        observable.Subscribe(Observer.Create(Action<_> onNext, Action<_> onError, Action onCompleted))

    /// Subscribes to the observable with the given observer
    let subscribeObserver observer (observable: IObservable<'T>) =
        observable.Subscribe observer
    
    /// Returns the observable sequence that reacts first
    let amb second first = Observable.Amb(first, second)

    /// Matches when both observable sequences have an available value
    let both second first = Observable.And(first, second)

    /// Merges two observable sequences into one observable sequence
    let zip (second: IObservable<'U>) (first: IObservable<'T>) =
        let inner a b = a, b
        Observable.Zip(first, second, inner)

    /// Merges two observable sequences into one observable sequence
    /// whenever one of the observable sequences has a new value
    /// ===> More results than zip
    let combineLatest second first =
        let inner a b = a, b
        Observable.CombineLatest(first, second, inner)
    
    /// Concats (flattens) an observable of observables into an observable
    /// ===> Observable.SelectMany(observable, Func<_,_>(fun (x:IObservable<'T>) -> x))
    let concat (second: IObservable<'T>) (first: IObservable<'T>) = Observable.Concat(first, second)
    
    /// Maps the given observable with the given function
    let map f source = Observable.Select(source, Func<_,_>(f))    
     
    /// Maps the given observable with the given function
    let mapi f source = Observable.Select(source, flipApply f)

    /// Lifts the values of f and m and applies f to m, returning an IObservable of the result.
    let apply f m = f |> bind (fun f' -> m |> bind (fun m' -> Observable.Return(f' m')))

    /// Maps two observables to the specified function.
    let map2 f a b = apply (apply f a) b
     
    /// Filters all elements where the given predicate is satisfied
    let filter f source = Observable.Where(source, Func<_,_> f)

    /// Filters all elements where the given predicate is satisfied
    let filteri f source = Observable.Where(source, flipApply f)
     
    /// Skips n elements
    let skip (n: int) source = Observable.Skip(source, n)
     
    /// Skips elements while the predicate is satisfied
    let skipWhile f source = Observable.SkipWhile(source, Func<_,_> f)

    /// Skips elements while the predicate is satisfied
    let skipWhilei f source = Observable.SkipWhile(source, flipApply f)
     
    /// Counts the elements
    let count source = Observable.Count(source)
     
    /// Takes n elements
    let take (n: int) source = Observable.Take(source, n)    

    /// Determines whether the given observable is empty 
    let isEmpty source = source = Observable.Empty()

    /// Determines whether an observable sequence contains a specified value
    /// which satisfies the given predicate
    let exists f source = Observable.Any(source, f)

    /// Throttles the observable for the given interval
    let throttle (interval: TimeSpan) source = Observable.Throttle(source, interval)
    
    /// Samples the observable at the given interval
    let sample (interval: TimeSpan) source = Observable.Sample(source, interval)

    /// Continues an observable sequence that is terminated
    /// by an exception with the next observable sequence.
    let catch (second: IObservable<'T>) first = Observable.Catch(first, second) 
     
    /// Takes elements while the predicate is satisfied
    let takeWhile f source = Observable.TakeWhile(source, Func<_,_> f)

    /// Takes elements while the predicate is satisfied
    let takeWhilei f source = Observable.TakeWhile(source, flipApply f)
     
    /// Iterates through the observable and performs the given side-effect
    let perform f source = Observable.Do(source, Action<_> f)
     
    /// Invokes the finally action after source observable sequence terminates normally or by an exception.
    let performFinally f source = Observable.Finally(source, Action f)
     
    /// Folds the observable
    let fold f seed source = Observable.Aggregate(source, seed, f)

    /// Reduces the observable
    let reduce f source = Observable.Aggregate(source, f)
