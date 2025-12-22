# Kütüphane API - Docker Image
# Railway.app için optimize edilmiş

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Tüm projeyi kopyala
WORKDIR /src
COPY . .

# API'yi derle - Railway'in beklediği /app/out dizinine
WORKDIR /src/api
RUN dotnet restore
RUN dotnet publish -c Release -o /app/out

# Website ve Mobile'ı /app/out altına kopyala
RUN cp -r /src/website /app/out/website
RUN mkdir -p /app/out/mobile && \
    cp /src/mobile/index.html /app/out/mobile/ && \
    cp /src/mobile/manifest.json /app/out/mobile/ && \
    cp /src/mobile/sw.js /app/out/mobile/ && \
    cp -r /src/mobile/css /app/out/mobile/ && \
    cp -r /src/mobile/js /app/out/mobile/

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Türkçe karakter desteği
ENV LANG=tr_TR.UTF-8
ENV LC_ALL=tr_TR.UTF-8

# Çalışma dizini /app/out olarak ayarla (Railway'in beklediği)
WORKDIR /app/out
COPY --from=build /app/out .

# Uygulamayı başlat
ENTRYPOINT ["dotnet", "KutuphaneApi.dll"]
