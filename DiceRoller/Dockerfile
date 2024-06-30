FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
ARG TARGETARCH
WORKDIR /source

# copy csproj and restore as distinct layers
COPY ["./DiceRoller.csproj", "."]
RUN dotnet restore -a $TARGETARCH

# copy everything else and build app
COPY [".", "."]
RUN dotnet publish -a $TARGETARCH --no-restore -o /app /p:UseAppHost=false


# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled
USER $APP_UID
EXPOSE 8080
WORKDIR /app
COPY --from=build ["/app", "."]
ENTRYPOINT ["dotnet", "DiceRoller.dll"]
