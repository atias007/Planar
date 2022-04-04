#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Planner/Planner.csproj", "Planner/"]
RUN dotnet restore "Planner/Planner.csproj"
COPY . .
WORKDIR "/src/Planner"
RUN dotnet build "Planner.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Planner.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Planner.dll"]