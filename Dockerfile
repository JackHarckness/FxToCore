FROM mcr.microsoft.com/dotnet/sdk:9.0@sha256:3fcf6f1e809c0553f9feb222369f58749af314af6f063f389cbd2f913b4ad556 AS build-dotnet
WORKDIR /fx2c
COPY . ./
RUN dotnet restore src/FxToCore.CoreApp/FxToCore.CoreApp.csproj
RUN dotnet publish -c:Release src/FxToCore.CoreApp/FxToCore.CoreApp.csproj

FROM gcc:14 AS build-gcc
WORKDIR /fx2c
COPY . ./
RUN mkdir -p out/Release
RUN gcc -Wall -shared -fPIC -o out/Release/libcpp.so src/FxToCore.CoreApp.CppLib/ReverseString.c

FROM mcr.microsoft.com/dotnet/aspnet:9.0@sha256:b4bea3a52a0a77317fa93c5bbdb076623f81e3e2f201078d89914da71318b5d8
WORKDIR /fx2c
COPY --from=build-dotnet /fx2c/out/Release .
COPY --from=build-gcc /fx2c/out/Release/libcpp.so /usr/lib/libcpp.so
ENTRYPOINT ["./f2c-http"]