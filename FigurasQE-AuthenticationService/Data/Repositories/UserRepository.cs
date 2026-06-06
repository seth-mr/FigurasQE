using System.IO.Compression;
using FigurasQE_AuthenticationService.Data.Entities;
using FigurasQE_AuthenticationService.Data;
using FigurasQE_AuthenticationService.Models;
using Microsoft.EntityFrameworkCore;


namespace FigurasQE_AuthenticationService.Data.Repositories;

public class UserRepository
{

    private readonly FigurasqeContext Context;

    public UserRepository(FigurasqeContext context)
    {
        Context = context;
    }

    public async Task<AuthUser> GetUserWithCredentialsAsync(string email)
    {
        var student = await Context.Students.FirstOrDefaultAsync(u => u.Email == email);
        if (student != null)
            return MapStudentToUserRequest(student);

        var tutor = await Context.Tutors.FirstOrDefaultAsync(u => u.Email == email);
        if (tutor != null)
            return MapTutorToUserRequest(tutor);

        return null;
    }

    public async Task<AuthUser> GetAdminWithCredentialsAsync(string email)
    {
        var admin = await Context.Admins.FirstOrDefaultAsync(u => u.Email == email);
        if (admin != null)
            return MapAdminToUserRequest(admin);

        return null;
    }

    public async Task<bool> RegisterUserAsync(RegisterRequest user)
    {
        if (await EmailExistsAsync(user.Email))
        {
            return false;
        }

        if (user.Role.Equals("student"))
        {
            var student = new Student
            {
                Name = user.Name,
                Email = user.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.Password),
                Age = user.Age,
                Gender = user.Gender,
                Country = user.Country,
                Neurodivergency = user.Neurodivergency
            };
            await Context.Students.AddAsync(student);
        }
        else
        {
            var tutor = new Tutor
            {
                Name = user.Name,
                Email = user.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.Password),
                Age = user.Age,
                Gender = user.Gender,
                Country = user.Country,
                Degree = user.Degree
            };
            await Context.Tutors.AddAsync(tutor);
        }
        await Context.SaveChangesAsync();
        return true;
    }

    private async Task<bool> EmailExistsAsync(string email)
    {
        var emailInStudents = await Context.Students.AnyAsync(s => s.Email == email);
        if (emailInStudents)
        {
            return true;
        }

        var emailInTutors = await Context.Tutors.AnyAsync(t => t.Email == email);
        if (emailInTutors)
        {
            return true;
        }

        return await Context.Admins.AnyAsync(a => a.Email == email);
    }

    private AuthUser MapAdminToUserRequest(Admin admin)
    {
        return new AuthUser
        {
            Id = admin.IdAdmin,
            Email = admin.Email,
            Password = admin.PasswordHash,
            Role = "admin",
            Admin = new AuthAdminDto
            {
                IdAdmin = admin.IdAdmin,
                Name = admin.Name,
                Email = admin.Email,
                Phone = admin.Phone,
                Username = admin.Username,
                RegistrationDate = admin.RegistrationDate
            }
        };
    }

    private AuthUser MapStudentToUserRequest(Student student)
    {
        return new AuthUser
        {
            Id = student.IdStudent,
            Email = student.Email,
            Password = student.PasswordHash,
            Role = "student"
        };
    }

    private AuthUser MapTutorToUserRequest(Tutor tutor)
    {
        return new AuthUser
        {
            Id = tutor.IdTutor,
            Email = tutor.Email,
            Password = tutor.PasswordHash,
            Role = "tutor"
        };
    }
}