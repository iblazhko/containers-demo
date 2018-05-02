# Docker and Kubernetes

This repository demonstrates using Docker and Kubernetes to run
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

```cmd
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
```

As a result, the solution structure will look like this:

    build\
    src\
      Client\
      Infrastructure.Logging\
      WebApi\
      WebApi.Test.Unit\

Build the solution to make sure that everything was done correctly:

```cmd
dotnet build .\Containers.sln
dotnet test .\WebApi.Test.Unit\WebApi.Test.Unit.csproj
dotnet publish .\Containers.sln
```

You may add a build script to automate the steps above,
this repository uses PowerShell-based `build.ps1` script.

See tag [Step_01](https://github.com/iblazhko/containers-demo/releases/tag/Step_01 "Step_01")
in this repository for reference implementation.

## Step 2. Implementing WebAPI and Client

### Step 2.1 WebAPI

In this step we'll add simplest possible API implementation based on
`Dictionary<string,string>`. The purpose of this step is just to see that
API starts and responds to requests, so values will not persist after
API restart; we'll add persistent storage in later steps.

Modify `WebApi\Controllers\ValueController` to implement
`GET`, `POST`, `PUT`, `DELETE` operations using static
`Dictionary<string,string>` as values repository. Note that the dictionary has
to be static because ASP.NET Core will create new controller instance per
request.

Run the API. Change directory to `src\WebApi` and run command

```cmd
dotnet run
```

You should see output

```txt
Now listening on: http://*:5000
Application started. Press Ctrl+C to shut down.
```

Use `curl` or `Postman` client to test the API.

See tag [Step_02_1](https://github.com/iblazhko/containers-demo/releases/tag/Step_02_1 "Step_02_1") in this repository for reference implementation.

### Step 2.2 Client

In this step we'll add a client that will be continuously sending
random requests to the API. The purpose of this client is to
emulate some system activity.

Add `Configuration` packages and `appsettings.json` settings file:

```cmd
dotnet add .\Client\Client.csproj package Microsoft.Extensions.Configuration
dotnet add .\Client\Client.csproj package Microsoft.Extensions.Configuration.CommandLine
dotnet add .\Client\Client.csproj package Microsoft.Extensions.Configuration.Json
```

`appsettings.json`:

```json
{
  "ApiUrl": "http://localhost:5000/api",
  "MaxDelay": "00:00:05"
}
```

Modify `Client` project to send `GET`, `POST`, `PUT`, and `DELETE`
commands periodically to the API.

Run the API. Change directory to `src\WebApi` and run command

```cmd
dotnet run
```

You should see output

```txt
Now listening on: http://*:5000
Application started. Press Ctrl+C to shut down.
```

Run the client. Leave the API running; in a new command prompt
change directory to `src\Client` and run command

```cmd
dotnet run
```

You should see that the client is sending random commands, e.g.

```txt
REST API Random Test Client. API Url: http://localhost:5000/api
GET http://localhost:5000/api/values
POST http://localhost:5000/api/values
GET http://localhost:5000/api/values
GET http://localhost:5000/api/values/5baa8239-70b4-42d6-a360-1cc1c73ce9ac
GET http://localhost:5000/api/values/5baa8239-70b4-42d6-a360-1cc1c73ce9ac
DELETE http://localhost:5000/api/values/5baa8239-70b4-42d6-a360-1cc1c73ce9ac
```

See tag [Step_02_2](https://github.com/iblazhko/containers-demo/releases/tag/Step_02_2 "Step_02_2")
in this repository for reference implementation.

### Step 2.3 Logging

To ensure that system activity is logged consistently, add logging implementation
to `Infrastructure.Logging`; modify `WebApi` and `Client` projects to use that
implementation and log activity to console.

This repository uses [Microsoft.Diagnostics.EventFlow](https://github.com/Azure/diagnostics-eventflow/ "Microsoft.Diagnostics.EventFlow")
as underlying implementation.

```cmd
dotnet add .\Infrastructure.Logging\Infrastructure.Logging.csproj package Microsoft.Diagnostics.EventFlow

dotnet add .\Client\Client.csproj package Microsoft.Diagnostics.EventFlow
dotnet add .\Client\Client.csproj package Microsoft.Diagnostics.EventFlow.Outputs.StdOutput
dotnet add .\Client\Client.csproj package Microsoft.Diagnostics.EventFlow.Outputs.ElasticSearch

dotnet add .\WebApi\WebApi.csproj package Microsoft.Diagnostics.EventFlow
dotnet add .\WebApi\WebApi.csproj package Microsoft.Diagnostics.EventFlow.Outputs.StdOutput
dotnet add .\WebApi\WebApi.csproj package Microsoft.Diagnostics.EventFlow.Outputs.ElasticSearch
```

In later steps we will add [ElasticSearch output](https://github.com/Azure/diagnostics-eventflow/#elasticsearch "ElasticSearch output")
to send logs to centralized storage.

See tag [Step_02_3](https://github.com/iblazhko/containers-demo/releases/tag/Step_02_3 "Step_02_3")
in this repository for reference implementation.

## Step 3. Docker Containers

### Step 3.1 WebAPI

In this step we'll add Docker container for the API.

Add `WebApi\Dockerfile` file to define content of the API container.

```Dockerfile
FROM microsoft/dotnet:2.0-runtime
WORKDIR /app
EXPOSE 5000
COPY _publish .
ENTRYPOINT ["dotnet", "WebApi.dll"]
```

There are many ways to compose a Docker container, see
[Dockerfile reference](https://docs.docker.com/engine/reference/builder/ "Dockerfile reference")
for more information.

In this project we'll be using `dotnet publish` output, i.e. compiled binaries,
to compose API container. `Dockerfile` in this example expects the application
to be published in the `_publish` directory.

In a command prompt, change directory to `<project directory>\src\WebApi`
and run commands

```cmd
dotnet build
dotnet publish --output _publish

docker build --tag containers-demo/webapi:develop .
```

You should see output from Docker similar to this:

```txt
Sending build context to Docker daemon    105MB
Step 1/5 : FROM microsoft/dotnet:2.0-runtime
---> 059aeb771f22
Step 2/5 : WORKDIR /app
---> Using cache
---> cd26e80266c7
Step 3/5 : EXPOSE 5000
---> Using cache
---> 46964d341932
Step 4/5 : COPY _publish .
---> Using cache
---> 3191af7d0c59
Step 5/5 : ENTRYPOINT ["dotnet", "WebApi.dll"]
---> Using cache
---> b6fbf6ec67c3
Successfully built b6fbf6ec67c3
Successfully tagged containers-demo/webapi:develop
SECURITY WARNING: You are building a Docker image from Windows against a non-Windows Docker host. All files and directories added to build context will have '-rwxr-xr-x' permissions. It is recommended to double check and reset permissions for sensitive files and directories.
```

(your ids will be different).
We will address the secutiry warning later.

```cmd
docker create --name containersdemo_webapi containers-demo/webapi:develop
docker start --interactive containersdemo_webapi
```

API should now be running in Docker. Press `Ctrl+C` when you need to stop it.
You can also run it in non-interactive mode and inspect logs on demand:

```cmd
docker start containersdemo_webapi
docker logs containersdemo_webapi
...
docker stop containersdemo_webapi
```

See tag [Step_03_1](https://github.com/iblazhko/containers-demo/releases/tag/Step_03_1 "Step_03_1")
in this repository for reference implementation.

### Step 3.2 Client

In this step we'll add Docker container for the client.

Add `Client\Dockerfile` file to define content of the API container.

```Dockerfile
FROM microsoft/dotnet:2.0-runtime
WORKDIR /app
COPY _publish .
ENTRYPOINT ["dotnet", "Client.dll"]
```

In a command prompt, change directory to `<project directory>\src\Client`
and run commands

```cmd
dotnet build
dotnet publish --output _publish

docker build --tag containersdemo/client:develop .
docker create --name containersdemo_client containers-demo/client:develop
docker start containersdemo_client
docker logs containersdemo_client
...
docker stop containersdemo_client
```

See tag [Step_03_2](https://github.com/iblazhko/containers-demo/releases/tag/Step_03_2 "Step_03_2")
in this repository for reference implementation.

### Step 3.3 MongoDB

(TODO: MongoDB Docker container is optional and not really
required by Kubernetes deployment the way it is now.)

In this step, we will add another Docker container to our system, to run
MongoDB server, and will modify API implementation to store values
in the database.

Note that we are adding the MongoDB Docker container only for demonstration
purposes, to have a self-contained system. In a production environment
we will have a dedicated MongoDB cluster.

```cmd
docker run --name containersdemo-mongo -d mongo:latest
```

Note that MongoDB .NET driver is compatible with `netstandard1.5` or higher.
Check `TargetFramework` in project files to make sure that they
meet this requirement:

```xml
<TargetFramework>netcoreapp2.0</TargetFramework>
```

Modify API implementation to use database instead of in-memory dictionary.

```cmd
cd <project directory>\src

dotnet add .\WebApi\WebApi.csproj package Microsoft.Extensions.Configuration
dotnet add .\WebApi\WebApi.csproj package Microsoft.Extensions.Configuration.CommandLine
dotnet add .\WebApi\WebApi.csproj package Microsoft.Extensions.Configuration.Json
dotnet add .\WebApi\WebApi.csproj package MongoDB.Driver
```

In `<project directory>\src\WebApi\appsettings.json` add settings for MongoDB:

```json
{
    "MongoDB.ServerAddress": "A.B.C.D",
    "MongoDB.ServerPort": "27017",
    "MongoDB.DatabaseName": "containersdemo",
    "MongoDB.UserName": "",
    "MongoDB.UserPassword": ""
}
```

Where `A.B.C.D` is the IP address of a host where MongoDB Server is running.
Note that we need to use a host name or IP that will be accessible from within
a container, so avoid using `localhost`.

Modify `<project directory>\src\WebApi\Startup.cs`
to read MongoDB settings, and
`<project directory>\src\WebApi\Controllers\ValuesController.cs`
to use MongoDB database.

See tag [Step_03_3](https://github.com/iblazhko/containers-demo/releases/tag/Step_03_3 "Step_03_3")
in this repository for reference implementation.

### Step 3.4 ElasticSearch+Kibana

(TODO: ElasticSearch and Kibana Docker containers are optional and not really
required by Kubernetes deployment the way it is now.)

In this step, we will add two more Docker container to our system,
to run ElasticSearch and Kibana servers, and will modify logging implementation
to store logs from both API and Client in centralized ElasticSearch
database.

Note that this is done only for demonstration purposes, to have a self-contained
system. In a production environment we will have a dedicated ElasticSearch
cluster.

```cmd
docker pull docker.elastic.co/elasticsearch/elasticsearch-oss:6.2.4
docker run --name containersdemo-elasticsearch -p 9200:9200 -p 9300:9300 -e "discovery.type=single-node" -d docker.elastic.co/elasticsearch/elasticsearch-oss:6.2.4

docker pull docker.elastic.co/kibana/kibana-oss:6.2.4
docker run --name containersdemo-kibana -p 5601:5601 -e "discovery.type=single-node" -d docker.elastic.co/kibana/kibana-oss:6.2.4
```

(note that the ElasticSearch repository does not have `latest` tag)

Add configuration settings to WebApi and Client `appsettings.json`
files:

```json
"ElasticSearch.Url": "http://A.B.C.D:9200",
"ElasticSearch.IndexFormat": "containersdemo",
"LoggingLevel": "Information"
```

Where `A.B.C.D` is the IP address of a host where ElasticSearch Server is running.
Note that we need to use a host name or IP that will be accessible from within
a container, so avoid using `localhost`.

Add ElasticSearch output package reference:

```cmd
cd <project directory>\src
dotnet add .\WebApi\WebApi.csproj package Microsoft.Diagnostics.EventFlow.Outputs.ElasticSearch
dotnet add .\Client\Client.csproj package Microsoft.Diagnostics.EventFlow.Outputs.ElasticSearch
```

See tag [Step_03_4](https://github.com/iblazhko/containers-demo/releases/tag/Step_03_4 "Step_03_4")
in this repository for reference implementation.

### Step 4 Kubernetes

#### Topology Overview

Let's create following topology in K8s:

```txt
[WebAPI - Deployment, 3 Instances] - [WebAPI - Service] - [Client - Deployment, 5 Instances]
[MongoDB - Service]
[ElasticSearch - Service]
```

(TODO: make a better diagram)

- We will have Deployment running 3 instances of our `WebApi`
- On top of the Deployment, we will have a Service that will provide
  a stable IP address and simple load balancing
- We will have Deployment running 5 instances of `Client` sending requests
  to `Service` (effectively, to any of our three `WebApi` instances)
- We wile have MongoDB and ElasticSearch services that will point to
  external instances

#### Infrastructure - MongoDB

To make MongoDB accessible for pods inside Kubernetes, we will create a service
that will redirect requests to an external instance.

Create manifest for the MongoDB Service in `k8s\mongodb-service.yml`:

```yaml
kind: Service
apiVersion: v1
metadata:
  name: containersdemo-mongodb
spec:
  type: ExternalName
  externalName: <external hostname or IP>
```

For the pods inside the Kubernetes MongoDB will be accessible by hostname
`containersdemo-mongodb`.

Create the Service:

```cmd
kubectl create -f .\mongodb-service.yml
```

Verify that Service was created:

```cmd
kubectl get svc/containersdemo-mongodb
```

Expected output is

```txt
NAME                     TYPE           CLUSTER-IP   EXTERNAL-IP     PORT(S)   AGE
containersdemo-mongodb   ExternalName   <none>       ?.?.?.?         <none>    ???
```

`EXTERNAL-IP` should match value from the manifest.

#### Infrastructure - ElasticSearch

To make ElasticSearch accessible for pods inside Kubernetes, we will create a service
that will redirect requests to an external instance.

Create manifest for the ElasticSearch Service in `k8s\elasticsearch-service.yml`:

```yaml
kind: Service
apiVersion: v1
metadata:
  name: containersdemo-elasticsearch
spec:
  type: ExternalName
  externalName: <external hostname or IP>
```

For the pods inside the Kubernetes ElasticSearch will be accessible by hostname
`containersdemo-elasticsearch`.

Create the Service:

```cmd
kubectl create -f .\elasticsearch-service.yml
```

Verify that Service was created:

```cmd
kubectl get svc/containersdemo-elasticsearch
```

Expected output is

```txt
NAME                           TYPE           CLUSTER-IP   EXTERNAL-IP     PORT(S)   AGE
containersdemo-elasticsearch   ExternalName   <none>       ?.?.?.?         <none>    ??
```

`EXTERNAL-IP` should match value from the manifest.

#### WebAPI - Deployment

Create manifest for the WebAPI Deployment in `k8s\webapi-deployment.yml`:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: containersdemo-webapi-deployment
  labels:
    app: containersdemo-webapi
spec:
  replicas: 3
  selector:
    matchLabels:
      app: containersdemo-webapi
  template:
    metadata:
      labels:
        app: containersdemo-webapi
    spec:
      containers:
      - name: containersdemo-webapi-ctr
        image: containers-demo/webapi:develop
        ports:
        - containerPort: 5000
          protocol: TCP
          name: webapi-port-po
```

Create the Deployment:

```cmd
kubectl create -f .\webapi-deployment.yml
```

Verify that Deployment was created:

```cmd
kubectl get deploy/containersdemo-webapi-deployment
```

Expected output is

```txt
NAME                               DESIRED   CURRENT   UP-TO-DATE   AVAILABLE   AGE
containersdemo-webapi-deployment   3         3         3            3           ???
```

Desired and actual instance count should match one from the manifest (3).

#### WebAPI - Service

Create manifest for the WebAPI Service in `k8s\webapi-service.yml`:

```yaml
apiVersion: v1
kind: Service
metadata:
  name: containersdemo-webapi-service
  labels:
    app: containersdemo-webapi
spec:
  type: LoadBalancer
  ports:
  - port: 5000
    targetPort: 5000
    protocol: TCP
    name: webapi-port-lb
  selector:
    app: containersdemo-webapi
```

Create the Service:

```cmd
kubectl create -f .\webapi-service.yml
```

Verify that Service was created:

```cmd
kubectl get svc/containersdemo-webapi-service
```

Expected output is

```txt
NAME                            TYPE         CLUSTER-IP      EXTERNAL-IP   PORT(S)          AGE
containersdemo-webapi-service   LoadBalancer ?.?.?.?         <none>        5000:31319/TCP   ???
```

Desired and actual instance count should match one from the manifest (3).

#### Client - Deployment

Create manifest for the Client Deployment in `k8s\client-deployment.yml`:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: containersdemo-client-deployment
  labels:
    app: containersdemo-client
spec:
  replicas: 5
  selector:
    matchLabels:
      app: containersdemo-client
  template:
    metadata:
      labels:
        app: containersdemo-client
    spec:
      containers:
      - name: containersdemo-client-ctr
        image: containers-demo/client:develop
```

Create the Deployment:

```cmd
kubectl create -f .\client-deployment.yml
```

Verify that Deployment was created:

```cmd
kubectl get deploy/containersdemo-client-deployment
```

Expected output is

```txt
NAME                               DESIRED   CURRENT   UP-TO-DATE   AVAILABLE   AGE
containersdemo-client-deployment   5         5         5            5           ???
```

Desired and available instance count should match one from the manifest (5).

#### Validation

Examine system logs using `kubectl logs` command.
To get exact name of a pod, run

```cmd
kubectl get pods
```

look for pods with name staring with `containersdemo-client-deployment` and
`containersdemo-webapi-deployment`. Then run commands

```cmd
kubectl logs containersdemo-webapi-deployment-<your-id>
kubectl logs containersdemo-client-deployment-<your-id>
```

Log output should be similar to examples above for the console apps.

#### Infrastructure

TODO: Consider deploying ElasticSearch+Kibana and MongoDB to Kubernetes
to make this example fully self-contained.
