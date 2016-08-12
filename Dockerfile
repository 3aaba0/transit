FROM microsoft/dotnet:1.0.0-preview2-sdk

COPY /TransitApiExample /TransitApiExample

WORKDIR /TransitApiExample

RUN dotnet restore

EXPOSE 5000

ENTRYPOINT ["dotnet", "run"]