using App.Server.ORM;

/// Purpose: is a simple diagnostic service. It injects the database context and performs a test query on the Users table. 
/// It does not affect the application logic and is not part of the main learning platform workflow.
/// Dependency: uses AppDbContext directly.

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
