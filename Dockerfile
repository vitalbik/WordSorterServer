FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY WordSorterServer.cs .

RUN echo '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>' > app.csproj

RUN dotnet build -o out

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/out .

ENV PORT=10000
EXPOSE 10000

CMD ["dotnet", "app.dll"]
