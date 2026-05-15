
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
            AppService Plan : F1 Free Tier (CPU 60 minutes/day , Storage 1 GB, Maximum Scale Instance 0 )
            ${builder.Configuration["Demographics:validateUrl"]}

            """);

            app.Run();
        }
    }
}
