namespace FSharp.Control.Reactive.Testing

/// Reactive Marbles representation to make working with recorded test notifications like visualising the notifications as marbles.
module Marbles =

    open System
    open Microsoft.Reactive.Testing
    open FSharp.Control.Reactive.Testing
    open FSharp.Control.Reactive.Testing.TestNotification
    open FParsec
    open Swensen.Unquote

    type private Token<'a> =
        | Next of time:int64 * value:'a
        | Error of time:int64 * ex:exn
        | Done of time:int64

    let private mapTime f = function
        | Next (t, v) -> Next (f t, v)
        | Error (t, ex) -> Error (f t, ex)
        | Done t -> Done (f t)

    let private toNotifications xs =
        List.fold (fun (time, ms) x ->
            match x with
            | Next (t, v) -> time + t, onNext (time + t) v
            | Error (t, ex) -> time + t, onError (time + t) ex
            | Done t -> time + t, onCompleted (time + t)
            |> fun (t, n) -> t, n :: ms) (0L, []) xs
        |> snd
        |> List.rev

    type private Temperature = Hot | Cold

    let private frameMultiplier = 10L

    let private frsToTime xs =
        List.map (fun _ -> frameMultiplier) xs
        |> List.fold (+) 0L

    let private pframes = many (pchar '-')

    let private parseMarbles temp txt =
        let txt = txt |> String.filter (fun c -> c <> ' ')

        let pvalueOne = noneOf [ '#'; '^'; '|'; '('; ')' ]
        let pvalueMultiple =
            between
                (pchar '(')
                (pchar ')')
                (many1 pvalueOne |>> (Array.ofList >> String))

        let pvaluewrap = (pvalueOne |>> string) <|> pvalueMultiple
        let pframevalue =
            pframes .>>. pvaluewrap
            |>> fun (frs, x) -> Next (frsToTime frs, x)

        let pframeError =
            pframes .>> pchar '#'
            |>> fun frs -> Error (frsToTime frs, exn "error")

        let pemit =
            attempt pframevalue
            <|> attempt pframeError

        let pdone =
            attempt (pframes .>> pchar '|' |>> (frsToTime >> Some))
            <|> (pframes |>> fun _ -> None)

        let pemitsdone =
            many pemit .>>. pdone
            |>> fun (cs, c) ->
                match c with
                | Some x -> cs @ [Done x]
                | None -> cs

        let psub =
            pframes .>> pchar '^'
            |>> (frsToTime >> (+) frameMultiplier)

        let pemitssub =
            (many pemit) .>> psub .>>. pemitsdone
            |>> fun (xs, ys) ->
                let xs = xs |> List.map (mapTime ((*) -1L))
                xs @ ys

        match temp with
        | Hot -> attempt pemitssub <|> pemitsdone
        | Cold -> pemitsdone
        |> fun p -> run p txt
        |> function
            | Success (r, _, _) -> toNotifications r |> TestNotifications
            | Failure (msg, _, _) -> failwith msg

    let private parseMarblesAsSubscription txt =
        let txt = txt |> String.filter (fun c -> c <> ' ')
        let psub = pframes .>> pchar '^' |>> frsToTime
        let punsub = pframes .>> pchar '!' |>> (frsToTime >> (+) frameMultiplier)
        let psubscriptions = psub .>>. punsub |>> fun (s, u) -> Subscription (s, s + u)

        match run psubscriptions txt with
        | Success (r, _, _) -> r
        | Failure (msg, _, _) -> failwith msg

    /// Creates from the given marble representation a Cold Observable
    /// using the specified `TestScheduler`.
    /// Note that the marble representation can't have a subscription marker (`^`),
    /// only Hot Observables can have these.
    ///
    /// ## Parameters
    /// - `x` - Marble text representation. Example: `--a---b--|`.
    /// - `sch` - The given `TestScheduler` to use to create the Cold Observable.
    let cold x sch = parseMarbles Cold x |> TestSchedule.coldObservable sch

    /// Creates from the given marble representation a Hot Observable
    /// using the specified `TestScheduler`.
    ///
    /// ## Parameters
    /// - `x` - Marble text represetantion. Example: `--^-a---b--|`.
    ///- `sch` - The given `TestScheduler` to use to create the Hot Observable.
    let hot x sch = parseMarbles Hot x |> TestSchedule.hotObservable sch

    /// Creates from the given marble representation a `TestNotifications<'a>` model.
    let messages txt =
        parseMarbles Cold txt

    /// Verifies that the given marble representation is indeed the same as the observed messages found in the given test observer.
    let expectMessages sch txt obs =
        TestSchedule.subscribeTestObserverStart sch obs
        |> TestObserver.messages =! (parseMarbles Cold txt)

    /// Creates from the given marble representation a `Subscription` model.
    let subscription = parseMarblesAsSubscription

    /// Creates from the given marble representation a `Subscription` list model.
    let subscriptions = List.map parseMarblesAsSubscription

    /// Verifies that the given marble representation is indeed the same as the subscription found in the given test observable sequence.
    let expectSubscription txt xs =
        subscription txt |> (=!) xs

    /// Verifies that the given marble representation is indeed the same as the subscriptions found in the given test observable sequence.
    let expectSubscriptions txt xs =
        subscriptions txt |> (=!) xs