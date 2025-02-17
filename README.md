# UserIdentityApi

A robust ASP.NET Core Identity API for user authentication and management.

## Features

- 🔐 **Authentication & Authorization**
  - JWT-based authentication
  - Role-based authorization
  - Email verification
  - Password reset functionality

- 👥 **User Management**
  - User registration and login
  - Profile management
  - Role management
  - Profile photo upload

- 📧 **Email Services**
  - Email verification
  - Password reset notifications
  - SendGrid integration

- 🛡️ **Security**
  - JWT token authentication
  - Password hashing
  - Email verification
  - Secure password reset

## Tech Stack

- ASP.NET Core 7.0
- Entity Framework Core
- SQL Server
- SendGrid (Email Service)
- JWT for Authentication

## Project Structure

```
UserIdentityApi/
├── Controllers/
│   ├── AccountController.cs    # Authentication operations
│   ├── ManageController.cs     # User management
│   ├── UserController.cs       # User operations
│   └── RoleController.cs       # Role management
├── Models/
│   └── UserDtos.cs            # Data transfer objects
├── Data/
│   ├── Entities/              # Database entities
│   └── UserDbContext.cs       # EF Core context
├── Services/                   # Business logic services
├── Infrastructure/            # Cross-cutting concerns
└── Program.cs                 # Application configuration
```

## API Endpoints

### Authentication

```
POST /api/Account/login              # User login
POST /api/Account/register          # User registration
POST /api/Account/verify-email      # Email verification
POST /api/Account/forgot-password   # Password reset request
POST /api/Account/reset-password    # Password reset
```

### User Management

```
GET    /api/Manage/profile          # Get user profile
POST   /api/Manage/update           # Update profile
POST   /api/Manage/upload-photo     # Upload profile photo
GET    /api/Manage/users            # Get all users (Admin)
POST   /api/Manage/create-user      # Create user (Admin)
PUT    /api/Manage/update-user/{id} # Update user (Admin)
DELETE /api/Manage/delete-user/{id} # Delete user (Admin)
```

## Getting Started

1. **Prerequisites**
   - .NET 7.0 SDK
   - SQL Server
   - SendGrid Account

2. **Configuration**
   Update `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Your_SQL_Connection_String"
     },
     "JwtConfiguration": {
       "SecurityKey": "Your_JWT_Security_Key",
       "Issuer": "Your_Issuer",
       "Audience": "Your_Audience"
     },
     "SendGridOptions": {
       "ApiKey": "Your_SendGrid_API_Key"
     }
   }
   ```

3. **Database Setup**
   ```bash
   dotnet ef database update
   ```

4. **Run the Application**
   ```bash
   dotnet run
   ```

## Database Migrations

### Initial Setup
1. **Install EF Core Tools** (if not already installed):
   ```bash
   dotnet tool install --global dotnet-ef
   ```

2. **Add a New Migration**:
   ```bash
   # Create a new migration
   dotnet ef migrations add InitialCreate --project UserIdentityApi

   # Create a migration with a specific name
   dotnet ef migrations add AddUserProfilePhoto --project UserIdentityApi
   ```

3. **Apply Migrations**:
   ```bash
   # Update database with all pending migrations
   dotnet ef database update --project UserIdentityApi

   # Update to a specific migration
   dotnet ef database update MigrationName --project UserIdentityApi
   ```

### Common Migration Commands

```bash
# List all migrations
dotnet ef migrations list --project UserIdentityApi

# Remove last migration (if not applied to database)
dotnet ef migrations remove --project UserIdentityApi

# Generate SQL script for all migrations
dotnet ef migrations script --project UserIdentityApi

# Generate SQL script from one migration to another
dotnet ef migrations script Migration1 Migration2 --project UserIdentityApi
```

### Troubleshooting Migrations

1. **Reset Database**:
   ```bash
   # Drop the database
   dotnet ef database drop --project UserIdentityApi

   # Remove all migrations
   dotnet ef migrations remove --project UserIdentityApi

   # Start fresh
   dotnet ef migrations add InitialCreate --project UserIdentityApi
   dotnet ef database update --project UserIdentityApi
   ```

2. **Common Issues**:
   - If migration fails, check the migration history table
   - Ensure all model changes are properly defined
   - Verify connection string is correct
   - Check for pending changes in other migrations

## Security Considerations

- JWT tokens expire after 24 hours
- Passwords are hashed using ASP.NET Core Identity
- Email verification required for new accounts
- Role-based access control implemented

## Error Handling

The API uses standard HTTP status codes:
- 200: Success
- 400: Bad Request
- 401: Unauthorized
- 403: Forbidden
- 404: Not Found
- 500: Internal Server Error

## Development Guidelines

1. **Code Style**
   - Follow C# coding conventions
   - Use async/await for asynchronous operations
   - Implement proper exception handling

2. **Security**
   - Never commit sensitive data
   - Always validate user input
   - Use proper authorization attributes

3. **Testing**
   - Write unit tests for new features
   - Test API endpoints with Swagger
   - Validate security measures

## Production Deployment

1. Update connection strings and configuration
2. Set up proper CORS policies
3. Configure proper logging
4. Set up monitoring
5. Use HTTPS
6. Implement rate limiting

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License. 