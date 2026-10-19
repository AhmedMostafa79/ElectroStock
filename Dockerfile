FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
#http
EXPOSE 8080
#https
EXPOSE 8081

# copying .csproj files(dependencies) for docker-optimization, to run only at image build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src 
COPY ["src/ElectroStock.Api/ElectroStock.Api.csproj","ElectroStock.Api/"]
COPY ["src/ElectroStock.Application/ElectroStock.Application.csproj","ElectroStock.Application/"]
COPY ["src/ElectroStock.Domain/ElectroStock.Domain.csproj","ElectroStock.Domain/"]
COPY ["src/ElectroStock.Infrastructure/ElectroStock.Infrastructure.csproj","ElectroStock.Infrastructure/"]

# read these files if needed to restore missing dependencies from class libraries csproj
RUN dotnet restore "ElectroStock.Api/ElectroStock.Api.csproj" 

# Copy each project into the same flattened layout used by the project references.
# The build context is the repository root, while the source projects live under src/.
COPY src/ElectroStock.Api/. ElectroStock.Api/
COPY src/ElectroStock.Application/. ElectroStock.Application/
COPY src/ElectroStock.Domain/. ElectroStock.Domain/
COPY src/ElectroStock.Infrastructure/. ElectroStock.Infrastructure/

# move to main file's folder (.Api)
WORKDIR "/src/ElectroStock.Api"
#build the main file ElectroStock.Api.csproj with build=Release(faster execution), 
# and store build output in /app/build
RUN dotnet build "ElectroStock.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# use build's output .dll and use it as publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release 
# produces a folder of all dependent DLLs (Application, Domain, Infrastructure),
# and stores them in /app/publish
RUN dotnet publish "ElectroStock.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish 


# creates image from base 
FROM base AS final
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080 
WORKDIR /app
# pulls files out of the previous stage's filesystem into /app.
# Only the publish output is copied — no SDK, no source code, no NuGet cache → small image.
COPY --from=publish /app/publish .

# sets the default command to run when the container starts
ENTRYPOINT ["dotnet","ElectroStock.Api.dll"]

# We made it multi-stage so that we use the .NET SDK to produce the build output,
# then the publish stage turns that into a deployment-ready folder,
# and finally the final stage uses base (the lightweight dotnet:8.0 runtime, not the SDK),
# copying in the published .dll files from the publish stage and running them. Result: a lightweight image.

#Notes: 
        #dotnet build → compiles your code into .dll files (but includes stuff you don't need to run, and references SDK paths).
        #dotnet publish → takes the build output and produces a self-contained deployment folder: 
        # your app DLL + all dependency DLLs + appsettings.json + runtimeconfig.json + everything needed to just run it.