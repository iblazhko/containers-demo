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

    dotnet new sln --name DockerDotNetCore
    dotnet new classlib --name Infrastructure.Logging
    dotnet new webapi --name WebApi
    dotnet new xunit --name WebApi.Test.Unit
    dotnet new console --name Client

    dotnet sln .\DockerDotNetCore.sln add .\Infrastructure.Logging\Infrastructure.Logging.csproj
    dotnet sln .\DockerDotNetCore.sln add .\WebApi\WebApi.csproj
    dotnet sln .\DockerDotNetCore.sln add .\WebApi.Test.Unit\WebApi.Test.Unit.csproj
    dotnet sln .\DockerDotNetCore.sln add .\Client\Client.csproj

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

    dotnet restore .\DockerDotNetCore.sln
    dotnet build .\DockerDotNetCore.sln
    dotnet test .\WebApi.Test.Unit\WebApi.Test.Unit.csproj
    dotnet publish .\DockerDotNetCore.sln

You may add a build script to automate the steps above,
this repository uses PowerShell-based `build.ps1` script.

See tag [Step_01](https://github.com/iblazhko/containers-demo/releases/tag/Step_01 "Step_01") in this repository for reference implementation.