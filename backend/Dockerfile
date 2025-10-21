# ビルドステージ
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# プロジェクトファイルをコピーして依存関係を復元
COPY ["src/Shared/Shared.csproj", "src/Shared/"]
COPY ["src/Enrollments/Domain/Enrollments.Domain.csproj", "src/Enrollments/Domain/"]
COPY ["src/Enrollments/Application/Enrollments.Application.csproj", "src/Enrollments/Application/"]
COPY ["src/Enrollments/Infrastructure/Enrollments.Infrastructure.csproj", "src/Enrollments/Infrastructure/"]
COPY ["src/Enrollments/Api/Enrollments.Api.csproj", "src/Enrollments/Api/"]

RUN dotnet restore "src/Enrollments/Api/Enrollments.Api.csproj"

# ソースコードをコピーしてビルド
COPY . .
WORKDIR "/src/src/Enrollments/Api"
RUN dotnet build "Enrollments.Api.csproj" -c Release -o /app/build

# パブリッシュステージ
FROM build AS publish
RUN dotnet publish "Enrollments.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 実行ステージ
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app
EXPOSE 8080

# 非rootユーザーで実行
RUN addgroup -g 1000 appuser && \
    adduser -D -u 1000 -G appuser appuser
USER appuser

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Enrollments.Api.dll"]
