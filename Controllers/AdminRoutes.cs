using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using StretchScheduler.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System;
using System.Net;
using System.Net.Mail;


namespace StretchScheduler
{
    public static class AdminRoutes
    {

        public static void MapEndpoints(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapMethods("api/{*path}", new[] { "OPTIONS" }, AllowAccess);
            endpoints.MapGet("/api/clients", GetClients);
            endpoints.MapPost("/api/login", Login);
            endpoints.MapPost("/api/newAdmin", CreateAdmin);
            endpoints.MapPost("/api/newAppts", CreateNewAppts);
            endpoints.MapPut("/api/approveAppt", ApproveAppt);
            endpoints.MapPut("/api/denyAppt", DenyAppt);
            endpoints.MapPut("/api/completeAppt", CompleteAppt);
            endpoints.MapPut("/api/adjustBalance", AdjustBalance);
            endpoints.MapDelete("/api/deleteAppt", DeleteAppt);
            endpoints.MapDelete("/api/deleteClient", DeleteClient);
        }
        public static async Task WriteResponseAsync(HttpContext context, int statusCode, string contentType, object data)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = contentType;
            await context.Response.WriteAsync(JsonConvert.SerializeObject(data));
            return;
        }

        private static async Task AllowAccess(HttpContext context)
        {
            context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
            context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            context.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization");
            context.Response.StatusCode = 200; // OK
            await context.Response.WriteAsync("Access granted");
        }
        private static async Task<bool> Authenticate(HttpContext context)
        {
            var authenticated = await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);

            if (authenticated.Succeeded)
            {
                return true;
            }
            else
            {
                await WriteResponseAsync(context, 401, "application/json", "Unauthorized");
                return false;
            }

        }
        private static async Task GetClients(HttpContext context)
        {
            var authenticated = await Authenticate(context);
            if (!authenticated) { return; }

            using (var scope = context.RequestServices.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<StretchSchedulerContext>();
                var clients = await dbContext.Clients.ToListAsync();
                var appts = await dbContext.Appointments.Where(a => a.Status != Appointment.StatusOptions.Available).ToListAsync();
                var clientData = clients.Select(client => new
                {
                    Client = client,
                    Appointments = appts.Where(a => a.ClientId == client.Id).ToList()
                }).ToList();
                await WriteResponseAsync(context, 200, "application/json", clientData);
            }
        }

        private static async Task Login(HttpContext context)
        {
            var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
            try
            {
                var adminLoggingIn = JsonConvert.DeserializeObject<Admin>(requestBody);

                if (adminLoggingIn == null)
                {
                    await WriteResponseAsync(context, 400, "application/json", "Invalid username or password");
                    return;
                }

                using (var scope = context.RequestServices.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<StretchSchedulerContext>();
                    var admin = await dbContext.Admins.FirstOrDefaultAsync(a => a.Username == adminLoggingIn.Username);

                    if (admin == null || !admin.VerifyPassword(adminLoggingIn.Password))
                    {
                        await WriteResponseAsync(context, 401, "application/json", "Invalid username or password");
                        return;
                    }

                    var jwtToken = admin.GenerateJwtToken("ouP12@fsNv#27G48E1l1e53T59l8V0Af", "http://localhost:5062", "http://localhost:5173", 60);

                    await WriteResponseAsync(context, 200, "application/json", new { token = jwtToken });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                await WriteResponseAsync(context, 500, "application/json", "An error occurred while processing the request");
            }
        }
        private static async Task CreateAdmin(HttpContext context)
        {
            var authenticated = await Authenticate(context);
            if (!authenticated) { return; }
            var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
            try
            {
                var adminData = JsonConvert.DeserializeObject<Admin>(requestBody);

                if (adminData == null)
                {
                    await WriteResponseAsync(context, 400, "application/json", "Invalid admin data");
                    return;
                }

                using (var scope = context.RequestServices.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<StretchSchedulerContext>();


                    var existingAdmin = await dbContext.Admins.FirstOrDefaultAsync(a => a.Username == adminData.Username);
                    if (existingAdmin != null)
                    {
                        await WriteResponseAsync(context, 400, "application/json", "Username already exists");
                        return;
                    }

                    adminData.SetPassword(adminData.Password);

                    await dbContext.Admins.AddAsync(adminData);
                    await dbContext.SaveChangesAsync();

                    await WriteResponseAsync(context, 201, "application/json", adminData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                await WriteResponseAsync(context, 500, "application/json", "An error occurred while creating the admin");
            }
        }
        private static async Task CreateNewAppts(HttpContext context)
        {
            var authenticated = await Authenticate(context);
            if (!authenticated) { return; }
            // Data is array of appointments
            var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
            try
            {
                // Deserializes the JSON request body and create an array of appointments
                var newAppts = JsonConvert.DeserializeObject<List<Appointment>>(requestBody);

                if (newAppts == null || !newAppts.Any())
                {
                    await WriteResponseAsync(context, 400, "application/json", "Invalid appointment data");
                    return;
                }

                using (var scope = context.RequestServices.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<StretchSchedulerContext>();

                    foreach (var newAppt in newAppts)
                    {
                        newAppt.Status = Appointment.StatusOptions.Available;
                        await dbContext.Appointments.AddAsync(newAppt);
                    }

                    await dbContext.SaveChangesAsync();
                }

                await WriteResponseAsync(context, 201, "application/json", newAppts);
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"An error occurred while saving changes to the database: {ex.InnerException?.Message}");
                await WriteResponseAsync(context, 500, "application/json", "An error occurred while saving changes to the database.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                await WriteResponseAsync(context, 500, "application/json", "An error occurred while creating the appointments.");
            }
        }
        private static async Task ApproveAppt(HttpContext context)
        {
            var authenticated = await Authenticate(context);
            if (!authenticated) { return; }
            var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
            try
            {
                var appt = JsonConvert.DeserializeObject<Appointment>(requestBody);

                if (appt == null)
                {
                    await WriteResponseAsync(context, 400, "application/json", "Invalid appointment data");
                    return;
                }

                using (var scope = context.RequestServices.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<StretchSchedulerContext>();
                    var requestedAppt = await dbContext.Appointments.Include(a => a.Client).FirstOrDefaultAsync(a => a.Id == appt.Id);
                    if (requestedAppt == null || requestedAppt.Client == null)
                    {
                        await WriteResponseAsync(context, 404, "application/json", "Appointment or Client not found");
                        return;
                    }
                    requestedAppt.Status = Appointment.StatusOptions.Booked;
                    await dbContext.SaveChangesAsync();

                    var email = Environment.GetEnvironmentVariable("EMAIL");
                    var password = Environment.GetEnvironmentVariable("GPW");
                    if (email == null || password == null)
                    {
                        await WriteResponseAsync(context, 500, "application/json", "Email credentials not found");
                        return;
                    }

                    SmtpClient smtpClient = new SmtpClient("smtp.gmail.com");
                    smtpClient.Port = 587;
                    smtpClient.Credentials = new NetworkCredential(email, password);
                    smtpClient.EnableSsl = true;

                    MailMessage mailMessage = new MailMessage();
                    mailMessage.From = new MailAddress(email);
                    mailMessage.To.Add(requestedAppt.Client.Email);
                    mailMessage.Subject = "Appointment Confirmation";
                    mailMessage.Body = "Your appointment has been confirmed for " + requestedAppt.DateTime.ToLocalTime().ToString("MMMM dd, yyyy 'at' h:mm tt")
                     + " for the following session: " + requestedAppt.Type + ". See you soon!";

                    try
                    {
                        smtpClient.Send(mailMessage);
                        Console.WriteLine("Email sent successfully.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Failed to send email: " + ex.Message);
                        await context.Response.WriteAsync(ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                await WriteResponseAsync(context, 500, "application/json", "An error occurred while updating the appointment");
            }
        }
        private static async Task DenyAppt(HttpContext context)
        {
            var authenticated = await Authenticate(context);
            if (!authenticated) { return; }
            var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
            try
            {
                var appt = JsonConvert.DeserializeObject<Appointment>(requestBody);

                if (appt == null)
                {
                    await WriteResponseAsync(context, 400, "application/json", "Invalid appointment data");
                    return;
                }

                using (var scope = context.RequestServices.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<StretchSchedulerContext>();
                    var requestedAppt = await dbContext.Appointments.FindAsync(appt.Id);
                    if (requestedAppt == null)
                    {
                        await WriteResponseAsync(context, 404, "application/json", "Appointment not found");
                        return;
                    }
                    requestedAppt.Type = null;
                    requestedAppt.Price = null;
                    requestedAppt.Duration = null;
                    requestedAppt.ClientId = null;
                    requestedAppt.Client = null;
                    requestedAppt.Status = Appointment.StatusOptions.Available;
                    await dbContext.SaveChangesAsync();
                }

                await WriteResponseAsync(context, 200, "application/json", "Appointment Remains Available");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                await WriteResponseAsync(context, 500, "application/json", "An error occurred while updating the appointment");
            }
        }
        private static async Task CompleteAppt(HttpContext context)
        {
            var authenticated = await Authenticate(context);
            if (!authenticated) { return; }
            var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
            try
            {
                var appt = JsonConvert.DeserializeObject<Appointment>(requestBody);

                if (appt == null)
                {
                    await WriteResponseAsync(context, 400, "application/json", "Invalid appointment data");
                    return;
                }

                using (var scope = context.RequestServices.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<StretchSchedulerContext>();
                    var requestedAppt = await dbContext.Appointments.FindAsync(appt.Id);
                    if (requestedAppt == null)
                    {
                        await WriteResponseAsync(context, 404, "application/json", "Appointment not found");
                        return;
                    }
                    var client = await dbContext.Clients.FindAsync(requestedAppt.ClientId);
                    if (client == null)
                    {
                        await WriteResponseAsync(context, 404, "application/json", "Client not found");
                        return;
                    }
                    client.Balance += requestedAppt.Price ?? 0;
                    requestedAppt.Status = Appointment.StatusOptions.Completed;
                    await dbContext.SaveChangesAsync();
                }

                await WriteResponseAsync(context, 200, "application/json", "Appointment Complete");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                await WriteResponseAsync(context, 500, "application/json", "An error occurred while updating the appointment");
            }
        }
        private static async Task AdjustBalance(HttpContext context)
        {
            var authenticated = await Authenticate(context);
            if (!authenticated) { return; }
            var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
            try
            {
                var appt = JsonConvert.DeserializeObject<Appointment>(requestBody);

                if (appt == null)
                {
                    await WriteResponseAsync(context, 400, "application/json", "Invalid appointment data");
                    return;
                }

                using (var scope = context.RequestServices.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<StretchSchedulerContext>();
                    var client = await dbContext.Clients.FindAsync(appt.ClientId);
                    if (client == null)
                    {
                        await WriteResponseAsync(context, 404, "application/json", "Client not found");
                        return;
                    }
                    client.Balance -= appt.Price ?? 0;
                    await dbContext.SaveChangesAsync();
                }

                await WriteResponseAsync(context, 200, "application/json", "Appointment Set Complete");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                await WriteResponseAsync(context, 500, "application/json", "An error occurred while updating the appointment");
            }
        }
        private static async Task DeleteAppt(HttpContext context)
        {
            var authenticated = await Authenticate(context);
            if (!authenticated) { return; }
            var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
            try
            {
                var appt = JsonConvert.DeserializeObject<Appointment>(requestBody);

                if (appt == null)
                {
                    await WriteResponseAsync(context, 400, "application/json", "Invalid appointment data");
                    return;
                }

                using (var scope = context.RequestServices.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<StretchSchedulerContext>();
                    var requestedAppt = await dbContext.Appointments.FindAsync(appt.Id);
                    if (requestedAppt == null)
                    {
                        await WriteResponseAsync(context, 404, "application/json", "Appointment not found");
                        return;
                    }
                    dbContext.Appointments.Remove(requestedAppt);
                    await dbContext.SaveChangesAsync();
                }

                await WriteResponseAsync(context, 200, "application/json", "Appointment deleted successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                await WriteResponseAsync(context, 500, "application/json", "An error occurred while deleting the appointment");
            }
        }
        private static async Task DeleteClient(HttpContext context)
        {
            var authenticated = await Authenticate(context);
            if (!authenticated) { return; }
            var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
            try
            {
                var client = JsonConvert.DeserializeObject<Client>(requestBody);

                if (client == null)
                {
                    await WriteResponseAsync(context, 400, "application/json", "Invalid client data");
                    return;
                }

                using (var scope = context.RequestServices.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<StretchSchedulerContext>();
                    var requestedClient = await dbContext.Clients.FirstOrDefaultAsync(c => c.Email == client.Email);
                    if (requestedClient == null)
                    {
                        await WriteResponseAsync(context, 404, "application/json", "Client not found");
                        return;
                    }
                    dbContext.Clients.Remove(requestedClient);
                    await dbContext.SaveChangesAsync();
                }

                await WriteResponseAsync(context, 200, "application/json", client);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                await WriteResponseAsync(context, 500, "application/json", "An error occurred while deleting the client");
            }
        }
    }
}