
## RailEssentials - MIT License

**~~RailwaysEssentials~~** is renamed to **RailEssentials**.

**[`RailEssentials`](http://www.railessentials.net)** is a modern and intuitive software for creating and controling
model railways. Currently, the only supported control unit 
is [ESU's ECoS 50210/50200](http://www.esu.eu/produkte/digitale-steuerung/ecos-50210-zentrale/was-ecos-kann/). 
The software is based on a client-server architecture, i.e the server is the brain and bridge between the control unit
and any conntected webclient. The server is written in C# and the client is written in JavaScript/TypeScript and can be
used in [Chrome](https://www.google.com/chrome/), [Firefox](https://www.mozilla.org/en-US/firefox/new/), 
and [Vivaldi](https://vivaldi.com/). 

## System Requirements

- Windows 10
- .NET Framework 4.8 or higher
- Chrome, Firefox or Vivaldi

## Releases

We do not provide any official releases yet, because the software is still under development.
In January'21 the RailEssentials team decided to do a full rewrite.
To use this software, checkout the source, and just click "Build and Run (F5)" in VisualStudio.

## Who is Using It?

If you're using RailEssentials, I'd love to hear about it, please email to `mail@cbries.de` 
the name of your project, attach few screenshots and your plan, and if you have, some ideas
of improvements. 

## Quick Start for Developer

- install [`VisualStudio 2019`](https://visualstudio.microsoft.com/vs/) or the old variant [`VisualStudio 2017`](https://visualstudio.microsoft.com/vs/older-downloads/)
- clone the RailEssentials repository `git clone https://github.com/cbries/railessentials.git` or download the latest archive [master.zip](https://github.com/cbries/railessentials/archive/master.zip)
- open the solution `railessentials.sln`
- set `railessentials` as _Startup Project_
- click `F5` or call `VisualStudio Toolbar / Debug / Start Debugging`

As a result the software will build and all relevant files are copied to the `Output Directory`, in general this is `railessentials \ bin \ Debug|Release`. `railessentials.exe` will be executed, a command prompt opens and shows some status information. If not browser opens automatially, go to your browser and open `http://localhost:8081/?workspace=Basement`.

Finally, you should see something like this:
![firstImpressionsAfterBuild]

[firstImpressionsAfterBuild]: docs/images/firstImpressionAfterBuild.png "Welcome View of RailEssentials after Build and Run"

## Documentation & Demos

To be defined

## Bug Tracking

Have a bug or a feature request? Please open an issue here [https://github.com/cbries/railessentials/issues](https://github.com/cbries/railessentials/issues). Please make sure that the same issue was not previously submitted by someone else.

## First Steps

To be defined, probably will be provided under `Wiki`.

<br><br><br>

# First Impressions

## Locomotive Control

![Locomotives]

![LocomotivesView]

![LocomotivesControl]

## Accessories

![Accessories]

## Blocks and S88

![BlocksS88]

![S88Viewer]

## Routing

![Routes]

![RoutesAnalyzing]

## Edit

![Labels]

![Toolbox]

![WorkspaceSelection]

[Locomotives]: Screenshots/Impressions/RailEssentials-Locomotives.png "Locomotives View and Handling"
[LocomotivesControl]: Screenshots/Impressions/RailEssentials-LocomotivesControl.png "Locomotives Control directly in the Plan"
[LocomotivesView]: Screenshots/Impressions/RailEssentials-LocomotivesView.png "Locomotives View"

[Accessories]: Screenshots/Impressions/RailEssentials-Accessories.png "Accessories"

[BlocksS88]: Screenshots/Impressions/RailEssentials-BlocksS88.png "Blocks and S88"
[S88Viewer]: Screenshots/Impressions/RailEssentials-S88Viewer.png "S88 Viewer"

[Routes]: Screenshots/Impressions/RailEssentials-Routes.png "Routes"
[RoutesAnalyzing]: Screenshots/Impressions/RailEssentials-RoutesAnalyzing.png "Routes Analyzing"

[Labels]: Screenshots/Impressions/RailEssentials-Labels.png "Labels in Track"
[Toolbox]: Screenshots/Impressions/RailEssentials-Toolbox.png "Toolbox to create any Plan individually"
[WorkspaceSelection]: Screenshots/Impressions/RailEssentials-WorkspaceSelection.png "Workspace Selection"
