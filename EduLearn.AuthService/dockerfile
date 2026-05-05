FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["EduLearn.AuthService/EduLearn.AuthService.csproj", "EduLearn.AuthService/"]
RUN dotnet restore "EduLearn.AuthService/EduLearn.AuthService.csproj"
COPY . .
WORKDIR "/src/EduLearn.AuthService"
RUN dotnet build "EduLearn.AuthService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EduLearn.AuthService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EduLearn.AuthService.dll"]