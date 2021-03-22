---
title: About
permalink: /about/
---

# About

**[`RailEssentials`](http://www.railessentials.net)** is a modern and intuitive software for creating and controling
model railways. Currently, the only supported control unit 
is [ESU's ECoS 50210/50200](http://www.esu.eu/produkte/digitale-steuerung/ecos-50210-zentrale/was-ecos-kann/). 
The software is based on a client-server architecture, i.e the server is the brain and bridge between the control unit
and any conntected webclient. The server is written in C# and .NET Framework and the client is written in JavaScript/TypeScript and can be
used in [Chrome](https://www.google.com/chrome/), [Firefox](https://www.mozilla.org/en-US/firefox/new/), 
and [Vivaldi](https://vivaldi.com/). 

# Book

In the late of 2018 I wrote a book about programming the ESU's ECoS 50210.

[![Book](/assets/img/ecosbook.jpg)](https://www.christianbenjaminries.de/ecos) 

* ISBN: **978-1-790-36403-9**    
* ASIN (Kindle): **B07L4Z4MYL**

The book introduces the basics of managing commands for remote control of your model railway by use of C# and .NET Framework/.net Core. The included examples and API information are all used in [`ecos`](https://github.com/cbries/ecos), a software library hosted on GitHub which is completly used by `RailEssentials` (with few improvements and bug fixes). The source is completly merged into `RailEssentials`, i.e. check out the subdirectory [`ecoslibNet48`](https://github.com/cbries/railessentials/tree/master/ecoslibNet48).

## Support

If you need help, please don't hesitate to [open an issue](https://www.github.com/{{ site.github_user }}/{{ site.github_repo }}).

