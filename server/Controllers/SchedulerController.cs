using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using StretchScheduler.Models;

namespace StretchScheduler
{
    public static class Endpoints
    {
        public static void MapEndpoints(IApplicationBuilder app)
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapPost("/api/newAppt", async context =>
                  {
                      // Read the request body to get the data: MonthNumber, Name, YearNumber
                      var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();

                      try
                      {
                          // Deserialize the JSON request body and create an instance of a Month
                          var newAppt = JsonConvert.DeserializeObject<Appointment>(requestBody);

                          if (newAppt == null)
                          {
                              context.Response.StatusCode = 400; // Bad Request
                              await context.Response.WriteAsync("Invalid appointment data");
                              return;
                          }

                          using (var scope = app.ApplicationServices.CreateScope())
                          {
                              var dbContext = scope.ServiceProvider.GetRequiredService<StretchSchedulerContext>();
                              await dbContext.Appointments.AddAsync(newAppt);
                              await dbContext.SaveChangesAsync();
                          }

                          context.Response.StatusCode = 201; // Created
                          context.Response.ContentType = "application/json";
                          await context.Response.WriteAsync(JsonConvert.SerializeObject(newAppt));
                      }
                      catch (DbUpdateException ex)
                      {
                          // Log the exception details
                          Console.WriteLine($"An error occurred while saving changes to the database: {ex.InnerException?.Message}");
                          context.Response.StatusCode = 500; // Internal Server Error
                          await context.Response.WriteAsync("An error occurred while saving changes to the database.");
                      }
                      catch (Exception ex)
                      {
                          Console.WriteLine($"An error occurred: {ex.Message}");
                          context.Response.StatusCode = 500; // Internal Server Error
                          await context.Response.WriteAsync("An error occurred while creating the month.");
                      }
                  });
            });
        }
    }
}
