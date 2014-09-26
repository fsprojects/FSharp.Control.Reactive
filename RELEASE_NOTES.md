### New in 2.3-rc - (TBD)
* Add QueryBuilder
* Add many more wrappers to Rx from F#, thanks to Jared Hester!

**Breaking changes**
* New namespace is `FSharp.Control.Reactive`
* `Observable` module is no longer auto-opened with the namespace
* `Observable.zip` now takes parameters in the "correct" order of `obs1` then `obs2`
* `Observable.subscribe` has moved to `Observable.subscribeWithCallbacks`
* `Observable.subscribe` now takes the `onNext` callback only

**TODO**
* Add tests
* Add docs

### New in 2.2.131002 - (Released 2013/10/02)
* Restructure modules and namespaces to comply with F# coding guidelines

### New in 2.1.131002 - (Released 2013/10/02)
* Release of F# wrappers for Rx

### New in 2.0.121121-rc - (Released 2012/11/21)
* Release candidate for F# wrappers for Rx based on Rx 2.0-rtm

### New in 2.0.6-rc - (Released 2012/07/26)
* Initial NuGet release of F# wrappers for Rx 2.0-rc
