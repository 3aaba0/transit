FROM microsoft/dotnet:1.0.0-preview2-sdk

COPY src/TansitApiExample /TansitApiExample

WORKDIR /TansitApiExample

RUN dotnet restore

EXPOSE 5000

ENTRYPOINT ["dotnet", "run"]