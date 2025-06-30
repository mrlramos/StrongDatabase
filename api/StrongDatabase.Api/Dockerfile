# Utiliza imagem oficial do .NET para build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["StrongDatabase.Api/*.csproj", "StrongDatabase.Api/"]
RUN dotnet restore "StrongDatabase.Api/StrongDatabase.Api.csproj"
COPY . .
WORKDIR "/src/StrongDatabase.Api"
RUN dotnet publish "StrongDatabase.Api.csproj" -c Release -o /app/publish

# Imagem runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "StrongDatabase.Api.dll"] 