using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Vivcord.Server.DbContext;
using Vivcord.Server.DTO;
using Vivcord.Server.Infastructure.Jwt;
using Vivcord.Server.Models;
using Vivcord.Server.Services;
using Vivcord.Server.Extensions;

namespace Vivcord.xUnit.AccountTests
{
    public class AccountTest
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IAccountService _accountService;
        private readonly MainDbContext _dbContext;

        public AccountTest()
        {
            var services = new ServiceCollection();

            // Use unique database name per test instance to prevent shared state
            var dbName = $"TestDatabase_{Guid.NewGuid()}";
            services.AddDbContext<MainDbContext>(options => options.UseInMemoryDatabase(dbName));

            services.AddVivcordIdentity();

            services.AddLogging();

            var jwtMock = new Mock<ITokenService>();
            jwtMock.Setup(x => x.GetTokenAsync(It.IsAny<AppUser>())).ReturnsAsync("jwt-token");
            jwtMock.Setup(x => x.GetRefreshToken()).Returns(() => Guid.NewGuid().ToString());
            services.AddSingleton(provider => jwtMock.Object);

            services.AddScoped<IAccountService, AccountService>();
            services.AddSingleton(TimeProvider.System);

            var serviceProvider = services.BuildServiceProvider();

            // Seed roles for tests
            using (var scope = serviceProvider.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                // Check if roles already exist before creating
                if (roleManager.FindByNameAsync("User").GetAwaiter().GetResult() == null)
                    roleManager.CreateAsync(new IdentityRole<Guid>("User")).GetAwaiter().GetResult();
                if (roleManager.FindByNameAsync("Admin").GetAwaiter().GetResult() == null)
                    roleManager.CreateAsync(new IdentityRole<Guid>("Admin")).GetAwaiter().GetResult();
            }

            _userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
            _accountService = serviceProvider.GetRequiredService<IAccountService>();
            _dbContext = serviceProvider.GetRequiredService<MainDbContext>();
        }

        [Fact]
        public async Task Fail_Register_Password_length()
        {
            // Arrange
            var registerDto = new RegisterDTO
            {
                Name = "TestUser",
                Email = "user@test.com",
                Password = "pass1"
            };
            // Act
            var result = await _accountService.UserRegister(registerDto);
            //Assert
            Assert.True(result.IsError);
        }
        [Fact]
        public async Task Fail_Register_Password_No_Digit()
        {
            // Arrange
            var registerDto = new RegisterDTO
            {
                Name = "TestUser",
                Email = "user@test.com",
                Password = "password"
            };
            // Act
            var result = await _accountService.UserRegister(registerDto);
            //Assert
            Assert.True(result.IsError);
        }
        [Fact]
        public async Task Fail_Register_Password_No_Letter()
        {
            // Arrange
            var registerDto = new RegisterDTO
            {
                Name = "TestUser",
                Email = "user@test.com",
                Password = "123456"
            };
            // Act
            var result = await _accountService.UserRegister(registerDto);
            //Assert
            Assert.True(result.IsError);
        }

        [Fact]
        public async Task Fail_Login_UserNotFound()
        {
            // Arrange
            var loginDto = new LoginDTO
            {
                Email = "nonexistent@test.com",
                Password = "Password123"
            };
            // Act
            var result = await _accountService.UserLogin(loginDto);
            // Assert
            Assert.True(result.IsError);
            Assert.Equal("UserNotFound", result.Errors.First().Code);
        }

        [Fact]
        public async Task Fail_Login_InvalidPassword()
        {
            // Arrange
            var registerDto = new RegisterDTO
            {
                Name = "TestUser",
                Email = "user@test.com",
                Password = "Password123"
            };
            await _accountService.UserRegister(registerDto);

            var loginDto = new LoginDTO
            {
                Email = "user@test.com",
                Password = "WrongPassword123"
            };
            // Act
            var result = await _accountService.UserLogin(loginDto);
            // Assert
            Assert.True(result.IsError);
            Assert.Equal("InvalidPassword", result.Errors.First().Code);
        }

        [Fact]
        public async Task Success_Login_ValidCredentials()
        {
            // Arrange
            var registerDto = new RegisterDTO
            {
                Name = "TestUser",
                Email = "user@test.com",
                Password = "Password123"
            };
            await _accountService.UserRegister(registerDto);

            var loginDto = new LoginDTO
            {
                Email = "user@test.com",
                Password = "Password123"
            };
            // Act
            var result = await _accountService.UserLogin(loginDto);
            // Assert
            Assert.False(result.IsError);
            Assert.NotNull(result.Value);
            Assert.Equal("user@test.com", result.Value.User.Email);
            Assert.NotNull(result.Value.Token);
            Assert.NotNull(result.Value.RefreshToken);
        }

        [Fact]
        public async Task Success_Logout_RemovesOnlyCurrentRefreshToken()
        {
            var registerResult = await _accountService.UserRegister(new RegisterDTO
            {
                Name = "TestUser",
                Email = "logout@test.com",
                Password = "Password123"
            });
            var loginResult = await _accountService.UserLogin(new LoginDTO
            {
                Email = "logout@test.com",
                Password = "Password123"
            });

            var logoutResult = await _accountService.UserLogout(registerResult.Value.RefreshToken);

            Assert.False(logoutResult.IsError);
            Assert.False(await _dbContext.RefreshTokens.AnyAsync(t => t.Token == registerResult.Value.RefreshToken));
            Assert.True(await _dbContext.RefreshTokens.AnyAsync(t => t.Token == loginResult.Value.RefreshToken));
        }

        [Fact]
        public async Task Success_Login_UserHasCorrectRoles()
        {
            // Arrange
            var registerDto = new RegisterDTO
            {
                Name = "TestUser",
                Email = "user@test.com",
                Password = "Password123"
            };
            await _accountService.UserRegister(registerDto);

            var loginDto = new LoginDTO
            {
                Email = "user@test.com",
                Password = "Password123"
            };
            // Act
            var result = await _accountService.UserLogin(loginDto);
            // Assert
            Assert.False(result.IsError);
            Assert.NotEmpty(result.Value.User.Roles);
            Assert.Contains("User", result.Value.User.Roles);
        }

        [Fact]
        public async Task Fail_Login_EmptyEmail()
        {
            // Arrange
            var loginDto = new LoginDTO
            {
                Email = "",
                Password = "Password123"
            };
            // Act
            var result = await _accountService.UserLogin(loginDto);
            // Assert
            Assert.True(result.IsError);
        }

        [Fact]
        public async Task Success_Login_MultipleAttempts()
        {
            // Arrange
            var registerDto = new RegisterDTO
            {
                Name = "TestUser",
                Email = "user@test.com",
                Password = "Password123"
            };
            await _accountService.UserRegister(registerDto);

            var loginDto = new LoginDTO
            {
                Email = "user@test.com",
                Password = "Password123"
            };
            // Act - First login
            var result1 = await _accountService.UserLogin(loginDto);
            // Act - Second login
            var result2 = await _accountService.UserLogin(loginDto);
            // Assert
            Assert.False(result1.IsError);
            Assert.False(result2.IsError);
            Assert.NotEqual(result1.Value.RefreshToken, result2.Value.RefreshToken);
        }

        [Fact]
        public async Task Fail_Register_DuplicateEmail()
        {
            // Arrange
            var firstRegisterDto = new RegisterDTO
            {
                Name = "FirstUser",
                Email = "duplicate@test.com",
                Password = "Password123"
            };
            await _accountService.UserRegister(firstRegisterDto);

            var secondRegisterDto = new RegisterDTO
            {
                Name = "SecondUser",
                Email = "duplicate@test.com",
                Password = "Password123"
            };
            // Act
            var result = await _accountService.UserRegister(secondRegisterDto);
            // Assert
            Assert.True(result.IsError);
            Assert.Equal("EmailAlreadyRegistered", result.Errors.First().Code);
        }

        [Fact]
        public async Task Fail_Register_DuplicateUsername()
        {
            // Arrange
            var firstRegisterDto = new RegisterDTO
            {
                Name = "SameUsername",
                Email = "user1@test.com",
                Password = "Password123"
            };
            await _accountService.UserRegister(firstRegisterDto);

            var secondRegisterDto = new RegisterDTO
            {
                Name = "SameUsername",
                Email = "user2@test.com",
                Password = "Password123"
            };
            // Act
            var result = await _accountService.UserRegister(secondRegisterDto);
            // Assert
            Assert.True(result.IsError);
            Assert.Equal("UsernameAlreadyTaken", result.Errors.First().Code);
        }

        [Fact]
        public async Task Success_RefreshUserToken_ReplacesUsedToken()
        {
            // Arrange
            var registerResult = await _accountService.UserRegister(new RegisterDTO
            {
                Name = "RefreshUser",
                Email = "refresh@test.com",
                Password = "Password123"
            });

            // Act
            var result = await _accountService.RefreshUserToken(registerResult.Value.RefreshToken);

            // Assert
            Assert.False(result.IsError);
            Assert.NotEqual(registerResult.Value.RefreshToken, result.Value.RefreshToken);
            Assert.True(await _dbContext.RefreshTokens
                .AnyAsync(token => token.Token == registerResult.Value.RefreshToken && token.IsUsed));
            Assert.True(await _dbContext.RefreshTokens
                .AnyAsync(token => token.Token == result.Value.RefreshToken && !token.IsUsed && !token.IsRevoked));
            Assert.Equal("refresh@test.com", result.Value.User.Email);
        }

        [Fact]
        public async Task Fail_RefreshUserToken_TokenNotFoundOrInvalid()
        {
            // Arrange
            const string token = "unknown-refresh-token";

            // Act
            var result = await _accountService.RefreshUserToken(token);

            // Assert
            Assert.True(result.IsError);
            Assert.Equal("InvalidToken", result.Errors.First().Code);
        }

        [Theory]
        [InlineData("expired")]
        [InlineData("used")]
        [InlineData("revoked")]
        public async Task Fail_RefreshUserToken_InactiveToken(string state)
        {
            // Arrange
            var token = new RefreshToken
            {
                Token = $"{state}-refresh-token",
                UserId = Guid.NewGuid(),
                Created = DateTimeOffset.UtcNow.AddDays(-2),
                Expires = state == "expired" ? DateTimeOffset.UtcNow.AddDays(-1) : DateTimeOffset.UtcNow.AddDays(1),
                IsUsed = state == "used",
                IsRevoked = state == "revoked"
            };
            await _dbContext.RefreshTokens.AddAsync(token);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _accountService.RefreshUserToken(token.Token);

            // Assert
            Assert.True(result.IsError);
            Assert.Equal("InvalidToken", result.Errors.First().Code);
        }

        [Fact]
        public async Task Fail_RefreshUserToken_UserNotFound()
        {
            // Arrange
            var token = new RefreshToken
            {
                Token = "orphaned-refresh-token",
                UserId = Guid.NewGuid(),
                Created = DateTimeOffset.UtcNow,
                Expires = DateTimeOffset.UtcNow.AddDays(1)
            };
            await _dbContext.RefreshTokens.AddAsync(token);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _accountService.RefreshUserToken(token.Token);

            // Assert
            Assert.True(result.IsError);
            Assert.Equal("UserNotFound", result.Errors.First().Code);
            Assert.False((await _dbContext.RefreshTokens.SingleAsync()).IsUsed);
        }

        [Fact]
        public async Task Fail_Logout_TokenNotFound()
        {
            // Arrange
            const string token = "unknown-refresh-token";

            // Act
            var result = await _accountService.UserLogout(token);

            // Assert
            Assert.True(result.IsError);
            Assert.Equal("TokenNotFound", result.Errors.First().Code);
        }

        [Fact]
        public async Task Success_GetActiveUser_ReturnsUserAndRoles()
        {
            // Arrange
            var registerResult = await _accountService.UserRegister(new RegisterDTO
            {
                Name = "ActiveUser",
                Email = "active@test.com",
                Password = "Password123"
            });

            // Act
            var result = await _accountService.GetActiveUser(Guid.Parse(registerResult.Value.User.Id));

            // Assert
            Assert.False(result.IsError);
            Assert.Equal("active@test.com", result.Value.Email);
            Assert.Equal("ActiveUser", result.Value.DisplayName);
            Assert.Contains("User", result.Value.Roles);
        }

        [Fact]
        public async Task Fail_GetActiveUser_UserNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var result = await _accountService.GetActiveUser(userId);

            // Assert
            Assert.True(result.IsError);
            Assert.Equal("UserNotFound", result.Errors.First().Code);
        }

        [Fact]
        public async Task Success_ForgotPasswordEmail_UnknownEmailDoesNotRevealAccountExistence()
        {
            // Arrange
            const string email = "unknown@test.com";

            // Act
            var result = await _accountService.ForgotPasswordEmail(email);

            // Assert
            Assert.False(result.IsError);
        }

        [Fact]
        public async Task Fail_ForgotPasswordEmail_EmailServiceUnavailable()
        {
            // Arrange
            await _accountService.UserRegister(new RegisterDTO
            {
                Name = "PasswordResetUser",
                Email = "password-reset@test.com",
                Password = "Password123"
            });

            // Act
            var result = await _accountService.ForgotPasswordEmail("password-reset@test.com");

            // Assert
            Assert.True(result.IsError);
            Assert.Equal("EmailServiceUnavailable", result.Errors.First().Code);
        }

        [Fact]
        public async Task Fail_ResetPassword_UserNotFound()
        {
            // Arrange
            var request = new ResetPasswordDTO(Guid.NewGuid(), "reset-token", "NewPassword123");

            // Act
            var result = await _accountService.ResetPassword(request);

            // Assert
            Assert.True(result.IsError);
            Assert.Equal("UserNotFound", result.Errors.First().Code);
        }

        [Fact]
        public async Task Fail_ResetPassword_InvalidEncodedToken()
        {
            // Arrange
            var registerResult = await _accountService.UserRegister(new RegisterDTO
            {
                Name = "PasswordResetUser",
                Email = "invalid-reset-token@test.com",
                Password = "Password123"
            });
            var request = new ResetPasswordDTO(
                Guid.Parse(registerResult.Value.User.Id),
                "%",
                "NewPassword123");

            // Act
            var result = await _accountService.ResetPassword(request);

            // Assert
            Assert.True(result.IsError);
            Assert.Equal("InvalidToken", result.Errors.First().Code);
        }

        [Fact]
        public async Task Fail_ResetPassword_IdentityRejectsInvalidResetToken()
        {
            // Arrange
            var registerResult = await _accountService.UserRegister(new RegisterDTO
            {
                Name = "PasswordResetUser",
                Email = "identity-reset-token@test.com",
                Password = "Password123"
            });
            var encodedToken = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(
                System.Text.Encoding.UTF8.GetBytes("invalid-identity-reset-token"));
            var request = new ResetPasswordDTO(
                Guid.Parse(registerResult.Value.User.Id),
                encodedToken,
                "NewPassword123");

            // Act
            var result = await _accountService.ResetPassword(request);

            // Assert
            Assert.True(result.IsError);
            Assert.Contains(result.Errors, error => error.Code == "InvalidToken");
        }
    }
}