##FSharp.Reactive

This library is a small wrapper around reactive extensions that allow easier usage from F#.  See the [Some kind of monster][0] post by Dave Thomas for more details of this.  

###Windows

Under Windows you should build using either [Fake][1] or using Visual Studio.  

###Mono

Under Mono Rx is packaged as part of Mono 3.x so there is no Nuget dependency on Rx in the main project.  The Test project does not currently compile under Mono as there are still some Windows dependencies.  Also Fake has a few issues on non Windows machines so you are advised to build using Xamarin Studio, excluding the test project from the build.    


[0]: http://7sharpnine.com/posts/some-kind-of-monster/
[1]: https://github.com/fsharp/FAKE

