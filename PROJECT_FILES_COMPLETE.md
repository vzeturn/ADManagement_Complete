# ADManagement - COMPLETE PROJECT FILES REFERENCE

## 📁 Cấu trúc thư mục hoàn chỉnh

```
ADManagement/
├── ADManagement.sln
├── .gitignore
├── README.md
├── SETUP_GUIDE.md
│
├── ADManagement.Domain/
│   ├── ADManagement.Domain.csproj
│   ├── Entities/
│   │   ├── ADUser.cs ✓
│   │   ├── ADGroup.cs ✓
│   │   └── OrganizationalUnit.cs ✓
│   ├── Interfaces/
│   │   ├── IADRepository.cs ✓
│   │   └── IExcelExporter.cs ✓
│   ├── Common/
│   │   └── Result.cs ✓
│   └── Exceptions/
│       └── ADManagementException.cs ✓
│
├── ADManagement.Application/
│   ├── ADManagement.Application.csproj
│   ├── DTOs/
│   │   ├── ADUserDto.cs ✓
│   │   ├── ADGroupDto.cs ✓
│   │   ├── OrganizationalUnitDto.cs ✓
│   │   └── PasswordChangeRequest.cs ✓
│   ├── Services/
│   │   ├── ADUserService.cs ✓
│   │   └── ExportService.cs ✓
│   ├── Interfaces/
│   │   ├── IADUserService.cs ✓
│   │   └── IExportService.cs ✓
│   ├── Configuration/
│   │   ├── ADConfiguration.cs ✓
│   │   └── ExportConfiguration.cs ✓
│   ├── Validators/
│   │   └── PasswordChangeRequestValidator.cs ✓
│   ├── Mappings/
│   │   ├── ADUserMapper.cs ✓
│   │   └── ADGroupMapper.cs ✓
│   └── DependencyInjection.cs ✓
│
├── ADManagement.Infrastructure/
│   ├── ADManagement.Infrastructure.csproj
│   ├── Repositories/
│   │   └── ADRepository.cs ✓
│   ├── Exporters/
│   │   └── ExcelExporter.cs ✓
│   └── DependencyInjection.cs ✓
│
└── ADManagement.Console/
    ├── ADManagement.Console.csproj
    ├── Program.cs ✓
    └── appsettings.json ✓
```

## 📄 Nội dung các file .csproj và .sln

### 1. ADManagement.sln

```xml
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "ADManagement.Domain", "ADManagement.Domain\ADManagement.Domain.csproj", "{A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "ADManagement.Application", "ADManagement.Application\ADManagement.Application.csproj", "{B2C3D4E5-F6A7-4B5C-8D9E-0F1A2B3C4D5E}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "ADManagement.Infrastructure", "ADManagement.Infrastructure\ADManagement.Infrastructure.csproj", "{C3D4E5F6-A7B8-4C5D-8E9F-0A1B2C3D4E5F}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "ADManagement.Console", "ADManagement.Console\ADManagement.Console.csproj", "{D4E5F6A7-B8C9-4D5E-8F9A-0B1C2D3E4F5A}"
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D}.Release|Any CPU.Build.0 = Release|Any CPU
		{B2C3D4E5-F6A7-4B5C-8D9E-0F1A2B3C4D5E}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{B2C3D4E5-F6A7-4B5C-8D9E-0F1A2B3C4D5E}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{B2C3D4E5-F6A7-4B5C-8D9E-0F1A2B3C4D5E}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{B2C3D4E5-F6A7-4B5C-8D9E-0F1A2B3C4D5E}.Release|Any CPU.Build.0 = Release|Any CPU
		{C3D4E5F6-A7B8-4C5D-8E9F-0A1B2C3D4E5F}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{C3D4E5F6-A7B8-4C5D-8E9F-0A1B2C3D4E5F}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{C3D4E5F6-A7B8-4C5D-8E9F-0A1B2C3D4E5F}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{C3D4E5F6-A7B8-4C5D-8E9F-0A1B2C3D4E5F}.Release|Any CPU.Build.0 = Release|Any CPU
		{D4E5F6A7-B8C9-4D5E-8F9A-0B1C2D3E4F5A}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{D4E5F6A7-B8C9-4D5E-8F9A-0B1C2D3E4F5A}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{D4E5F6A7-B8C9-4D5E-8F9A-0B1C2D3E4F5A}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{D4E5F6A7-B8C9-4D5E-8F9A-0B1C2D3E4F5A}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
EndGlobal
```

### 2. ADManagement.Domain/ADManagement.Domain.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>

</Project>
```

### 3. ADManagement.Application/ADManagement.Application.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FluentValidation" Version="11.11.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\ADManagement.Domain\ADManagement.Domain.csproj" />
  </ItemGroup>

</Project>
```

### 4. ADManagement.Infrastructure/ADManagement.Infrastructure.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="EPPlus" Version="7.5.2" />
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="9.0.0" />
    <PackageReference Include="System.DirectoryServices" Version="9.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\ADManagement.Application\ADManagement.Application.csproj" />
    <ProjectReference Include="..\ADManagement.Domain\ADManagement.Domain.csproj" />
  </ItemGroup>

</Project>
```

### 5. ADManagement.Console/ADManagement.Console.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    <UserSecretsId>admanagement-console-secrets</UserSecretsId>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.0" />
    <PackageReference Include="Serilog.Extensions.Hosting" Version="8.0.0" />
    <PackageReference Include="Serilog.Sinks.Console" Version="6.0.0" />
    <PackageReference Include="Serilog.Sinks.File" Version="6.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\ADManagement.Application\ADManagement.Application.csproj" />
    <ProjectReference Include="..\ADManagement.Infrastructure\ADManagement.Infrastructure.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Update="appsettings.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>

</Project>
```

## ✅ Checklist các file đã tạo

### Domain Layer (✓ Hoàn tất)
- [x] ADUser.cs
- [x] ADGroup.cs
- [x] OrganizationalUnit.cs
- [x] IADRepository.cs
- [x] IExcelExporter.cs
- [x] Result.cs
- [x] ADManagementException.cs

### Application Layer (✓ Hoàn tất)
- [x] ADUserDto.cs
- [x] ADGroupDto.cs
- [x] OrganizationalUnitDto.cs
- [x] PasswordChangeRequest.cs
- [x] ADUserService.cs
- [x] ExportService.cs
- [x] IADUserService.cs
- [x] IExportService.cs
- [x] ADConfiguration.cs
- [x] ExportConfiguration.cs
- [x] PasswordChangeRequestValidator.cs
- [x] ADUserMapper.cs
- [x] ADGroupMapper.cs
- [x] DependencyInjection.cs

### Infrastructure Layer (✓ Hoàn tất)
- [x] ADRepository.cs
- [x] ExcelExporter.cs
- [x] DependencyInjection.cs

### Console Application (✓ Hoàn tất)
- [x] Program.cs
- [x] appsettings.json

### Documentation (✓ Hoàn tất)
- [x] README.md
- [x] SETUP_GUIDE.md
- [x] .gitignore

## 🔨 Hướng dẫn tạo project từ file này

### Cách 1: Tạo thủ công (Windows)

1. Tạo thư mục dự án:
```powershell
mkdir ADManagement
cd ADManagement
```

2. Tạo solution:
```powershell
dotnet new sln -n ADManagement
```

3. Tạo các projects:
```powershell
dotnet new classlib -n ADManagement.Domain -f net9.0
dotnet new classlib -n ADManagement.Application -f net9.0
dotnet new classlib -n ADManagement.Infrastructure -f net9.0
dotnet new console -n ADManagement.Console -f net9.0
```

4. Add vào solution:
```powershell
dotnet sln add **/*.csproj
```

5. Add references:
```powershell
dotnet add ADManagement.Application reference ADManagement.Domain
dotnet add ADManagement.Infrastructure reference ADManagement.Domain ADManagement.Application
dotnet add ADManagement.Console reference ADManagement.Application ADManagement.Infrastructure
```

6. Add NuGet packages:
```powershell
# Application
cd ADManagement.Application
dotnet add package Microsoft.Extensions.DependencyInjection --version 9.0.0
dotnet add package Microsoft.Extensions.Logging.Abstractions --version 9.0.0
dotnet add package FluentValidation --version 11.11.0

# Infrastructure  
cd ../ADManagement.Infrastructure
dotnet add package System.DirectoryServices --version 9.0.0
dotnet add package EPPlus --version 7.5.2
dotnet add package Microsoft.Extensions.Logging --version 9.0.0
dotnet add package Microsoft.Extensions.Configuration --version 9.0.0

# Console
cd ../ADManagement.Console
dotnet add package Microsoft.Extensions.Hosting --version 9.0.0
dotnet add package Microsoft.Extensions.Configuration.Json --version 9.0.0
dotnet add package Microsoft.Extensions.Configuration.UserSecrets --version 9.0.0
dotnet add package Microsoft.Extensions.Configuration.EnvironmentVariables --version 9.0.0
dotnet add package Serilog.Extensions.Hosting --version 8.0.0
dotnet add package Serilog.Sinks.Console --version 6.0.0
dotnet add package Serilog.Sinks.File --version 6.0.0

cd ..
```

7. Copy các file source code vào đúng thư mục
8. Build:
```powershell
dotnet restore
dotnet build
```

### Cách 2: Sử dụng file ZIP (Khuyến nghị)

1. Giải nén file ADManagement.zip
2. Mở ADManagement.sln trong Visual Studio 2022
3. Restore NuGet packages (tự động)
4. Build solution (Ctrl + Shift + B)
5. Chạy ADManagement.Console project (F5)

## 📝 Ghi chú quan trọng

- Tất cả các file .cs đã được tạo và có trong package
- File .csproj cần được tạo với nội dung như trên
- Solution file (.sln) cần được tạo để quản lý toàn bộ projects
- Đảm bảo các NuGet packages được restore trước khi build
- User Secrets ID đã được set cho Console project

## 🎯 Next Steps sau khi setup

1. Cấu hình AD connection trong appsettings.json
2. Test connection đến Active Directory
3. Export users ra Excel để test
4. Explore các chức năng khác
