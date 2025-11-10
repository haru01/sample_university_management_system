# ビルドステージ
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# プロジェクトファイルをコピーして依存関係を復元
COPY ["src/Shared/Shared.csproj", "src/Shared/"]
COPY ["src/StudentRegistrations/Domain/StudentRegistrations.Domain.csproj", "src/StudentRegistrations/Domain/"]
COPY ["src/StudentRegistrations/Application/StudentRegistrations.Application.csproj", "src/StudentRegistrations/Application/"]
COPY ["src/StudentRegistrations/Infrastructure/StudentRegistrations.Infrastructure.csproj", "src/StudentRegistrations/Infrastructure/"]
COPY ["src/StudentRegistrations/Api/StudentRegistrations.Api.csproj", "src/StudentRegistrations/Api/"]

RUN dotnet restore "src/StudentRegistrations/Api/StudentRegistrations.Api.csproj"

# ソースコードをコピーしてビルド
COPY . .
WORKDIR "/src/src/StudentRegistrations/Api"
RUN dotnet build "StudentRegistrations.Api.csproj" -c Release -o /app/build

# パブリッシュステージ
FROM build AS publish
RUN dotnet publish "StudentRegistrations.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 実行ステージ
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app
EXPOSE 8080

# 非rootユーザーで実行
RUN addgroup -g 1000 appuser && \
    adduser -D -u 1000 -G appuser appuser
USER appuser

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StudentRegistrations.Api.dll"]
