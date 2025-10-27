
### 3. Run Entity Framework Migrations

From the ARS project directory:

```bash
# Create initial migration
dotnet ef migrations add InitialCreate

# Apply migration to database
dotnet ef database update
```

## Testing the Connection

You can test the connection by running:

```bash
dotnet build
dotnet run
```

### Register User
```bash
POST http://localhost:5096/api/auth/register
Content-Type: application/json

{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com",
  "password": "SecurePassword123!",
  "phone": "+1-555-123-4567",
  "address": "123 Main Street, New York, NY 10001",
  "gender": "M",
  "age": 30,
  "creditCardNumber": "4532123456789012",
  "skyMiles": 0,
  "role": "User"
}

{
  "firstName": "Jane",
  "lastName": "Smith",
  "email": "jane.smith@example.com",
  "password": "MyPassword456!",
  "phone": "555-987-6543",
  "address": "456 Oak Avenue, Los Angeles, CA 90001",
  "gender": "F",
  "age": 25,
  "creditCardNumber": "5425233430109903"
}
```

### Login User
```bash
POST http://localhost:5000/api/auth/login
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "password123"
}
```

## Troubleshooting

### Common Issues:

4. **SSL connection error**
   - Add `SslMode=None` to connection string for development (not recommended for production)

