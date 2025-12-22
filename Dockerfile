# Kütüphane API - Docker Image
# Railway.app için optimize edilmiş

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Tüm projeyi kopyala
WORKDIR /src
COPY . .

# API'yi derle - doğrudan /app'e
WORKDIR /src/api
RUN dotnet restore
RUN dotnet publish -c Release -o /app

# Website ve Mobile'ı /app altına kopyala
RUN cp -r /src/website /app/website
RUN mkdir -p /app/mobile && \
    cp /src/mobile/index.html /app/mobile/ && \
    cp /src/mobile/manifest.json /app/mobile/ && \
    cp /src/mobile/sw.js /app/mobile/ && \
    cp -r /src/mobile/css /app/mobile/ && \
    cp -r /src/mobile/js /app/mobile/

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Türkçe karakter desteği
ENV LANG=tr_TR.UTF-8
ENV LC_ALL=tr_TR.UTF-8

# Tüm dosyaları kopyala - doğrudan /app'ten
COPY --from=build /app .

# Uygulamayı başlat
ENTRYPOINT ["dotnet", "KutuphaneApi.dll"]
