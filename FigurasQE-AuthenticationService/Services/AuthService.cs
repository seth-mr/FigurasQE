using FigurasQE_AuthenticationService.Data.Repositories;
using FigurasQE_AuthenticationService.Models;


namespace FigurasQE_AuthenticationService.Services
{
    public class AuthService
    {
        private readonly JwtService JwtProvider;
        private readonly UserRepository UserRepo;

        public AuthService(JwtService jwt, UserRepository dao)
        {
            JwtProvider = jwt;
            UserRepo = dao;
        }

        public async Task<AuthResponse> Signup(RegisterRequest user)
        {
            var created = await UserRepo.RegisterUserAsync(user);
            if (!created) throw new Exception("Error creating user");

            var loginUser = await UserRepo.GetUserWithCredentialsAsync(user.Email);
            if (loginUser is null)
            {
                throw new Exception("Error retrieving created user");
            }

            return BuildAuthResponse(loginUser);
        }

        public async Task<AuthResponse> Login(LoginRequest user)
        {
            var loginUser = await UserRepo.GetUserWithCredentialsAsync(user.Email);

            if (loginUser != null && loginUser.Role == "admin")
            {
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            if (loginUser != null && BCrypt.Net.BCrypt.Verify(user.Password, loginUser.Password))
            {
                return BuildAuthResponse(loginUser);
            }
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        public async Task<AuthResponse> AdminLogin(LoginRequest user)
        {
            var loginUser = await UserRepo.GetAdminWithCredentialsAsync(user.Email);

            if (loginUser != null && BCrypt.Net.BCrypt.Verify(user.Password, loginUser.Password))
            {
                return BuildAuthResponse(loginUser);
            }

            throw new UnauthorizedAccessException("Invalid credentials");
        }

        private AuthResponse BuildAuthResponse(AuthUser user)
        {
            return new AuthResponse
            {
                Token = JwtProvider.EncodeToken(user),
                Role = user.Role,
                Admin = user.Admin
            };
        }

    }
}