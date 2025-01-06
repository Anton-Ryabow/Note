using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Notes;
using System.Reflection.Metadata.Ecma335;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors();

builder.Services.AddAuthentication("Cookies")
    .AddCookie(options => {
        options.Cookie.HttpOnly = false;
    });

builder.Services.AddAuthorization();

string connection = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddDbContext<ApplicationContext>(options => options.UseNpgsql(connection));

var app = builder.Build();

string host = builder.Configuration.GetConnectionString("HostConnection")!;
app.UseCors(builder => builder.WithOrigins(host)
                             .AllowCredentials()
                             .AllowAnyHeader());

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", (HttpContext context) =>
{
    return Results.Ok();
});

app.MapPost("/login", async (HttpContext context, ApplicationContext db) =>
{
    IFormCollection form = context.Request.Form;

    string? login = form["login"];
    string? password = form["password"];

    if (!Check.AreValid(login, password))
        return Results.BadRequest(new { message = "������� ������� ����� ��� ������!" });

    User? user = db.Users.Include(u => u.Notes).FirstOrDefault(u => u.Login == login && u.Password == password!.GetHashPass());

    if (user == null)
        return Results.BadRequest(new { message = "������� ������� ����� ��� ������!" });

    context.Response.Cookies.Append("CountOfNotes", user.Notes.Count().ToString());

    List<Claim> claims = new List<Claim>()
    {
        new Claim("Id", user.Id.ToString()),
        new Claim("Name", user.Name)
    };

    ClaimsIdentity identity = new ClaimsIdentity(claims, "Cookies");
    await context.SignInAsync("Cookies", new ClaimsPrincipal(identity));

    return Results.Ok(new { message = $"������������ �����������!" });
});

app.MapPost("/registration", async (HttpContext context, ApplicationContext db) =>
{
    IFormCollection form = context.Request.Form;

    string? name = form["name"];
    string? lastname = form["lastname"];
    string? email = form["email"];
    string? login = form["login"];
    string? password = form["password"];

    if (!Check.AreValid(name, lastname, email, login, password))
        return Results.BadRequest("��� ���� ������ ���� ���������!");

    User user = new User(name!, lastname!, email!, login!, password!.GetHashPass());

    await db.Users.AddAsync(user);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "������������ ������� ���������������!" });
});

app.MapPost("/logout", async (HttpContext context) =>
{
    context.Response.Cookies.Delete("CountOfNotes");
    context.Response.Cookies.Delete("NoteId");
    await context.SignOutAsync("Cookies");
    return Results.Ok(new { message = "������������ ����� �� ��������!" });
});

app.MapPost("/addNote", async (HttpContext context, ApplicationContext db) =>
{
    IFormCollection form = context.Request.Form;

    string? title = form["title"];
    string? content = form["content"];

    if (!Check.AreValid(title, content))
        return Results.BadRequest(new { message = "��� ���� ������ ���� ���������!" });

    string? idClaim = context.User.FindFirst("Id")?.Value;

    if (idClaim == null)
        return Results.BadRequest(
            new { message = "��� �������������� ������� Claim ��� ������������� ������������!" });

    Guid userId = Guid.Parse(idClaim);

    User? user = db.Users.FirstOrDefault(u => u.Id == userId);

    if (user == null)
        return Results.BadRequest(new { message = "������������ �� ������!" });

    try
    {
        Note note = new(title = null!, content = null!, DateOnly.FromDateTime(DateTime.Now), user);
        user.Notes.Add(note);
        await db.SaveChangesAsync();

        string? cookieCountOfNotes = context.Request.Cookies["CountOfNotes"];
        if (cookieCountOfNotes == null)
            return Results.BadRequest(new { message = "����������� ���� �� ����������!" });
        int count = int.Parse(cookieCountOfNotes) + 1;
        context.Response.Cookies.Append("CountOfNotes", count.ToString());

        return Results.Ok(new { message = "������� ���������!" });
    }
    catch
    {
        return Results.BadRequest(new { message = "������ ����������� � ���� ������!" });
    }
});

app.MapGet("/getNotes", (HttpContext context, ApplicationContext db) =>
{
    string? idClaim = context.User.FindFirst("Id")?.Value;

    if (idClaim == null)
        return Results.BadRequest(
            new { message = "��� �������������� ������� Claim ��� ������������� ������������!" });

    Guid userId = Guid.Parse(idClaim);

    User? user = db.Users.Include(u => u.Notes).FirstOrDefault(u => u.Id == userId);

    if (user == null)
    {
        return Results.BadRequest(new { message = "������������ �� ������!" });
    }

    return Results.Json(user.Notes.Select(n => new
    {
        n.Id,
        n.Title,
        n.Content,
        date = n.Date.ToString()
    }));
});

app.MapGet("/getNote", (HttpContext context, ApplicationContext db) =>
{
    string? idCookie = context.Request.Cookies["NoteId"];

    if (idCookie == null)
        return Results.BadRequest(
            new { message = "��� �������������� ������� Cookie ��� ������������� �������!" });

    Guid noteId = Guid.Parse(idCookie);

    var note = db.Notes.FirstOrDefault(n => n.Id == noteId);

    if (note == null)
    {
        return Results.BadRequest(new { message = "������� �� �������!" });
    }

    return Results.Json(new
    {
        note.Id,
        note.Title,
        note.Content,
    });
});

app.MapPost("/editNote", (HttpContext context, ApplicationContext db) =>
{
    IFormCollection form = context.Request.Form;

    string? formId = form["id"];
    if (formId == null)
        return Results.BadRequest(new { message = "������ �� ������!" });
    
    Guid noteId = Guid.Parse(formId);
    string? title = form["title"];
    string? content = form["content"];

    if (!Check.AreValid(title, content))
        return Results.BadRequest(new { message = "��� ���� ������ ���� ���������!" });

    var note = db.Notes.FirstOrDefault(db => db.Id == noteId);

    if (note == null)
    {
        return Results.BadRequest(new { message = "������� �� �������!" });
    }

    note.Title = title!;
    note.Content = content!;
    note.Date = DateOnly.FromDateTime(DateTime.Now);

    db.SaveChanges();

    context.Response.Cookies.Delete("NoteId");
    return Results.Ok(new { message = "��������� ������!" });
});

app.MapGet("/deleteNote", (HttpContext context, ApplicationContext db) =>
{
    string? idCookie = context.Request.Cookies["NoteId"];

    if (idCookie == null)
        return Results.BadRequest(
            new { message = "��� �������������� ������� Cookie ��� ������������� �������!" });

    Guid noteId = Guid.Parse(idCookie);

    var note = db.Notes.FirstOrDefault(n => n.Id == noteId);

    if (note == null)
    {
        return Results.BadRequest(new { message = "������� �� �������!" });
    }

    db.Notes.Remove(note);
    db.SaveChanges();

    string? countOfNotes = context.Request.Cookies["CountOfNotes"];
    if (countOfNotes == null)
        return Results.BadRequest("����������� ������������� ������ Cookie ��� ����������� ���������� �������!");

    int count = int.Parse(countOfNotes) - 1;
    context.Response.Cookies.Append("CountOfNotes", count.ToString());
    context.Response.Cookies.Delete("NoteId");

    return Results.Ok(new { message = "������� �������!" });
});

app.Run();
