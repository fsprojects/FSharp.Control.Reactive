// --------------------------------------------------------------------------------------
// FAKE build script 
// --------------------------------------------------------------------------------------

#I "packages/build/FAKE/tools"
#r "NuGet.Core.dll"
#r "FakeLib.dll"
open System
open System.IO
open Fake 
open Fake.Git
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open Fake.Testing.NUnit3

// --------------------------------------------------------------------------------------
// Provide project-specific details below
// --------------------------------------------------------------------------------------

// Information about the project are used
//  - for version and project name in generated AssemblyInfo file
//  - by the generated NuGet package 
//  - to run tests and to publish documentation on GitHub gh-pages
//  - for documentation, you also need to edit info in "docs/tools/generate.fsx"

// The name of the project 
// (used by attributes in AssemblyInfo, name of a NuGet package and directory in 'src')
let project = "FSharp.Control.Reactive"

// Short summary of the project
// (used as description in AssemblyInfo and as a short summary for NuGet package)
let summary = "A F#-friendly wrapper for the Reactive Extensions."

// Longer description of the project
// (used as a description for NuGet package; line breaks are automatically cleaned up)
let description = """
  A F#-friendly wrapper for the Reactive Extensions."""
// List of author names (for NuGet package)
let authors = ["Ryan Riley"; "Steffen Forkmann"; "Jared Hester"]
// Tags for your project (for NuGet package)
let tags = "F# fsharp reactive extensions rx"

// File system information 
// (<solutionFile>.sln is built during the building process)
let solutionFile = "FSharp.Control.Reactive.sln"

// Git configuration (used for publishing documentation in gh-pages branch)
// The profile where the project is posted 
let gitHome = "git@github.com:fsprojects"
// The name of the project on GitHub
let gitName = "FSharp.Control.Reactive"
let gitRaw = environVarOrDefault "gitRaw" "https://raw.github.com/fsprojects"

let buildDir = "bin"


// --------------------------------------------------------------------------------------
// The rest of the file includes standard build steps 
// --------------------------------------------------------------------------------------

// Read additional information from the release notes document
Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let (!!) includes = (!! includes).SetBaseDirectory __SOURCE_DIRECTORY__
let release = parseReleaseNotes (IO.File.ReadAllLines "RELEASE_NOTES.md")
let isAppVeyorBuild = environVar "APPVEYOR" <> null
let nugetVersion = 
    if isAppVeyorBuild then
        // If the `release.NugetVersion` contains a preview release, just append the `buildVersion`.
        if release.NugetVersion.Contains("-") then
            sprintf "%s%s" release.NugetVersion buildVersion
        else sprintf "%s.%s" release.NugetVersion buildVersion
    else release.NugetVersion

// Generate assembly info files with the right version & up-to-date information
Target "AssemblyInfo" (fun _ ->
  let fileName = "src/FSharp.Control.Reactive/AssemblyInfo.fs"
  CreateFSharpAssemblyInfo fileName
      [ Attribute.Title project
        Attribute.Product project
        Attribute.Description summary
        Attribute.Version release.AssemblyVersion
        Attribute.FileVersion release.AssemblyVersion ] )

Target "BuildVersion" (fun _ ->
    Shell.Exec("appveyor", sprintf "UpdateBuild -Version \"%s\"" nugetVersion) |> ignore
)

// --------------------------------------------------------------------------------------
// Clean build results & restore NuGet packages

Target "Clean" (fun _ ->
    CleanDirs ["bin"; "temp"]
)

Target "CleanDocs" (fun _ ->
    CleanDirs ["docs/output"]
)

// --------------------------------------------------------------------------------------
// Build library & test project

Target "Build" (fun _ ->
    // We need to invoke restore in order to create the .NetCore obj files
    DotNetCli.Build (fun defaults ->
        { defaults with
            Project = solutionFile
            Configuration = "Release" })
)

Target "CopyLicense" (fun _ ->
    [ "LICENSE.txt" ] |> CopyTo "bin"
)

// --------------------------------------------------------------------------------------
// Run the unit tests using test runner

Target "RunTests" (fun _ ->
    try
        DotNetCli.Test(fun p ->
            { p with
                WorkingDir = "bin"
                Project = "../tests"
                Configuration = "Release"
                TimeOut = TimeSpan.FromMinutes 20. })
    finally
        AppVeyor.UploadTestResultsXml AppVeyor.TestResultsType.NUnit "bin"
)

// --------------------------------------------------------------------------------------
// Build a NuGet package

Target "NuGet" (fun _ ->
    Paket.Pack (fun p -> 
        { p with 
            Version = release.NugetVersion
            OutputPath = buildDir
            ReleaseNotes = toLines release.Notes })
)

// --------------------------------------------------------------------------------------
// Generate the documentation


Target "PublishNuGet" (fun _ ->
    Paket.Push (fun p -> 
        { p with
            WorkingDir = buildDir }) 
)


Target "GenerateReferenceDocs" (fun _ ->
    if not <| executeFSIWithArgs "docs/tools" "generate.fsx" ["--define:RELEASE"; "--define:REFERENCE"] [] then
      failwith "generating reference documentation failed"
)

Target "GenerateHelp" (fun _ ->
    if not <| executeFSIWithArgs "docs/tools" "generate.fsx" ["--define:RELEASE"; "--define:HELP"] [] then
      failwith "generating help documentation failed"
)

Target "GenerateDocs" DoNothing

// --------------------------------------------------------------------------------------
// Release Scripts

Target "ReleaseDocs" (fun _ ->
    let tempDocsDir = "temp/gh-pages"
    CleanDir tempDocsDir
    Repository.cloneSingleBranch "" (gitHome + "/" + gitName + ".git") "gh-pages" tempDocsDir

    CopyRecursive "docs/output" tempDocsDir true |> tracefn "%A"
    StageAll tempDocsDir
    Commit tempDocsDir (sprintf "Update generated documentation for version %s" release.NugetVersion)
    Branches.push tempDocsDir
)

Target "Release" (fun _ ->
    StageAll ""
    Commit "" (sprintf "Bump version to %s" release.NugetVersion)
    Branches.push ""

    Branches.tag "" release.NugetVersion
    Branches.pushTag "" "origin" release.NugetVersion
)

Target "BuildPackage" DoNothing

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target "All" DoNothing

"Clean"
  =?> ("BuildVersion", isAppVeyorBuild)
  ==> "CopyLicense"
  ==> "AssemblyInfo"
  ==> "Build"
  ==> "RunTests"
  ==> "All"
  =?> ("GenerateReferenceDocs",isLocalBuild && not isMono)
  =?> ("GenerateDocs"         ,isLocalBuild && not isMono)
  =?> ("ReleaseDocs"          ,isLocalBuild && not isMono)

"All" 
  ==> "NuGet"
  ==> "BuildPackage"

"NuGet"
  ==> "PublishNuget"

"CleanDocs"
  ==> "GenerateHelp"
  ==> "GenerateReferenceDocs"
  ==> "GenerateDocs"
    
"ReleaseDocs"
  ==> "Release"

"BuildPackage"
  ==> "Release"

RunTargetOrDefault "All"
