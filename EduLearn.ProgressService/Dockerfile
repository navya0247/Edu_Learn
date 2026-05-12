FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

RUN apt-get update && apt-get install -y \
    libfontconfig1 \
    libfreetype6 \
    libpng16-16 \
    libgdiplus \
    && rm -rf /var/lib/apt/lists/*

COPY ["EduLearn.ProgressService/EduLearn.ProgressService.csproj", "EduLearn.ProgressService/"]
RUN dotnet restore "EduLearn.ProgressService/EduLearn.ProgressService.csproj"
COPY . .
WORKDIR "/src/EduLearn.ProgressService"
RUN dotnet build "EduLearn.ProgressService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EduLearn.ProgressService.csproj" -c Release -o /app/publish

FROM base AS final

RUN apt-get update && apt-get install -y \
    libfontconfig1 \
    libfreetype6 \
    libpng16-16 \
    libgdiplus \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EduLearn.ProgressService.dll"]