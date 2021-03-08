
## RailEssentials - MIT License

[![Build and UnitTests](https://github.com/cbries/railessentials/actions/workflows/msbuild.yml/badge.svg)](https://github.com/cbries/railessentials/actions/workflows/msbuild.yml)

[![Deploy Nightly](https://github.com/cbries/railessentials/actions/workflows/dailyBuild.yml/badge.svg)](https://github.com/cbries/railessentials/actions/workflows/dailyBuild.yml)

**~~RailwaysEssentials~~** is renamed to **RailEssentials**.

**[`RailEssentials`](http://www.railessentials.net)** is a modern and intuitive software for creating and controling
model railways. Currently, the only supported control unit 
is [ESU's ECoS 50210/50200](http://www.esu.eu/produkte/digitale-steuerung/ecos-50210-zentrale/was-ecos-kann/). 
The software is based on a client-server architecture, i.e the server is the brain and bridge between the control unit
and any conntected webclient. The server is written in C# and the client is written in JavaScript/TypeScript and can be
used in [Chrome](https://www.google.com/chrome/), [Firefox](https://www.mozilla.org/en-US/firefox/new/), 
and [Vivaldi](https://vivaldi.com/). 

## Daily Builds

`RailEssentials` is automatically build everyday. The always latest version is provided under the `Release` tab: [`Daily Build`](https://github.com/cbries/railessentials/releases/tag/dailybuild)
- download the archive `railessentials-dailybuild-*.zip`
- after unzip just call the included script `startRailEssentials.bat`

As default the workspace `Basement` is loaded and provided and can be used directly in simulation mode.

![Locomotives]

## Releases

We do not provide any official releases yet, because the software is still under development, but we provide a [`Daily Build`](https://github.com/cbries/railessentials/releases/tag/dailybuild). In January'21 the RailEssentials team decided to do a full rewrite. To use this software, checkout the source, and just click "Build and Run (F5)" in VisualStudio.

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

## System Requirements

- Windows 10
- .NET Framework 4.8 or higher
- Chrome, Firefox or Vivaldi

## Documentation & Demos

To be defined

## Bug Tracking

Have a bug or a feature request? Please open an issue here [https://github.com/cbries/railessentials/issues](https://github.com/cbries/railessentials/issues). Please make sure that the same issue was not previously submitted by someone else.

## First Steps

To be defined, probably will be provided under `Wiki`.

## Third-Party Components at Their Best

- [`w2ui`](http://w2ui.com/web/) JavaScript UI Library for the Modern Web
- [`WebSocket4Net`](https://github.com/kerryjiang/WebSocket4Net) A popular .NET WebSocket Client
- [`SuperSocket`](https://github.com/kerryjiang/SuperSocket) SuperSocket is a light weight extensible socket application framework.
- [`Newtonsoft`](https://www.newtonsoft.com/json) Popular high-performance JSON framework for .NET
- [`jQuery`](https://jquery.com/) jQuery is a fast, small, and feature-rich JavaScript library.
- [`jQuery colorpicker`](https://github.com/vanderlee/colorpicker) A full-featured colorpicker for jQueryUI with full theming support.
- [`Contextual.js`](https://github.com/LucasReade/Contextual.js) Javascript contextual menu library
- [`select2`](https://github.com/select2/select2) Select2 is a jQuery based replacement for select boxes. It supports searching, remote data sets, and infinite scrolling of results.
- [`FontAwesome`](https://fontawesome.com/) Get vector icons and social logos on your website with Font Awesome, the web's most popular icon set and toolkit.

<br><br>

# Impressions

## Locomotive Control

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
