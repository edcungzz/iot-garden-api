# Reset Database Script
# This will drop all old tables and create new schema

Write-Host "🗑️  Removing old migrations..." -ForegroundColor Yellow
Remove-Item -Recurse -Force ./Migrations -ErrorAction SilentlyContinue

Write-Host "📋 Creating new migration..." -ForegroundColor Cyan
dotnet ef migrations add InitialNewSchema

Write-Host "💥 Dropping old database..." -ForegroundColor Red
dotnet ef database drop --force

Write-Host "🔨 Creating new database..." -ForegroundColor Green
dotnet ef database update

Write-Host "✅ Database reset complete!" -ForegroundColor Green
Write-Host "Run 'dotnet run' to start the API" -ForegroundColor White
