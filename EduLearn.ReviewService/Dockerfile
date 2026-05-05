FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["EduLearn.ReviewService/EduLearn.ReviewService.csproj", "EduLearn.ReviewService/"]
RUN dotnet restore "EduLearn.ReviewService/EduLearn.ReviewService.csproj"
COPY . .
WORKDIR "/src/EduLearn.ReviewService"
RUN dotnet build "EduLearn.ReviewService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EduLearn.ReviewService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EduLearn.ReviewService.dll"]