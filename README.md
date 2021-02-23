## RailwayEssential 2.0 beta

RailwayEssential is a software for controlling your Model Trains especially when [ESU's ECoS 50210/50200](http://www.esu.eu/en/products/digital-control/ecos-50210-dcc-system/what-ecos-can-do/) is used. RailwayEssential provides a Track-designer, an Analyzer for automatic detection of all possible Routes between directly connected Blocks, and an Event-system (e.g. by use of S88-Feedback-Bus) for setup a fully automatic drive of all your trains. Furthermore, any Train can be manually controlled, their functions are allowed to be switched directly on and off by the user interface. 

### Impressions

![RailwayEssential Main Window](https://raw.githubusercontent.com/cbries/railwayessential/master/Documentation/Website/images/RailwayEssential-main.png)

![RailwayEssential Included Example 'SingleCircle'](https://raw.githubusercontent.com/cbries/railwayessential/master/Documentation/Website/images/RailwayEssential-main2.png)

![RailwayEssential Locomotive Control](https://raw.githubusercontent.com/cbries/railwayessential/master/Documentation/Website/images/RailwayEssential-Locomotive.png)

## Download Setup & Releases
Current test releases are available here: [Releases / Release Candidates / Other Setups](https://github.com/cbries/railwayessential/releases).

### First Step Tutorials
* Create your first Track: [Tutorial: Create a First Track](https://github.com/cbries/railwayessential/wiki/Tutorial:-Create-a-First-Track)

### Requirements (Hardware & Software)
- .Net 4.6 must be installed, [you can find it here: Website](https://www.microsoft.com/en-us/download/details.aspx?id=48130)
- Works smoothly on Windows 7 and newer systems (e.g. Windows 10)
- Support for x86 and x64
- Required HDD-space is approximately 150 MB
- We use CefSharp [(see Website)](https://github.com/cefsharp/CefSharp), therefore if on your computer Firefox or Chrome work, then anything is fine!

### Prerequisites

RailwayEssential is -- currently -- designed for supporting [ESU's ECoS 50210/50200](http://www.esu.eu/en/products/digital-control/ecos-50210-dcc-system/what-ecos-can-do/).

## Built With

* [VisualStudio 2019](https://www.visualstudio.com/vs/whatsnew/) - The IDE used for
* [.Net 4.6](https://www.microsoft.com/en-us/download/details.aspx?id=53344) - Microsoft .NET Framework

In case you like to build the software on your own. Just load the Solution file 'RailwayEssential.sln', set 'RailwayEssentialMdi' as startup poject, change the Achitecture to 'x86' or 'x64' and press 'F5'. Enjoy.

## Authors

* **Dr. Christian Benjamin Ries** - *Initial work*
 - [Personal Website](http://www.christianbenjaminries.de) 
 - [Email: mail@cbries.de](mailto:mail@cbries.de?subject=RailwayEssential)

See also the list of [contributors](https://github.com/cbries/railwayessential/graphs/contributors) who participated in this project.

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/cbries/railwayessential/blob/master/LICENSE) file for details
