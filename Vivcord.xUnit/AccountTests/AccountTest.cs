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

        public AccountTest()
        {
            var services = new ServiceCollection();

            services.AddDbContext<MainDbContext>(options => options.UseInMemoryDatabase("TestDatabase"));

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
                roleManager.CreateAsync(new IdentityRole<Guid>("User")).GetAwaiter().GetResult();
                roleManager.CreateAsync(new IdentityRole<Guid>("Admin")).GetAwaiter().GetResult();
            }

            _userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
            _accountService = serviceProvider.GetRequiredService<IAccountService>();
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
    }
}