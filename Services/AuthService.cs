using Authentication.Context;
using Authentication.Controllers;
using Authentication.Models;
using Authentication.Services;
using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

public struct RequestResponse 
{
    public bool success { get; set; }
    public string message { get; set; }

    public bool? verified { get; set; }
}
public class AuthService
{
    private readonly DataContext _context;
    private readonly ILogger<LoginController> _logger;
    private readonly EmailService _emailService;

    public AuthService(ILogger<LoginController> logger, DataContext context, EmailService emailService)
    {
        _logger = logger;
        _context = context;
        _emailService = emailService;
    }

    public async Task<RequestResponse> Register(UserRegisterRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email || u.Username == request.Username))
        {
            return new RequestResponse 
            {
                success = false,
                message = "User already exist."
            };
        }

        CreatePasswordHash(request.Password, out byte[] passwordHash, out byte[] passwordSalt);

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            VerificationToken = CreateRandomToken(),
            AccountType = request.Password.IsNullOrEmpty() ? AccountType.Google : AccountType.Manual
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var verificationUrl = $"http://localhost:5038/Login/Verification?Token={user.VerificationToken}";

        await _emailService.SendEmail(new EmailData 
        { 
            Email = request.Email,
            Subject = "Account Registered",
            Message = $"Thank you for joining, please verify inorder to use your account. \n Verfication Code: {verificationUrl}"

        });

        return new RequestResponse
        {
            success = true,
            message = "User created."
        };
    }

    public async Task<RequestResponse> Login(UserLoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.EmailOrUsername || u.Username == request.EmailOrUsername);
        if (user == null || user.AccountType == AccountType.Google)
        {
            return new RequestResponse
            {
                success = false,
                message = "If user is active, you will get an email shortly."
            };
        }

        _logger.Log(LogLevel.Warning, $"{request.EmailOrUsername} {user.PasswordHash} {user.PasswordSalt}");

        if (!VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            return new RequestResponse
            {
                success = false,
                message = "Incorrect password."
            };
        }

        if (user.VerifiedAt == null)
        {
            return new RequestResponse
            {
                success = false,
                message = "User not verified",
                verified = false
            };
        }

        return new RequestResponse
        {
            success = true,
            message = $"Welcome back. {request.EmailOrUsername}"
        };
    }

    [HttpPost("verify")]
    public async Task<RequestResponse> Verify(string token)
    {
        //send email with link and token as a parameter 
        var user = await _context.Users.FirstOrDefaultAsync(u => u.VerificationToken == token);

        if (user == null)
        {
            return new RequestResponse
            {
                success = false,
                message = "Invalid token."
            };
        }
        user.VerifiedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return new RequestResponse
        {
            success = true,
            message = "User verified! {user.Email}."
        };
    }


    [HttpPost("forgot-password")]
    public async Task<RequestResponse> ForgotPassword(ForgotPasswordRequest request)
    {
        //send email with link and token as a parameter 
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || user.AccountType == AccountType.Google)
        {
            return new RequestResponse
            {
                success = false,
                message = "If user is active, you will get an email shortly."
            };
        }

        user.PasswordResetToken = CreateRandomToken();
        user.ResetTokenExpires = DateTime.Now.AddHours(1);
        await _context.SaveChangesAsync();

        var verificationUrl = $"http://localhost:5038/Login/ResetPassword?Token={user.PasswordResetToken}";


        await _emailService.SendEmail(new EmailData
        {
            Email = request.Email,
            Subject = "Account Password Reset",
            Message = $"Account has requested a new password. Use the following link to reset your password. \n Verfication Code: {verificationUrl}"

        });

        return new RequestResponse
        {
            success = true,
            message = "Password reset sent."
        };
    }

    [HttpPost("reset-password")]
    public async Task<RequestResponse> ResetPassword(ResetPasswordRequest request)
    {
        //send email with link and token as a parameter 
        var user = await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token);
        if (user == null || user.ResetTokenExpires < DateTime.Now)
        {
            return new RequestResponse
            {
                success = false,
                message = "Invalid token."
            };
        }

        CreatePasswordHash(request.Password, out byte[] passwordHash, out byte[] passwordSalt);

        user.PasswordHash = passwordHash;
        user.PasswordSalt = passwordSalt;
        user.PasswordResetToken = null;
        user.ResetTokenExpires = null;
        await _context.SaveChangesAsync();

        return new RequestResponse
        {
            success = true,
            message = "Password changed."
        };
    }

    private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
        using (var hmac = new HMACSHA512())
        {
            passwordSalt = hmac.Key;
            passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        }
    }

    private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
    {
        using (var hmac = new HMACSHA512(passwordSalt))
        {
            var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return computedHash.SequenceEqual(passwordHash);
        }
    }

    private string CreateRandomToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(64));
    }

    public async Task<string?> GetVerificationToken(string email) 
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        return user?.VerificationToken;
    }
}
