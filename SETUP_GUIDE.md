# 🚀 ADManagement - Hướng dẫn Setup từng bước

## 📋 Mục lục
1. [Yêu cầu hệ thống](#yêu-cầu-hệ-thống)
2. [Cài đặt môi trường](#cài-đặt-môi-trường)
3. [Setup dự án](#setup-dự-án)
4. [Cấu hình Active Directory](#cấu-hình-active-directory)
5. [Chạy ứng dụng](#chạy-ứng-dụng)
6. [Kiểm tra kết quả](#kiểm-tra-kết-quả)
7. [Troubleshooting](#troubleshooting)

---

## 1. Yêu cầu hệ thống

### ✅ Phần mềm cần thiết:

- **Operating System**: Windows 10/11 hoặc Windows Server 2016+
- **.NET SDK**: .NET 9.0 SDK ([Download](https://dotnet.microsoft.com/download/dotnet/9.0))
- **IDE** (chọn 1 trong 2):
  - Visual Studio 2022 (Community/Professional/Enterprise)
  - Visual Studio Code + C# Extension
- **Git** (Optional): Để clone repository

### ✅ Quyền truy cập:

- Máy tính phải join domain hoặc có khả năng kết nối đến Domain Controller
- Account có quyền đọc Active Directory
- Account có quyền admin (nếu cần thực hiện các thao tác như enable/disable user)

---

## 2. Cài đặt môi trường

### Bước 2.1: Cài đặt .NET 9.0 SDK

1. Truy cập: https://dotnet.microsoft.com/download/dotnet/9.0
2. Download ".NET SDK x64" cho Windows
3. Chạy installer và làm theo hướng dẫn
4. Kiểm tra cài đặt:

```bash
dotnet --version
# Kết quả: 9.0.x
```

### Bước 2.2: Cài đặt Visual Studio 2022 (Recommended)

1. Download Visual Studio 2022: https://visualstudio.microsoft.com/downloads/
2. Chọn workload khi cài đặt:
   - ✅ .NET desktop development
   - ✅ ASP.NET and web development (Optional)
3. Hoàn tất cài đặt

### Bước 2.3: Hoặc cài đặt VS Code (Alternative)

1. Download VS Code: https://code.visualstudio.com/
2. Cài đặt extensions:
   - C# for Visual Studio Code
   - C# Dev Kit
   - .NET Extension Pack

---

## 3. Setup dự án

### Bước 3.1: Giải nén source code

```bash
# Giải nén file ADManagement.zip vào thư mục của bạn
# Ví dụ: C:\Projects\ADManagement\
```

### Bước 3.2: Restore NuGet packages

Mở terminal/command prompt trong thư mục dự án:

```bash
cd C:\Projects\ADManagement
dotnet restore
```

### Bước 3.3: Build solution

```bash
dotnet build
```

Nếu build thành công, bạn sẽ thấy:

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

---

## 4. Cấu hình Active Directory

### Bước 4.1: Xác định thông tin AD của bạn

Bạn cần biết các thông tin sau:

1. **Domain Name**: Ví dụ: `corp.contoso.com`
2. **LDAP Server** (Optional): Địa chỉ IP hoặc hostname của DC
3. **Service Account** (Optional):
   - Username: `DOMAIN\username` hoặc `username@domain.com`
   - Password: `********`

**Cách kiểm tra Domain Name của bạn:**

```powershell
# Mở PowerShell và chạy:
$env:USERDNSDOMAIN
# Hoặc
(Get-WmiObject Win32_ComputerSystem).Domain
```

### Bước 4.2: Cấu hình trong appsettings.json

Mở file `ADManagement.Console/appsettings.json`:

```json
{
  "ADConfiguration": {
    "Domain": "corp.contoso.com",     // ⚠️ THAY ĐỔI DOMAIN NÀY
    "Username": "",                    // Để trống để dùng current user
    "Password": "",                    // Để trống để dùng current user
    "LdapServer": "",                  // Optional: để trống sẽ auto-detect
    "Port": 389,                       // 389 cho LDAP, 636 cho LDAPS
    "UseSSL": false,                   // true nếu dùng LDAPS (port 636)
    "PageSize": 1000,
    "TimeoutSeconds": 30,
    "DefaultSearchOU": ""              // Optional: OU mặc định để search
  }
}
```

### Bước 4.3: Cấu hình User Secrets (Recommended cho security)

Nếu bạn cần dùng specific username/password:

```bash
cd ADManagement.Console

# Initialize user secrets
dotnet user-secrets init

# Set username
dotnet user-secrets set "ADConfiguration:Username" "your-domain\your-username"

# Set password
dotnet user-secrets set "ADConfiguration:Password" "your-password"
```

**Lưu ý:** User secrets chỉ lưu trên máy local của bạn, không push lên Git.

### Bước 4.4: Cấu hình Export Settings

Trong cùng file `appsettings.json`, kiểm tra phần export:

```json
{
  "ExportConfiguration": {
    "OutputDirectory": "./Exports",              // Thư mục lưu file export
    "FilenamePattern": "ADExport_{0:yyyyMMdd_HHmmss}.xlsx",
    "IncludeDisabledAccounts": true,            // true = export cả disabled users
    "IncludeSystemAccounts": false,             // false = không export system accounts
    "AutoFitColumns": true,                     // Auto-fit Excel columns
    "ApplyFormatting": true,                    // Apply formatting to Excel
    "CreateTable": true                         // Create Excel table
  }
}
```

---

## 5. Chạy ứng dụng

### Bước 5.1: Chạy Console Application

#### Option 1: Từ Visual Studio
1. Mở file `ADManagement.sln`
2. Right-click vào project `ADManagement.Console`
3. Chọn "Set as Startup Project"
4. Nhấn F5 hoặc click Start

#### Option 2: Từ Command Line
```bash
cd ADManagement.Console
dotnet run
```

### Bước 5.2: Test Connection

Khi application khởi động, nó sẽ tự động test connection:

```
╔══════════════════════════════════════════════╗
║   AD Management Console Application          ║
╚══════════════════════════════════════════════╝

Testing Active Directory connection...
✓ Connection successful
```

Nếu thấy ✓ Connection successful → Bạn đã setup thành công!

---

## 6. Kiểm tra kết quả

### Test 1: Export Users

1. Chọn menu option `1` (Export All Users to Excel)
2. Nhấn Enter để dùng default path
3. Đợi quá trình export hoàn tất
4. Mở thư mục `Exports/` để xem file Excel

File Excel sẽ có:
- ✅ Header với formatting đẹp
- ✅ Data table với filters
- ✅ Freeze panes ở dòng đầu
- ✅ Auto-fit columns

### Test 2: Search Users

1. Chọn menu option `2` (Search Users)
2. Nhập tên user để tìm (ví dụ: "admin" hoặc "john")
3. Xem kết quả tìm kiếm

### Test 3: Get User Details

1. Chọn menu option `3` (Get User Details)
2. Nhập username chính xác
3. Xem thông tin chi tiết của user

---

## 7. Troubleshooting

### ❌ Lỗi: "Connection failed: The LDAP server is unavailable"

**Nguyên nhân:**
- Domain name không đúng
- Không thể kết nối đến Domain Controller
- Firewall block port 389/636

**Giải pháp:**

1. Kiểm tra domain name:
```powershell
$env:USERDNSDOMAIN
```

2. Test kết nối đến DC:
```powershell
Test-NetConnection -ComputerName your-domain.com -Port 389
```

3. Thử dùng IP của DC thay vì domain name:
```json
{
  "ADConfiguration": {
    "LdapServer": "192.168.1.10",  // IP của DC
    "Domain": "corp.contoso.com"
  }
}
```

### ❌ Lỗi: "The server is not operational"

**Nguyên nhân:**
- Service account không có quyền
- Password sai
- Account bị locked

**Giải pháp:**

1. Xóa username/password trong config để dùng current user:
```json
{
  "ADConfiguration": {
    "Username": "",
    "Password": ""
  }
}
```

2. Chạy app với account có quyền AD admin

3. Unlock account nếu bị khóa

### ❌ Lỗi: "Could not load file or assembly System.DirectoryServices"

**Nguyên nhân:**
- NuGet packages chưa được restore

**Giải pháp:**
```bash
dotnet restore
dotnet build
```

### ❌ Lỗi: Export Excel bị lỗi

**Nguyên nhân:**
- File Excel đang mở
- Không có quyền ghi vào thư mục Exports

**Giải pháp:**

1. Đóng file Excel nếu đang mở
2. Tạo thư mục Exports nếu chưa có:
```bash
mkdir Exports
```
3. Check quyền write vào thư mục

### ❌ Lỗi: "Access is denied"

**Nguyên nhân:**
- Account không có quyền thực hiện operation

**Giải pháp:**

1. Đối với read operations: Chỉ cần Domain User
2. Đối với write operations (enable/disable, password change): Cần Admin rights
3. Chạy app với account có đủ quyền

---

## 🎓 Best Practices

### 1. Security
- ✅ Luôn dùng User Secrets cho credentials
- ✅ Không commit passwords vào Git
- ✅ Sử dụng SSL/TLS (LDAPS) trong production
- ✅ Giới hạn quyền cho service accounts

### 2. Performance
- ✅ Sử dụng PageSize phù hợp (1000-5000)
- ✅ Filter users trước khi export
- ✅ Sử dụng Smart Update thay vì Full Export

### 3. Logging
- ✅ Check logs trong thư mục `logs/` khi có lỗi
- ✅ Log level mặc định: Information
- ✅ Tăng lên Debug nếu cần troubleshoot

---

## 📞 Hỗ trợ

Nếu gặp vấn đề không giải quyết được:

1. Check logs trong `logs/admanagement-YYYYMMDD.txt`
2. Đọc phần Troubleshooting trong README.md
3. Tạo issue trên GitHub với:
   - Mô tả lỗi
   - Log file
   - Steps to reproduce

---

## ✅ Checklist hoàn thành

Sau khi hoàn thành setup, bạn nên có:

- [ ] .NET 9.0 SDK đã cài đặt
- [ ] Source code đã giải nén và build thành công
- [ ] appsettings.json đã cấu hình đúng domain
- [ ] Connection test thành công (✓)
- [ ] Export users thành công, có file Excel
- [ ] Search users hoạt động tốt
- [ ] Hiểu cách sử dụng các chức năng chính

**Chúc mừng! Bạn đã setup thành công ADManagement! 🎉**

---

**Next Steps:**
- Explore các chức năng khác trong menu
- Đọc API Reference trong README.md
- Customize configuration theo nhu cầu
- Integrate vào workflow của bạn
