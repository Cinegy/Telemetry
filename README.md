# Cinegy Telemetry Library

This library is really a library supporting other Cinegy open-source projects that need telemetry to flow to the Cinegy Telemetry service. However, we open-sourced the library in case others want to learn, so you can see how our telemetry works (and verify we don't use it for backdoors or unexpected data collection), and because it's actually much easier to include a fully open-source library into our open-source projects than combine our internal-worlds with this external-world.

## What can I do with it?

Not a lot - we make this library to make it easier for our tooling to talk back to us, and then can include it into our open-source projects so users of binaries of these tools can opt-in to telemetry. The goal at the moment is not to make this library fit your needs - it's about our needs :-)

You can now control the destination of the telemetry for any application that consumes this library - set an environment variable with the name 'OVERRIDE_CINEGY_TELEMETRY_TARGET' to a URL of a suitable ElasticSearch server (you can target a few by separating via CSV) or some other ES compatible ingest proxy (this is what we do at Cinegy). If this override is not set, any logs will go to the defaults set by the application (likely https://telemetry.cinegy.com). Some applications can offer an explicit override - the presence of this ENV VAR will override this.

## Getting the library

Just to make our life easier, we auto-build this using AppVeyor and push to NuGet - here is how we are doing right now for MASTER: 

[![Build status](https://ci.appveyor.com/api/projects/status/o2ohedex2a596gfn/branch/master?svg=true)](https://ci.appveyor.com/project/cinegy/telemetry/branch/master)

You can check out the latest compiled binary from the master branch here:

[AppVeyor Telemetry Project Builder](https://ci.appveyor.com/project/cinegy/telemetry/build/artifacts)