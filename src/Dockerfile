#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

# ---------------------------- Base Image -------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# ---------------------------- Build Image -------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Calendars/Planar.Calendar/Planar.Calendar.csproj", "Calendars/Planar.Calendar/"]
COPY ["Calendars/Planar.Calendar.Hebrew/Planar.Calendar.Hebrew.csproj", "Calendars/Planar.Calendar.Hebrew/"]
COPY ["Jobs/CommonJob/CommonJob.csproj", "Jobs/CommonJob/"]
COPY ["Jobs/PlanarJob/PlanarJob.csproj", "Jobs/PlanarJob/"]
COPY ["Jobs/PowerShellJob/PowerShellJob.csproj", "Jobs/PowerShellJob/"]
COPY ["Planar/Planar.csproj", "Planar/"]
COPY ["Planar.API.Common/Planar.API.Common.csproj", "Planar.API.Common/"]
COPY ["Planar.CLI/Planar.CLI.csproj", "Planar.CLI/"]
COPY ["Planar.Common/Planar.Common.csproj", "Planar.Common/"]
COPY ["Planar.Service/Planar.Service.csproj", "Planar.Service/"]

RUN dotnet restore "Planar/Planar.csproj"
RUN dotnet restore "Planar.CLI/Planar.CLI.csproj"
COPY . .
WORKDIR "/src/Planar"
RUN dotnet build "Planar.csproj" -c Release -o /app/build
WORKDIR "/src/Planar.CLI"
RUN dotnet build "Planar.CLI.csproj" -c Release -o /app/build
WORKDIR "/src/Planar"

FROM build AS publish
RUN dotnet publish "../Planar.CLI/Planar.CLI.csproj" -c Release -o /app/publish /p:UseAppHost=false
RUN dotnet publish "Planar.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ---------------------------- Final Image -------------------------------------------
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# ---------------------------- Entry Point -------------------------------------------
ENV PATH="/app:${PATH}"
ENTRYPOINT ["dotnet", "Planar.dll"]