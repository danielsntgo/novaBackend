# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/FlexPos.Domain/FlexPos.Domain.csproj", "src/FlexPos.Domain/"]
COPY ["src/FlexPos.Application/FlexPos.Application.csproj", "src/FlexPos.Application/"]
COPY ["src/FlexPos.Infrastructure/FlexPos.Infrastructure.csproj", "src/FlexPos.Infrastructure/"]
COPY ["src/FlexPos.Api/FlexPos.Api.csproj", "src/FlexPos.Api/"]

RUN dotnet restore "src/FlexPos.Api/FlexPos.Api.csproj"

COPY . .
RUN dotnet publish "src/FlexPos.Api/FlexPos.Api.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish .

CMD ["sh", "-c", "dotnet FlexPos.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
