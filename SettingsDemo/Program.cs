using System.Reflection;

namespace SettingsDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            SiteConfiguration sc = builder.AddConfiguration<SiteConfiguration>();
            builder.Services.AddSingleton<SiteConfiguration>(x => sc);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }

    public class SiteConfiguration
    {
        public String Param1 { get; set; }

        public String Param2 { get; set; }

        public SiteConfiguration()
        {

        }
    }

    public static class WebApplicationBuilderExtensions
    {
        public static T AddConfiguration<T>(this WebApplicationBuilder builder) where T : class, new()
        {
            List<String> props = Functions.getProperties<T>();
            Dictionary<String, String> prps = new();

            foreach (String prop in props)
            {
                IConfigurationSection? tsect = builder.Configuration.GetSection($"Config:{prop}");
                if (tsect.Value == null)
                {
                    throw new Exception($"Configuration error: add {prop} key to appsettings/azure config");
                }
                prps.Add(prop, tsect.Value);
            }

            T temp = new T();
            foreach (KeyValuePair<String, String> prop in prps)
            {
                PropertyInfo? p = typeof(T).GetProperty(prop.Key);
                p.SetValue(temp, prop.Value);
            }
            //ToDo:  refactor this
            builder.Services.Configure<T>(x =>
            {
                foreach (KeyValuePair<String, String> prop in prps)
                {
                    PropertyInfo? p = typeof(T).GetProperty(prop.Key);
                    p.SetValue(x, prop.Value);
                }
            });
            return temp;
        }
    }

    public static class Functions
    {
        public static List<String> getProperties<T>()
        {
            List<String> r = new List<string>();
            Type myType = typeof(T);
            PropertyInfo[] properties = myType.GetProperties();
            foreach (PropertyInfo p in properties)
            {
                var temp = p.Name;
                r.Add(temp);
            }
            return r;
        }

        public static DirectoryInfo? TryGetSolutionDirectoryInfo(string? currentPath = null)
        {
            DirectoryInfo? directory = new DirectoryInfo(currentPath ?? Directory.GetCurrentDirectory());
            while (directory != null && !directory.GetFiles("*.sln").Any())
            {
                directory = directory.Parent;
            }
            return directory;
        }

        public static T? GetConfiguration<T>(String ProjectName) where T : class, new()
        {
            DirectoryInfo? dir = TryGetSolutionDirectoryInfo();
            if (dir != null)
            {
                String CurDir = Path.Combine(dir.ToString(), ProjectName);
                IConfigurationRoot Configuration = new ConfigurationBuilder().SetBasePath(CurDir).AddJsonFile("appsettings.json").Build();
                T res = new T();
                if (Configuration != null)
                {
                    var builder = WebApplication.CreateBuilder();
                    builder.Configuration.AddConfiguration(Configuration);
                    T temp = builder.AddConfiguration<T>();
                    return temp;
                }
                return res;
            }
            else
            {
                return null;
            }
        }

    }
}
