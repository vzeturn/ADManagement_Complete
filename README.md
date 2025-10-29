# ADManagement - Active Directory Management System

## 📋 Mô tả dự án

ADManagement là một hệ thống quản lý Active Directory hiện đại, được xây dựng theo Clean Architecture với .NET 9.0. Dự án cung cấp các tính năng:

- ✅ Quản lý người dùng AD (thêm, sửa, xóa, tìm kiếm)
- ✅ Xuất dữ liệu ra Excel với formatting chuyên nghiệp
- ✅ Quản lý nhóm và quyền
- ✅ Thay đổi mật khẩu và unlock tài khoản
- ✅ Console Application và WPF Application
- ✅ Logging chi tiết với Serilog
- ✅ Error handling toàn diện
- ✅ Validation với FluentValidation
- ✅ Dependency Injection
- ✅ Asynchronous operations

## 🏗️ Kiến trúc dự án

```
ADManagement/
├── ADManagement.Domain/          # Domain Layer - Entities, Interfaces
│   ├── Entities/
│   ├── Interfaces/
│   ├── Common/
│   └── Exceptions/
├── ADManagement.Application/     # Application Layer - Business Logic
│   ├── DTOs/
│   ├── Services/
│   ├── Interfaces/
│   ├── Configuration/
│   ├── Validators/
│   └── Mappings/
├── ADManagement.Infrastructure/  # Infrastructure Layer - Data Access
│   ├── Repositories/
│   └── Exporters/
├── ADManagement.Console/         # Console Application
└── ADManagement.WPF/            # WPF Application
```

## 🔧 Yêu cầu hệ thống

- .NET 9.0 SDK
- Windows Server với Active Directory
- Visual Studio 2022 hoặc VS Code
- Quyền truy cập Active Directory

## 📦 Cài đặt

### Bước 1: Clone hoặc giải nén dự án

```bash
# Nếu clone từ git
git clone <repository-url>
cd ADManagement

# Hoặc giải nén file zip
unzip ADManagement.zip
cd ADManagement
```

### Bước 2: Restore NuGet packages

```bash
dotnet restore
```

### Bước 3: Cấu hình kết nối Active Directory

Mở file `ADManagement.Console/appsettings.json` và cập nhật:

```json
{
  "ADConfiguration": {
    "Domain": "corp.contoso.com",     // Thay bằng domain của bạn
    "Username": "",                    // Để trống để dùng current user
    "Password": "",                    // Để trống để dùng current user
    "Port": 389,
    "UseSSL": false
  }
}
```

**Lưu ý bảo mật:** Không nên lưu password trực tiếp trong appsettings.json. Sử dụng User Secrets hoặc Environment Variables:

#### Sử dụng User Secrets (Khuyến nghị)

```bash
cd ADManagement.Console
dotnet user-secrets init
dotnet user-secrets set "ADConfiguration:Username" "your-username"
dotnet user-secrets set "ADConfiguration:Password" "your-password"
```

#### Sử dụng Environment Variables

```bash
# Windows PowerShell
$env:ADConfiguration__Username = "your-username"
$env:ADConfiguration__Password = "your-password"

# Windows CMD
set ADConfiguration__Username=your-username
set ADConfiguration__Password=your-password

# Linux/Mac
export ADConfiguration__Username="your-username"
export ADConfiguration__Password="your-password"
```

### Bước 4: Build dự án

```bash
dotnet build
```

### Bước 5: Chạy Console Application

```bash
cd ADManagement.Console
dotnet run
```

## 🎯 Hướng dẫn sử dụng Console App

### Menu chính

```
╔══════════════════════════════════════════════╗
║              MAIN MENU                       ║
╚══════════════════════════════════════════════╝

  1. Export All Users to Excel
  2. Search Users
  3. Get User Details
  4. Enable/Disable User
  5. Unlock User Account
  6. Change User Password
  7. Export All Groups to Excel
  8. Manage User Groups
  0. Exit
```

### 1. Export All Users to Excel

- Xuất toàn bộ users ra file Excel
- File được lưu trong thư mục `Exports/`
- Bao gồm đầy đủ thông tin: contact, organization, account status

### 2. Search Users

- Tìm kiếm users theo username hoặc display name
- Hỗ trợ wildcard search
- Hiển thị danh sách kết quả

### 3. Get User Details

- Xem chi tiết thông tin của một user
- Bao gồm: account status, groups, last logon, etc.

### 4. Enable/Disable User

- Kích hoạt hoặc vô hiệu hóa tài khoản user
- Yêu cầu quyền admin

### 5. Unlock User Account

- Mở khóa tài khoản bị locked
- Hữu ích khi user nhập sai password nhiều lần

### 6. Change User Password

- Thay đổi mật khẩu cho user
- Validate password theo policy: 
  - Tối thiểu 8 ký tự
  - Phải có chữ hoa, chữ thường, số, ký tự đặc biệt

### 7. Export All Groups to Excel

- Xuất danh sách groups ra Excel
- Bao gồm members và group properties

### 8. Manage User Groups

- Xem danh sách groups của user
- Thêm user vào group
- Xóa user khỏi group

## 📊 Cấu trúc Excel Export

### Users Export

| Column | Description |
|--------|-------------|
| Username | SAM Account Name |
| Display Name | Full display name |
| Email | Email address |
| Department | Department |
| Title | Job title |
| Account Status | Active/Disabled/Locked |
| Last Logon | Last successful logon |
| Groups | List of groups |
| ... | 31 columns total |

### Features:
- ✅ Auto-fit columns
- ✅ Excel Tables với filters
- ✅ Header formatting
- ✅ Freeze panes
- ✅ Professional styling

## 🔐 Bảo mật

### Best Practices:

1. **Không lưu credentials trong code hoặc appsettings.json**
   - Sử dụng User Secrets cho development
   - Sử dụng Azure Key Vault hoặc Environment Variables cho production

2. **Sử dụng SSL/TLS cho LDAP connection**
   ```json
   {
     "ADConfiguration": {
       "UseSSL": true,
       "Port": 636
     }
   }
   ```

3. **Giới hạn quyền truy cập**
   - Chỉ cấp quyền cần thiết cho service account
   - Sử dụng Read-Only account cho operations chỉ đọc

4. **Audit logging**
   - Tất cả operations đều được log
   - Logs được lưu trong `logs/` directory

## 🧪 Testing

### Unit Tests (Sẽ cập nhật)

```bash
dotnet test
```

### Integration Tests

```bash
# Test connection
dotnet run -- test-connection

# Test export
dotnet run -- export-users --output test.xlsx
```

## 📝 Logging

Logs được lưu tại:
- Console output (real-time)
- File: `logs/admanagement-YYYYMMDD.txt`

Log levels:
- Information: Operations thành công
- Warning: Operations thất bại nhưng không critical
- Error: Lỗi cần investigate
- Fatal: Application crash

## 🐛 Troubleshooting

### Lỗi kết nối AD

**Lỗi:** "Connection failed: The LDAP server is unavailable"

**Giải pháp:**
1. Kiểm tra domain name trong config
2. Kiểm tra firewall (port 389 hoặc 636)
3. Kiểm tra DNS resolution
4. Thử ping domain controller

### Lỗi authentication

**Lỗi:** "A referral was returned from the server"

**Giải pháp:**
1. Sử dụng FQDN (fully qualified domain name)
2. Kiểm tra username/password
3. Kiểm tra account có bị locked không

### Lỗi export Excel

**Lỗi:** "The process cannot access the file"

**Giải pháp:**
1. Đóng file Excel nếu đang mở
2. Kiểm tra quyền ghi vào thư mục Exports
3. Đổi tên file output

## 🚀 Performance Tips

1. **Sử dụng paging cho large datasets**
   ```json
   {
     "ADConfiguration": {
       "PageSize": 1000
     }
   }
   ```

2. **Filter users trước khi export**
   - Chỉ export users từ specific OU
   - Exclude disabled accounts nếu không cần

3. **Sử dụng Smart Update thay vì Export full**
   - Smart Update chỉ cập nhật thay đổi
   - Nhanh hơn cho scheduled exports

## 📚 API Reference

### IADUserService

```csharp
Task<Result> TestConnectionAsync();
Task<Result<IEnumerable<ADUserDto>>> GetAllUsersAsync();
Task<Result<ADUserDto>> GetUserByUsernameAsync(string username);
Task<Result> ChangePasswordAsync(PasswordChangeRequest request);
Task<Result> EnableUserAsync(string username);
Task<Result> DisableUserAsync(string username);
```

### IExportService

```csharp
Task<Result<string>> ExportAllUsersAsync(string? outputPath = null);
Task<Result<string>> ExportUsersByOUAsync(string ouPath, string? outputPath = null);
Task<Result<string>> SmartUpdateUsersAsync(string filePath);
Task<Result<string>> ExportAllGroupsAsync(string? outputPath = null);
```

## 🔄 Roadmap

- [ ] WPF Application với UI hiện đại
- [ ] REST API với ASP.NET Core
- [ ] Scheduled export với Windows Service
- [ ] Email notifications
- [ ] PowerShell module
- [ ] Unit tests & Integration tests
- [ ] Docker support
- [ ] Azure AD integration

## 👥 Đóng góp

Contributions are welcome! Please:
1. Fork the project
2. Create your feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## 📄 License

This project is licensed under the MIT License.

## 📧 Liên hệ

Nếu có câu hỏi hoặc vấn đề, vui lòng tạo issue trên GitHub repository.

## 🙏 Acknowledgments

- EPPlus for Excel export
- Serilog for logging
- FluentValidation for validation
- .NET Team for awesome framework

---

**Made with ❤️ using .NET 9.0 and Clean Architecture**
