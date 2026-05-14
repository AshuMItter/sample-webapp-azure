
namespace sample_webapp_azure
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();
            app.MapGet("/", () => """
            Hello World! from sample-webapp-azure :)
            GitHub: CI/CD pipeline for Azure Web App using GitHub Actions
            Azure AppService : Hosting me in F1 Free tier
            Vertical Scaling : Allowed 
            Horizontal Scaling : Allowed
            Slot : Staging 
            AppService Plan : S1 Standard (1 Core, 1.75 GB RAM)

            """);

            app.Run();
        }
    }
}
