# gRPC Simple Demo

####  Nuno Relvao

## Description
- Simple as per dotnet 8.0 template run of a gRPC Server/Client
- The simple idea of gRPC is to be able to remote procedures calls (methods) on another service, kind of as SOAP but in a modern aproach with interop **(proto)** and compressed binary data optipmized instead of XML and schemas of SOAP
- ideally used on microservices or services that need to be called for example by other languages not the same as the server is written on.


# To Run Server (need to be first)
 - Go to where the Solution root folder
 - Run command:  ``` ddotnet run --project gRPCServer/gRPCServer.csproj ```.
 - Please note it is running on **https** so **ssl** is needed (intructiosn as of this running on Fedora Workstation can be seen on this [link](https://fedoramagazine.org/set-up-a-net-development-environment/))

 To Run Client (only after Server running and exposing port 7043)
 - Go to where the Solution root folder
 - Run command:  ``` dotnet run --project gRPCClient/gRPCClient.csproj ```.

## PREVIEW OF RUNNING SERVER

![Server](./resources/Server.png)

## PREVIEW OF RUNNING CLIENT

![Server](./resources/Client.png)