using ARS.Models;
using ARS.Data;
using ARS.Models.DTO;
using Microsoft.EntityFrameworkCore;

namespace ARS.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _dbContext;

        public UserService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<AuthResDTO?> RegisterAsync(RegDTO registerDto)
        {
            if (string.IsNullOrEmpty(registerDto.Email))
            {
                return new AuthResDTO
                {
                    Success = false,
                    Message = "Email is required."
                };
            }

            if (string.IsNullOrEmpty(registerDto.Password))
            {
                return new AuthResDTO
                {
                    Success = false,
                    Message = "Password is required."
                };
            }

            if (registerDto.Password.Length < 6)
            {
                return new AuthResDTO
                {
                    Success = false,
                    Message = "Password must be at least 6 characters."
                };
            }

            // Check if user already exists
            var existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == registerDto.Email);
            if (existingUser != null)
            {
                return new AuthResDTO
                {
                    Success = false,
                    Message = "User with this email already exists."
                };
            }

            var user = new User
            {
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Email = registerDto.Email,
                Password = registerDto.Password,
                Phone = registerDto.Phone,
                Address = registerDto.Address,
                Gender = registerDto.Gender,
                Age = registerDto.Age,
                CreditCardNumber = registerDto.CreditCardNumber,
                SkyMiles = registerDto.SkyMiles,
                Role = registerDto.Role
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            return new AuthResDTO
            {
                Success = true,
                Message = "User registered successfully."
            };
        }

        public async Task<AuthResDTO?> LoginAsync(LoginDTO loginDto)
        {
            if (string.IsNullOrEmpty(loginDto.Email))
            {
                return new AuthResDTO
                {
                    Success = false,
                    Message = "Email is required."
                };
            }

            if (string.IsNullOrEmpty(loginDto.Password))
            {
                return new AuthResDTO
                {
                    Success = false,
                    Message = "Password is required."
                };
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null)
            {
                return new AuthResDTO
                {
                    Success = false,
                    Message = "Invalid email or password."
                };
            }

            if (!user.Password.Equals(loginDto.Password))
            {
                return new AuthResDTO
                {
                    Success = false,
                    Message = "Invalid email or password."
                };
            }

            return new AuthResDTO
            {
                Success = true,
                Message = "Login successful."
            };
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            return await _dbContext.Users.AnyAsync(u => u.Email == email);
        }
    }

    public interface IUserService
    {
        Task<AuthResDTO?> RegisterAsync(RegDTO registerDto);
        Task<AuthResDTO?> LoginAsync(LoginDTO loginDto);
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> UserExistsAsync(string email);
    }
}