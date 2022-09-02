#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Calendars/Planar.Calendar/Planar.Calendar.csproj", "Calendars/Planar.Calendar/"]
COPY ["Planar/Planar.csproj", "Planar/"]
COPY ["Planar.API.Common/Planar.API.Common.csproj", "Planar.API.Common/"]
COPY ["Planar.Service/Planar.Service.csproj", "Planar.Service/"]
COPY ["Jobs/RunPlanarJob/RunPlanarJob.csproj", "Jobs/RunPlanarJob/"]
COPY ["Jobs/CommonJob/CommonJob.csproj", "Jobs/CommonJob/"]
COPY ["Planar.Common/Planar.Common.csproj", "Planar.Common/"]
COPY ["Calendars/Planar.Calendar.Hebrew/Planar.Calendar.Hebrew.csproj", "Calendars/Planar.Calendar.Hebrew/"]
RUN dotnet restore "Planar/Planar.csproj"
COPY . .
WORKDIR "/src/Planar"
RUN dotnet build "Planar.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Planar.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Planar.dll"]