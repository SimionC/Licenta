using App.Server.ORM;

/// Purpose: lightweight diagnostic service currently used by WeatherForecast endpoint. ???
/// Dependency: uses AppDbContext directly.
/// Note: TestMethod reads all users and has no business-side return value.

namespace App.Server.Services
{
    public class TestService
    {
        private readonly AppDbContext _dbContext; 

        public TestService(AppDbContext dbContext)
        {
            _dbContext = dbContext; 
        }

        public void TestMethod()
        {
            var users = _dbContext.Users.ToList();
        }

    }
}
