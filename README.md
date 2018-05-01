# Docker and Kubernetes

This repository demostrates using Docker and Kubernetes to run
.NET Core based system.

Aim is to have a realistic demo, but still simple enough to be able to present
it in one hour session.

## System Overview

For the purpose of this demo, we'll build a system that consists of the
following parts:

- **API**. A very simple API that provides access to collection of
  `key-value` pairs.
- **Command-line client**. Non-interactive client that continuously sends
  commands to the API.
- **MongoDB** will be used as database server to store our `key-value` pairs.
- **Elastic Search** will be storing logs from both API and the client.

All the components will be wrapped in [Docker](https://www.docker.com/ "Docker")
containers and run in [Kubernetes](https://kubernetes.io/).

## Technology Stack

### .NET Core

REST API and the client will be implemented using .NET Core.

From [Wikipedia](https://en.wikipedia.org/wiki/.NET_Framework#.NET_Core ".NET Core")

>.NET Core is a cross-platform free and open-source managed software framework
>similar to .NET Framework. It consists of CoreCLR, a complete cross-platform
>runtime implementation of CLR, the virtual machine that manages the execution
>of .NET programs. CoreCLR comes with an improved just-in-time compiler,
>called RyuJIT. .NET Core also includes CoreFX, which is a partial fork of FCL.
>
>.NET Core's command-line interface offers an execution entry point for
>operating systems and provides developer services like compilation and
>package management.
>
>.NET Core supports four cross-platform scenarios: ASP.NET Core web apps,
>command-line apps, libraries, and Universal Windows Platform apps.
>It does not implement Windows Forms or WPF which render the standard GUI
>for desktop software on Windows. .NET Core is also modular, meaning that
>instead of assemblies, developers work with NuGet packages.
>Unlike .NET Framework, which is serviced using Windows Update, .NET Core
>relies on its package manager to receive updates.
>

.NET Core is an open-source project hosted on GitHub,
<https://github.com/dotnet/core>; and you can find overview, downloads,
learning and documentation resources at
<https://www.microsoft.com/net/core>.

.NET Core 1.0 was released on 27 June 2016, along with
Visual Studio 2015 Update 3, which enables .NET Core development.
.NET Core 1.1 was announced later that year <https://blogs.msdn.microsoft.com/dotnet/2016/11/16/announcing-net-core-1-1/>,
and was included in Visual Studio 2017.

Note that pre-release versions of .NET Core and version 1.0 included support
for JSON-based project format (`project.json`). The `project.json` is no more
in .NET Core version 1.1 and Visual Studio 2017, Microsoft decided to
go back to XML-based `.csproj` with some optimizations to reduce verbosity.

- <https://blogs.msdn.microsoft.com/dotnet/2016/11/16/announcing-net-core-tools-msbuild-alpha/>
- <https://www.stevejgordon.co.uk/project-json-replaced-by-csproj>
- <https://csharp.christiannagel.com/2017/01/10/dotnetcoreversionissues/>

### Docker

From [the horse's mouth](https://www.docker.com/what-docker "What is Docker"):

>Docker is the world’s leading software container platform.
>Developers use Docker to eliminate “works on my machine”
>problems when collaborating on code with co-workers.
>Operators use Docker to run and manage apps side-by-side
>in isolated containers to get better compute density.
>Enterprises use Docker to build agile software delivery
>pipelines to ship new features faster, more securely and
>with confidence for both Linux and Windows Server apps.

Docker is all about *containers*. A container is a piece
of software running in isolation.

Containers only bundle dependencies required to run
the software, unlike a Virtual Machine that isolates
full operating system.

### Kubernetes

From [the horse's mouth](https://kubernetes.io/ "Kubernetes"):

>Kubernetes is an open-source system for automating deployment, scaling,
>and management of containerized applications. It groups containers that
>make up an application into logical units for easy management and discovery.

## Step 0. Prerequisites

Install [.NET Core SDK](https://www.microsoft.com/net/download/core ".NET Core"),
and [Docker for Windows](https://docs.docker.com/docker-for-windows/ "Docker for Windows")
Edge channel (at the moment of writing Kubernetes support is only available in
Edge channel), and enable Kubernetes support.

## Step 1. Solution Skeleton

Create `src` and `build` directories, so that the top-level structure
looks like this:

    build\
    src\
    README.md

In command prompt, change directory to the `<project directory>\src`, and
run following commands:

    dotnet new sln --name Containers
    dotnet new classlib --name Infrastructure.Logging
    dotnet new webapi --name WebApi
    dotnet new xunit --name WebApi.Test.Unit
    dotnet new console --name Client

    dotnet sln .\Containers.sln add .\Infrastructure.Logging\Infrastructure.Logging.csproj
    dotnet sln .\Containers.sln add .\WebApi\WebApi.csproj
    dotnet sln .\Containers.sln add .\WebApi.Test.Unit\WebApi.Test.Unit.csproj
    dotnet sln .\Containers.sln add .\Client\Client.csproj

    dotnet add .\WebApi\WebApi.csproj reference .\Infrastructure.Logging\Infrastructure.Logging.csproj
    dotnet add .\Client\Client.csproj reference .\Infrastructure.Logging\Infrastructure.Logging.csproj
    dotnet add .\WebApi.Test.Unit\WebApi.Test.Unit.csproj reference .\WebApi\WebApi.csproj
    dotnet add .\WebApi.Test.Unit\WebApi.Test.Unit.csproj reference .\Infrastructure.Logging\Infrastructure.Logging.csproj

As a result, the soluition structure will look like this:

    build\
    src\
      Client\
      Infrastructure.Logging\
      WebApi\
      WebApi.Test.Unit\

Build the solution to make sure that everything was done correctly:

    dotnet build .\Containers.sln
    dotnet test .\WebApi.Test.Unit\WebApi.Test.Unit.csproj
    dotnet publish .\Containers.sln

You may add a build script to automate the steps above,
this repository uses PowerShell-based `build.ps1` script.

See tag [Step_01](https://github.com/iblazhko/containers-demo/releases/tag/Step_01 "Step_01") in this repository for reference implementation.

## Step 2. Implementing WebAPI and Client

### Step 2.1 WebAPI

In this step we'll add simplest possible API implementation based on `Dictionary<string,string>`.
The purpose of this step is just to see that API starts and responds to requests, so values will not persist after API restart; we'll add persistent storage in later steps.

Modify `WebApi\Controllers\ValueController` to implement
`GET`, `POST`, `PUT`, `DELETE` operations using static
`Dictionary<string,string>` as values repository. Note that the dictionary has to be static because ASP.NET Core will create new controller instance per request.

Run the API. Change directory to `src\WebApi` and run command

    dotnet run

You should see output

    Now listening on: http://*:5000
    Application started. Press Ctrl+C to shut down.

Use `curl` or `Postman` client to test the API.

See tag [Step_02_1](https://github.com/iblazhko/docker-dotnetcore-demo/releases/tag/Step_02_1 "Step_02_1") in this repository for reference implementation.

### Step 2.2 Client

In this step we'll add a client that will be continuously sending
random requests to the API. The purpose of this client is to
emulate some system activity.

Add `Configuration` packages and `appsettings.json` settings file:

    dotnet add .\Client\Client.csproj package Microsoft.Extensions.Configuration
    dotnet add .\Client\Client.csproj package Microsoft.Extensions.Configuration.CommandLine
    dotnet add .\Client\Client.csproj package Microsoft.Extensions.Configuration.Json


    appsettings.json:
    {
        "ApiUrl": "http://localhost:5000/api",
        "MaxDelay": "00:00:05"
    }

Modify `Client` project to send `GET`, `POST`, `PUT`, and `DELETE`
commands periodically to the API.

Run the API. Change directory to `src\WebApi` and run command

    dotnet run

You should see output

    Now listening on: http://*:5000
    Application started. Press Ctrl+C to shut down.

Run the client. Leave the API running; in a new command prompt
change directory to `src\Client` and run command

    dotnet run

You should see that the client is sending random commands, e.g.

    REST API Random Test Client. API Url: http://localhost:5000/api
    GET http://localhost:5000/api/values
    POST http://localhost:5000/api/values
    GET http://localhost:5000/api/values
    GET http://localhost:5000/api/values/5baa8239-70b4-42d6-a360-1cc1c73ce9ac
    GET http://localhost:5000/api/values/5baa8239-70b4-42d6-a360-1cc1c73ce9ac
    DELETE http://localhost:5000/api/values/5baa8239-70b4-42d6-a360-1cc1c73ce9ac

See tag [Step_02_2](https://github.com/iblazhko/docker-dotnetcore-demo/releases/tag/Step_02_2 "Step_02_2") in this repository for reference implementation.

### Step 2.3 Logging

To ensure that system activity is logged consistently, add logging implementation
to `Infrastracture.Logging`; modify `WebApi` and `Client` projects to use that
implementation and log activity to console.

This repository uses [Microsoft.Diagnostics.EventFlow](https://github.com/Azure/diagnostics-eventflow/ "Microsoft.Diagnostics.EventFlow")
as underlying implementation.

    dotnet add .\Infrastructure.Logging\Infrastructure.Logging.csproj package Microsoft.Diagnostics.EventFlow

    dotnet add .\Client\Client.csproj package Microsoft.Diagnostics.EventFlow
    dotnet add .\Client\Client.csproj package Microsoft.Diagnostics.EventFlow.Outputs.StdOutput
    dotnet add .\Client\Client.csproj package Microsoft.Diagnostics.EventFlow.Outputs.ElasticSearch

    dotnet add .\WebApi\WebApi.csproj package Microsoft.Diagnostics.EventFlow
    dotnet add .\WebApi\WebApi.csproj package Microsoft.Diagnostics.EventFlow.Outputs.StdOutput
    dotnet add .\WebApi\WebApi.csproj package Microsoft.Diagnostics.EventFlow.Outputs.ElasticSearch

In later steps we will add [ElasticSearch sink](https://github.com/serilog/serilog-sinks-elasticsearch "ElasticSearch sink") to send logs to centralized storage.

See tag [Step_02_3](https://github.com/iblazhko/docker-dotnetcore-demo/releases/tag/Step_02_3 "Step_02_3") in this repository for reference implementation.
